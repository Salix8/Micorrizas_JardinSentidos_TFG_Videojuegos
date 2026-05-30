using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
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
        private readonly NetworkList<int> pendingMismatchCardInstanceIds = new();
        private readonly NetworkList<int> pendingMatchedCardInstanceIds = new();
        private readonly NetworkVariable<int> matchedPairCount = new();
        private readonly NetworkVariable<int> failedAttemptCount = new();
        private readonly NetworkVariable<int> totalPairCount = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();

        private int assignmentSeed;
        private Coroutine pendingMatchedPairCoroutine;
        private double gameplayEndServerTime;
        private int lastPublishedWholeSecond;

        public int MatchedPairCount => matchedPairCount.Value;
        public int FailedAttemptCount => failedAttemptCount.Value;
        public int TotalPairCount => totalPairCount.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();
        public bool HasPendingMismatch => pendingMismatchCardInstanceIds.Count > 0;
        public bool HasPendingMatchedPair => pendingMatchedCardInstanceIds.Count > 0;

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return distributedPairsMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            cardStates.OnListChanged += HandleCardStatesChanged;
            pendingMismatchCardInstanceIds.OnListChanged += HandlePendingMismatchChanged;
            pendingMatchedCardInstanceIds.OnListChanged += HandlePendingMatchedChanged;
            matchedPairCount.OnValueChanged += HandleSimpleStateChanged;
            failedAttemptCount.OnValueChanged += HandleSimpleStateChanged;
            totalPairCount.OnValueChanged += HandleSimpleStateChanged;
            remainingTimeSeconds.OnValueChanged += HandleTimerChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusMessageChanged;

            base.OnNetworkSpawn();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            cardStates.OnListChanged -= HandleCardStatesChanged;
            pendingMismatchCardInstanceIds.OnListChanged -= HandlePendingMismatchChanged;
            pendingMatchedCardInstanceIds.OnListChanged -= HandlePendingMatchedChanged;
            matchedPairCount.OnValueChanged -= HandleSimpleStateChanged;
            failedAttemptCount.OnValueChanged -= HandleSimpleStateChanged;
            totalPairCount.OnValueChanged -= HandleSimpleStateChanged;
            remainingTimeSeconds.OnValueChanged -= HandleTimerChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusMessageChanged;

            base.OnNetworkDespawn();
            StopPendingMatchedPairCoroutine();
        }

        private void Update()
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing || HasPublishedResult || distributedPairsMinigameConfig == null)
            {
                return;
            }

            var remainingSeconds = Mathf.Max(0f, (float)(gameplayEndServerTime - NetworkManager.ServerTime.Time));
            var wholeSeconds = Mathf.CeilToInt(remainingSeconds);
            if (!Mathf.Approximately(remainingTimeSeconds.Value, remainingSeconds) &&
                (wholeSeconds != lastPublishedWholeSecond || remainingSeconds <= 0f))
            {
                remainingTimeSeconds.Value = remainingSeconds;
                lastPublishedWholeSecond = wholeSeconds;
            }

            if (remainingSeconds <= 0f)
            {
                CompleteMinigameServer();
            }
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

        public int GetDrawPileCount()
        {
            return CountCardsInZone(DistributedPairsCardZone.DrawPile);
        }

        public int GetDiscardPileCount()
        {
            return CountCardsInZone(DistributedPairsCardZone.Discarded);
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

            if (HasPendingMismatch)
            {
                TryAcknowledgePendingMismatch();
                return;
            }

            if (HasPendingMatchedPair)
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

        public void TryAcknowledgePendingMismatch()
        {
            if (Stage != CooperativeMinigameStage.Playing || !HasPendingMismatch)
            {
                return;
            }

            if (IsServer)
            {
                ClearPendingMismatchServer();
                return;
            }

            AcknowledgePendingMismatchServerRpc();
        }

        protected override void InitializeMinigameServer()
        {
            assignmentSeed = Environment.TickCount;
            pendingMismatchCardInstanceIds.Clear();
            pendingMatchedCardInstanceIds.Clear();
            StopPendingMatchedPairCoroutine();
            matchedPairCount.Value = 0;
            failedAttemptCount.Value = 0;
            totalPairCount.Value = distributedPairsMinigameConfig == null ? 0 : distributedPairsMinigameConfig.ActivePairCount;
            remainingTimeSeconds.Value = distributedPairsMinigameConfig == null ? 0f : distributedPairsMinigameConfig.TimeLimitSeconds;
            sharedStatusMessage.Value = new FixedString128Bytes("Selecciona una carta y comunicate con el resto del grupo.");

            cardStates.Clear();

            if (distributedPairsMinigameConfig == null || distributedPairsMinigameConfig.ActivePairCount <= 0)
            {
                Debug.LogError($"{nameof(DistributedPairsMinigameSession)} requires a valid {nameof(DistributedPairsMinigameConfig)}.", this);
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            var participantIds = GetParticipantIds();
            var initialDealPlan = CreateInitialDealPlan(participantIds, distributedPairsMinigameConfig, assignmentSeed++);
            var deckCards = initialDealPlan.DeckCards;
            var initialAssignments = initialDealPlan.Assignments;

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

        protected override void OnGameplayStartedServer()
        {
            if (!IsServer || distributedPairsMinigameConfig == null || HasPublishedResult)
            {
                return;
            }

            gameplayEndServerTime = NetworkManager.ServerTime.Time + distributedPairsMinigameConfig.TimeLimitSeconds;
            remainingTimeSeconds.Value = distributedPairsMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = Mathf.CeilToInt(distributedPairsMinigameConfig.TimeLimitSeconds);
            sharedStatusMessage.Value = new FixedString128Bytes("Selecciona una carta y comunicate con el resto del grupo.");
            StateChanged?.Invoke();
        }

        [Rpc(SendTo.Server)]
        private void ToggleSelectionServerRpc(int cardInstanceId, RpcParams rpcParams = default)
        {
            HandleToggleSelectionServer(cardInstanceId, rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server)]
        private void AcknowledgePendingMismatchServerRpc(RpcParams rpcParams = default)
        {
            ClearPendingMismatchServer();
        }

        private void HandleToggleSelectionServer(int cardInstanceId, ulong senderClientId)
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing)
            {
                return;
            }

            if (HasPendingMismatch)
            {
                ClearPendingMismatchServer();
                return;
            }

            if (HasPendingMatchedPair)
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
                pendingMismatchCardInstanceIds.Clear();
                pendingMatchedCardInstanceIds.Clear();
                pendingMatchedCardInstanceIds.Add(firstCard.CardInstanceId);
                pendingMatchedCardInstanceIds.Add(secondCard.CardInstanceId);
                matchedPairCount.Value += 1;
                sharedStatusMessage.Value = new FixedString128Bytes("Pareja encontrada. Las cartas permanecen boca arriba un momento.");
                StartPendingMatchedPairResolutionServer();

                return;
            }

            pendingMismatchCardInstanceIds.Clear();
            pendingMismatchCardInstanceIds.Add(firstCard.CardInstanceId);
            pendingMismatchCardInstanceIds.Add(secondCard.CardInstanceId);
            failedAttemptCount.Value += 1;
            sharedStatusMessage.Value = new FixedString128Bytes("Las cartas no coinciden. Toca la pantalla para girarlas de nuevo.");
        }

        private void StartPendingMatchedPairResolutionServer()
        {
            if (!IsServer)
            {
                return;
            }

            StopPendingMatchedPairCoroutine();
            pendingMatchedPairCoroutine = StartCoroutine(ResolvePendingMatchedPairAfterDelayServer());
        }

        private IEnumerator ResolvePendingMatchedPairAfterDelayServer()
        {
            var revealDuration = distributedPairsMinigameConfig == null
                ? 0f
                : distributedPairsMinigameConfig.MatchFeedbackSettings.MatchedRevealDuration;

            if (revealDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(revealDuration);
            }

            ResolvePendingMatchedPairNowServer();
            pendingMatchedPairCoroutine = null;
        }

        private void ResolvePendingMatchedPairNowServer()
        {
            if (!IsServer || pendingMatchedCardInstanceIds.Count < 2)
            {
                pendingMatchedCardInstanceIds.Clear();
                return;
            }

            for (var pendingIndex = 0; pendingIndex < pendingMatchedCardInstanceIds.Count; pendingIndex++)
            {
                var cardIndex = FindCardStateIndex(pendingMatchedCardInstanceIds[pendingIndex]);
                if (cardIndex < 0)
                {
                    continue;
                }

                MoveCardToDiscard(cardIndex);
            }

            pendingMatchedCardInstanceIds.Clear();
            TryTopUpHandsToTarget();

            if (matchedPairCount.Value >= totalPairCount.Value)
            {
                PublishResultServer(DistributedPairsScoreService.CreateResult(
                    distributedPairsMinigameConfig,
                    matchedPairCount.Value,
                    failedAttemptCount.Value));
                return;
            }

            UpdateSharedStatusForSelectionCount();
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
            var occupiedHandSlots = new Dictionary<ulong, HashSet<int>>();

            foreach (var participantId in participantIds)
            {
                var pairIds = new List<int>();
                var count = 0;
                var occupiedSlots = new HashSet<int>();
                for (var index = 0; index < cardStates.Count; index++)
                {
                    var state = cardStates[index];
                    if (state.Zone == DistributedPairsCardZone.Hand && state.OwnerClientId == participantId)
                    {
                        pairIds.Add(state.PairId);
                        count++;
                        if (state.HandOrder >= 0 && state.HandOrder < distributedPairsMinigameConfig.CardsPerDevice)
                        {
                            occupiedSlots.Add(state.HandOrder);
                        }
                    }
                }

                currentHandCounts[participantId] = count;
                currentHandPairIds[participantId] = pairIds;
                occupiedHandSlots[participantId] = occupiedSlots;
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
                assignmentSeed++,
                distributedPairsMinigameConfig.GuaranteedVisiblePairsOffset);

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
                state.HandOrder = GetNextAvailableHandSlot(occupiedHandSlots[assignment.Value], distributedPairsMinigameConfig.CardsPerDevice);
                occupiedHandSlots[assignment.Value].Add(state.HandOrder);
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
                var occupiedOrders = new HashSet<int>();
                var pendingIndices = new List<int>();
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

                for (var index = 0; index < handIndices.Count; index++)
                {
                    var state = cardStates[handIndices[index]];
                    if (state.HandOrder >= 0 &&
                        state.HandOrder < distributedPairsMinigameConfig.CardsPerDevice &&
                        occupiedOrders.Add(state.HandOrder))
                    {
                        continue;
                    }

                    pendingIndices.Add(handIndices[index]);
                }

                for (var index = 0; index < pendingIndices.Count; index++)
                {
                    var state = cardStates[pendingIndices[index]];
                    state.HandOrder = GetNextAvailableHandSlot(occupiedOrders, distributedPairsMinigameConfig.CardsPerDevice);
                    occupiedOrders.Add(state.HandOrder);
                    cardStates[pendingIndices[index]] = state;
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

        private static int GetNextAvailableHandSlot(HashSet<int> occupiedOrders, int slotCount)
        {
            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (!occupiedOrders.Contains(slotIndex))
                {
                    return slotIndex;
                }
            }

            return slotCount <= 0 ? 0 : slotCount - 1;
        }

        private static DistributedPairsInitialDealPlan CreateInitialDealPlan(
            IReadOnlyList<ulong> participantIds,
            DistributedPairsMinigameConfig config,
            int randomSeed)
        {
            var deckCards = BuildDeck(config.ActivePairCount);
            var emptyPairIds = participantIds.ToDictionary(playerId => playerId, _ => (IReadOnlyList<int>)Array.Empty<int>());
            var emptyCounts = participantIds.ToDictionary(playerId => playerId, _ => 0);
            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                emptyPairIds,
                emptyCounts,
                deckCards,
                config.CardsPerDevice,
                randomSeed,
                config.GuaranteedVisiblePairsOffset);

            return new DistributedPairsInitialDealPlan(deckCards, assignments);
        }

        private static List<DistributedPairsCardModel> BuildDeck(int pairCount)
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

        private readonly struct DistributedPairsInitialDealPlan
        {
            public DistributedPairsInitialDealPlan(
                IReadOnlyList<DistributedPairsCardModel> deckCards,
                IReadOnlyDictionary<int, ulong> assignments)
            {
                DeckCards = deckCards;
                Assignments = assignments;
            }

            public IReadOnlyList<DistributedPairsCardModel> DeckCards { get; }
            public IReadOnlyDictionary<int, ulong> Assignments { get; }
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
            if (HasPendingMismatch)
            {
                sharedStatusMessage.Value = new FixedString128Bytes("Las cartas no coinciden. Toca la pantalla para girarlas de nuevo.");
                return;
            }

            if (HasPendingMatchedPair)
            {
                sharedStatusMessage.Value = new FixedString128Bytes("Pareja encontrada. Las cartas permanecen boca arriba un momento.");
                return;
            }

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

        private void ClearPendingMismatchServer()
        {
            if (!IsServer || pendingMismatchCardInstanceIds.Count == 0)
            {
                return;
            }

            for (var pendingIndex = 0; pendingIndex < pendingMismatchCardInstanceIds.Count; pendingIndex++)
            {
                var cardIndex = FindCardStateIndex(pendingMismatchCardInstanceIds[pendingIndex]);
                if (cardIndex < 0)
                {
                    continue;
                }

                var state = cardStates[cardIndex];
                if (state.Zone != DistributedPairsCardZone.Hand)
                {
                    continue;
                }

                state.IsSelected = false;
                cardStates[cardIndex] = state;
            }

            pendingMismatchCardInstanceIds.Clear();
            UpdateSharedStatusForSelectionCount();
        }

        private void StopPendingMatchedPairCoroutine()
        {
            if (pendingMatchedPairCoroutine == null)
            {
                return;
            }

            StopCoroutine(pendingMatchedPairCoroutine);
            pendingMatchedPairCoroutine = null;
        }

        private void CompleteMinigameServer()
        {
            if (!IsServer || HasPublishedResult)
            {
                return;
            }

            StopPendingMatchedPairCoroutine();
            remainingTimeSeconds.Value = Mathf.Max(0f, remainingTimeSeconds.Value);

            var result = DistributedPairsScoreService.CreateResult(
                distributedPairsMinigameConfig,
                matchedPairCount.Value,
                failedAttemptCount.Value);

            if (matchedPairCount.Value < totalPairCount.Value && distributedPairsMinigameConfig != null)
            {
                result = new MinigameResultData(
                    distributedPairsMinigameConfig.TimeoutMessage,
                    result.ScoreOutOfTen,
                    result.SuccessfulActions,
                    result.FailedActions);
            }

            PublishResultServer(result);
        }

        private int CountCardsInZone(DistributedPairsCardZone zone)
        {
            var count = 0;
            for (var index = 0; index < cardStates.Count; index++)
            {
                if (cardStates[index].Zone == zone)
                {
                    count++;
                }
            }

            return count;
        }

        private void HandleCardStatesChanged(NetworkListEvent<DistributedPairsCardNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandlePendingMismatchChanged(NetworkListEvent<int> _)
        {
            StateChanged?.Invoke();
        }

        private void HandlePendingMatchedChanged(NetworkListEvent<int> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleSimpleStateChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleTimerChanged(float _, float __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusMessageChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }
    }
}
