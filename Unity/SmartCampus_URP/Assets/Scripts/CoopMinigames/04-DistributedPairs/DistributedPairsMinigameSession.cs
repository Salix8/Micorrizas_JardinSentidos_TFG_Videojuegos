using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsMinigameSession : CooperativeMinigameBase
    {
        private const ulong NoOwnerClientId = ulong.MaxValue;

        [SerializeField] private DistributedPairsMinigameConfig distributedPairsMinigameConfig;

        private readonly NetworkList<DistributedPairsCardNetworkState> cardStates = new();
        private readonly NetworkVariable<int> matchedPairCount = new();
        private readonly NetworkVariable<int> failedAttemptCount = new();
        private readonly NetworkVariable<int> totalPairCount = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();

        private int assignmentSeed;

        public int MatchedPairCount => matchedPairCount.Value;
        public int FailedAttemptCount => failedAttemptCount.Value;
        public int TotalPairCount => totalPairCount.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return distributedPairsMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            cardStates.OnListChanged += HandleCardStatesChanged;
            matchedPairCount.OnValueChanged += HandleSimpleStateChanged;
            failedAttemptCount.OnValueChanged += HandleSimpleStateChanged;
            totalPairCount.OnValueChanged += HandleSimpleStateChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusMessageChanged;

            base.OnNetworkSpawn();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            cardStates.OnListChanged -= HandleCardStatesChanged;
            matchedPairCount.OnValueChanged -= HandleSimpleStateChanged;
            failedAttemptCount.OnValueChanged -= HandleSimpleStateChanged;
            totalPairCount.OnValueChanged -= HandleSimpleStateChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusMessageChanged;

            base.OnNetworkDespawn();
        }

        public IReadOnlyList<DistributedPairsCardNetworkState> GetLocalHandStates()
        {
            return GetHandStatesForClient(GetLocalClientId());
        }

        public IReadOnlyList<DistributedPairsCardNetworkState> GetHandStatesForClient(ulong clientId)
        {
            var handStates = new List<DistributedPairsCardNetworkState>();
            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == clientId)
                {
                    handStates.Add(state);
                }
            }

            handStates.Sort((left, right) =>
            {
                var orderComparison = left.HandOrder.CompareTo(right.HandOrder);
                return orderComparison != 0 ? orderComparison : left.CardInstanceId.CompareTo(right.CardInstanceId);
            });

            return handStates;
        }

        public DistributedPairsCardNetworkState? GetLocalSelectedCard()
        {
            var localClientId = GetLocalClientId();
            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == localClientId && state.IsSelected)
                {
                    return state;
                }
            }

            return null;
        }

        public DistributedPairDefinition GetPairDefinition(int pairId)
        {
            return distributedPairsMinigameConfig == null ? null : distributedPairsMinigameConfig.GetPairDefinition(pairId);
        }

        public void TryToggleLocalCardSelection(int cardInstanceId)
        {
            if (Stage != CooperativeMinigameStage.Playing)
            {
                return;
            }

            if (IsServer)
            {
                HandleToggleSelectionServer(cardInstanceId, GetLocalClientId());
                return;
            }

            ToggleSelectionServerRpc(cardInstanceId);
        }

        protected override void InitializeMinigameServer()
        {
            assignmentSeed = Environment.TickCount;
            matchedPairCount.Value = 0;
            failedAttemptCount.Value = 0;
            totalPairCount.Value = distributedPairsMinigameConfig == null ? 0 : distributedPairsMinigameConfig.ActivePairCount;
            sharedStatusMessage.Value = new FixedString128Bytes("Selecciona una carta y comunicate con el resto del grupo.");

            cardStates.Clear();

            if (distributedPairsMinigameConfig == null || distributedPairsMinigameConfig.ActivePairCount <= 0)
            {
                Debug.LogError($"{nameof(DistributedPairsMinigameSession)} requires a valid {nameof(DistributedPairsMinigameConfig)}.", this);
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            var participantIds = GetParticipantIds();
            var deckCards = BuildDeck(distributedPairsMinigameConfig.ActivePairCount);

            var emptyPairIds = participantIds.ToDictionary(playerId => playerId, _ => (IReadOnlyList<int>)Array.Empty<int>());
            var emptyCounts = participantIds.ToDictionary(playerId => playerId, _ => 0);
            var initialAssignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                emptyPairIds,
                emptyCounts,
                deckCards,
                distributedPairsMinigameConfig.CardsPerDevice,
                assignmentSeed++);

            foreach (var card in deckCards)
            {
                if (initialAssignments.TryGetValue(card.CardInstanceId, out var ownerClientId))
                {
                    cardStates.Add(new DistributedPairsCardNetworkState
                    {
                        CardInstanceId = card.CardInstanceId,
                        PairId = card.PairId,
                        OwnerClientId = ownerClientId,
                        IsSelected = false,
                        HandOrder = -1,
                        Zone = DistributedPairsCardZone.Hand
                    });
                }
                else
                {
                    cardStates.Add(new DistributedPairsCardNetworkState
                    {
                        CardInstanceId = card.CardInstanceId,
                        PairId = card.PairId,
                        OwnerClientId = NoOwnerClientId,
                        IsSelected = false,
                        HandOrder = -1,
                        Zone = DistributedPairsCardZone.DrawPile
                    });
                }
            }

            NormalizeHandOrderFields();
        }

        [Rpc(SendTo.Server)]
        private void ToggleSelectionServerRpc(int cardInstanceId, RpcParams rpcParams = default)
        {
            HandleToggleSelectionServer(cardInstanceId, rpcParams.Receive.SenderClientId);
        }

        private void HandleToggleSelectionServer(int cardInstanceId, ulong senderClientId)
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing)
            {
                return;
            }

            var requestedCardIndex = FindCardStateIndex(cardInstanceId);
            if (requestedCardIndex < 0)
            {
                return;
            }

            var requestedCard = cardStates[requestedCardIndex];
            if (requestedCard.Zone != DistributedPairsCardZone.Hand || requestedCard.OwnerClientId != senderClientId)
            {
                return;
            }

            var currentlySelectedIndex = FindSelectedCardIndex(senderClientId);
            if (currentlySelectedIndex == requestedCardIndex)
            {
                requestedCard.IsSelected = !requestedCard.IsSelected;
                cardStates[requestedCardIndex] = requestedCard;
                UpdateSharedStatusForSelectionCount();
                return;
            }

            if (currentlySelectedIndex >= 0)
            {
                var previousCard = cardStates[currentlySelectedIndex];
                previousCard.IsSelected = false;
                cardStates[currentlySelectedIndex] = previousCard;
            }

            requestedCard.IsSelected = true;
            cardStates[requestedCardIndex] = requestedCard;

            ResolveSelectedCardsServer();
        }

        private void ResolveSelectedCardsServer()
        {
            var selectedIndices = GetSelectedCardIndices();
            if (selectedIndices.Count < 2)
            {
                UpdateSharedStatusForSelectionCount();
                return;
            }

            var firstIndex = selectedIndices[0];
            var secondIndex = selectedIndices[1];
            var firstCard = cardStates[firstIndex];
            var secondCard = cardStates[secondIndex];

            if (firstCard.OwnerClientId == secondCard.OwnerClientId)
            {
                secondCard.IsSelected = false;
                cardStates[secondIndex] = secondCard;
                UpdateSharedStatusForSelectionCount();
                return;
            }

            if (firstCard.PairId == secondCard.PairId)
            {
                MoveCardToDiscard(firstIndex);
                MoveCardToDiscard(secondIndex);
                matchedPairCount.Value += 1;
                sharedStatusMessage.Value = new FixedString128Bytes("Pareja encontrada. Se reponen las manos cuando el mazo lo permite.");
                TryTopUpHandsToTarget();

                if (matchedPairCount.Value >= totalPairCount.Value)
                {
                    PublishResultServer(DistributedPairsScoreService.CreateResult(
                        distributedPairsMinigameConfig,
                        matchedPairCount.Value,
                        failedAttemptCount.Value));
                }

                return;
            }

            firstCard.IsSelected = false;
            secondCard.IsSelected = false;
            cardStates[firstIndex] = firstCard;
            cardStates[secondIndex] = secondCard;
            failedAttemptCount.Value += 1;
            sharedStatusMessage.Value = new FixedString128Bytes("Las cartas no coinciden. Intentalo de nuevo.");
        }

        private void MoveCardToDiscard(int cardIndex)
        {
            var state = cardStates[cardIndex];
            state.IsSelected = false;
            state.OwnerClientId = NoOwnerClientId;
            state.HandOrder = -1;
            state.Zone = DistributedPairsCardZone.Discarded;
            cardStates[cardIndex] = state;
        }

        private void TryTopUpHandsToTarget()
        {
            var participantIds = GetParticipantIds();
            if (participantIds.Count == 0 || distributedPairsMinigameConfig == null)
            {
                return;
            }

            var currentHandCounts = new Dictionary<ulong, int>();
            var currentHandPairIds = new Dictionary<ulong, IReadOnlyList<int>>();

            foreach (var participantId in participantIds)
            {
                var pairIds = new List<int>();
                var count = 0;
                for (var index = 0; index < cardStates.Count; index++)
                {
                    var state = cardStates[index];
                    if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == participantId)
                    {
                        pairIds.Add(state.PairId);
                        count++;
                    }
                }

                currentHandCounts[participantId] = count;
                currentHandPairIds[participantId] = pairIds;
            }

            var drawPileCards = new List<DistributedPairsCardModel>();
            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone == DistributedPairsCardZone.DrawPile)
                {
                    drawPileCards.Add(new DistributedPairsCardModel(state.CardInstanceId, state.PairId));
                }
            }

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                drawPileCards,
                distributedPairsMinigameConfig.CardsPerDevice,
                assignmentSeed++);

            foreach (var assignment in assignments)
            {
                var cardIndex = FindCardStateIndex(assignment.Key);
                if (cardIndex < 0)
                {
                    continue;
                }

                var state = cardStates[cardIndex];
                state.Zone = DistributedPairsCardZone.Hand;
                state.OwnerClientId = assignment.Value;
                state.IsSelected = false;
                state.HandOrder = int.MaxValue;
                cardStates[cardIndex] = state;
            }

            NormalizeHandOrderFields();
        }

        private void NormalizeHandOrderFields()
        {
            var participantIds = GetParticipantIds();
            foreach (var participantId in participantIds)
            {
                var handIndices = new List<int>();
                for (var index = 0; index < cardStates.Count; index++)
                {
                    var state = cardStates[index];
                    if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == participantId)
                    {
                        handIndices.Add(index);
                    }
                }

                handIndices.Sort((leftIndex, rightIndex) =>
                {
                    var left = cardStates[leftIndex];
                    var right = cardStates[rightIndex];
                    var leftOrder = left.HandOrder < 0 ? int.MaxValue : left.HandOrder;
                    var rightOrder = right.HandOrder < 0 ? int.MaxValue : right.HandOrder;
                    var orderComparison = leftOrder.CompareTo(rightOrder);
                    return orderComparison != 0 ? orderComparison : left.CardInstanceId.CompareTo(right.CardInstanceId);
                });

                for (var order = 0; order < handIndices.Count; order++)
                {
                    var state = cardStates[handIndices[order]];
                    if (state.HandOrder == order)
                    {
                        continue;
                    }

                    state.HandOrder = order;
                    cardStates[handIndices[order]] = state;
                }
            }

            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone != DistributedPairsCardZone.Hand && state.HandOrder != -1)
                {
                    state.HandOrder = -1;
                    cardStates[index] = state;
                }
            }
        }

        private List<DistributedPairsCardModel> BuildDeck(int pairCount)
        {
            var deckCards = new List<DistributedPairsCardModel>(pairCount * 2);
            var cardInstanceId = 0;
            for (var pairId = 0; pairId < pairCount; pairId++)
            {
                deckCards.Add(new DistributedPairsCardModel(cardInstanceId++, pairId));
                deckCards.Add(new DistributedPairsCardModel(cardInstanceId++, pairId));
            }

            return deckCards;
        }

        private int FindCardStateIndex(int cardInstanceId)
        {
            for (var index = 0; index < cardStates.Count; index++)
            {
                if (cardStates[index].CardInstanceId == cardInstanceId)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindSelectedCardIndex(ulong ownerClientId)
        {
            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == ownerClientId && state.IsSelected)
                {
                    return index;
                }
            }

            return -1;
        }

        private List<int> GetSelectedCardIndices()
        {
            var selectedIndices = new List<int>();
            for (var index = 0; index < cardStates.Count; index++)
            {
                var state = cardStates[index];
                if (state.Zone == DistributedPairsCardZone.Hand && state.IsSelected)
                {
                    selectedIndices.Add(index);
                }
            }

            return selectedIndices;
        }

        private void UpdateSharedStatusForSelectionCount()
        {
            var selectionCount = 0;
            for (var index = 0; index < cardStates.Count; index++)
            {
                if (cardStates[index].Zone == DistributedPairsCardZone.Hand && cardStates[index].IsSelected)
                {
                    selectionCount++;
                }
            }

            sharedStatusMessage.Value = selectionCount switch
            {
                0 => new FixedString128Bytes("Selecciona una carta y comunicate con el resto del grupo."),
                1 => new FixedString128Bytes("Hay una carta activa. Otro dispositivo debe intentar completar la pareja."),
                _ => new FixedString128Bytes("Resolviendo intento cooperativo...")
            };
        }

        private void HandleCardStatesChanged(NetworkListEvent<DistributedPairsCardNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleSimpleStateChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusMessageChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }
    }
}
