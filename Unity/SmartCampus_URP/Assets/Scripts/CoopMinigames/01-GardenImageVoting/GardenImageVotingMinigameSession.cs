using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    [DisallowMultipleComponent]
    public sealed class GardenImageVotingMinigameSession : CooperativeMinigameBase
    {
        [SerializeField] private GardenImageVotingMinigameConfig gardenImageVotingMinigameConfig;

        private readonly NetworkList<GardenImageVotingPlayerProgressNetworkState> playerProgressStates = new();
        private readonly NetworkVariable<int> sharedCorrectAnswers = new();
        private readonly NetworkVariable<int> sharedIncorrectAnswers = new();
        private readonly NetworkVariable<int> totalScheduledCards = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();

        private readonly Dictionary<ulong, List<GardenImageVotingCardDefinition>> cardAssignmentsByClient = new();
        private readonly List<GardenImageVotingCardDefinition> loadedCardDefinitions = new();

        private Coroutine csvLoadingCoroutine;
        private bool hasLoadedCardDefinitions;
        private bool serverDataPrepared;
        private bool serverGameplayActive;
        private bool pendingGameplayStart;
        private string dataLoadError = string.Empty;
        private double gameplayEndServerTime;
        private int lastPublishedWholeSecond = -1;

        public bool HasLoadedCardDefinitions => hasLoadedCardDefinitions;
        public string DataLoadError => dataLoadError;
        public int SharedCorrectAnswers => sharedCorrectAnswers.Value;
        public int SharedIncorrectAnswers => sharedIncorrectAnswers.Value;
        public int SharedAnsweredCount => sharedCorrectAnswers.Value + sharedIncorrectAnswers.Value;
        public int TotalScheduledCards => totalScheduledCards.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return gardenImageVotingMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            playerProgressStates.OnListChanged += HandleStateChanged;
            sharedCorrectAnswers.OnValueChanged += HandleScalarValueChanged;
            sharedIncorrectAnswers.OnValueChanged += HandleScalarValueChanged;
            totalScheduledCards.OnValueChanged += HandleScalarValueChanged;
            remainingTimeSeconds.OnValueChanged += HandleTimerChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusChanged;

            base.OnNetworkSpawn();
            BeginCardDefinitionLoad();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            playerProgressStates.OnListChanged -= HandleStateChanged;
            sharedCorrectAnswers.OnValueChanged -= HandleScalarValueChanged;
            sharedIncorrectAnswers.OnValueChanged -= HandleScalarValueChanged;
            totalScheduledCards.OnValueChanged -= HandleScalarValueChanged;
            remainingTimeSeconds.OnValueChanged -= HandleTimerChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusChanged;

            if (csvLoadingCoroutine != null)
            {
                StopCoroutine(csvLoadingCoroutine);
                csvLoadingCoroutine = null;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !serverGameplayActive || Stage != CooperativeMinigameStage.Playing || HasPublishedResult)
            {
                return;
            }

            var remainingSeconds = Mathf.Max(0f, (float)(gameplayEndServerTime - NetworkManager.ServerTime.Time));
            var wholeSecond = Mathf.CeilToInt(remainingSeconds);
            if (wholeSecond != lastPublishedWholeSecond)
            {
                remainingTimeSeconds.Value = remainingSeconds;
                lastPublishedWholeSecond = wholeSecond;
            }

            if (remainingSeconds <= 0f)
            {
                CompleteMinigameServer(completedAllCards: false);
            }
        }

        public GardenImageVotingPlayerProgressNetworkState? GetLocalProgressState()
        {
            return TryGetProgressState(GetLocalClientId(), out var state) ? state : null;
        }

        public GardenImageVotingCardDefinition GetLocalCurrentCard()
        {
            return GetCurrentCardForClient(GetLocalClientId());
        }

        public bool CanLocalPlayerSubmitDecision()
        {
            if (Stage != CooperativeMinigameStage.Playing || !hasLoadedCardDefinitions || !string.IsNullOrWhiteSpace(dataLoadError))
            {
                return false;
            }

            return GetLocalCurrentCard() != null;
        }

        public void SubmitLocalDecision(bool hasSeenImageInGarden)
        {
            if (!CanLocalPlayerSubmitDecision())
            {
                return;
            }

            if (IsServer)
            {
                SubmitDecisionServer(hasSeenImageInGarden, GetLocalClientId());
                return;
            }

            SubmitDecisionServerRpc(hasSeenImageInGarden);
        }

        protected override void InitializeMinigameServer()
        {
            sharedCorrectAnswers.Value = 0;
            sharedIncorrectAnswers.Value = 0;
            totalScheduledCards.Value = 0;
            remainingTimeSeconds.Value = gardenImageVotingMinigameConfig == null ? 0f : gardenImageVotingMinigameConfig.TimeLimitSeconds;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando el conjunto de imagenes compartidas.");
            playerProgressStates.Clear();
            serverDataPrepared = false;
            serverGameplayActive = false;
            pendingGameplayStart = false;

            if (gardenImageVotingMinigameConfig == null)
            {
                dataLoadError = $"{nameof(GardenImageVotingMinigameSession)} requiere una configuracion valida.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer("Configuracion invalida");
                return;
            }

            if (hasLoadedCardDefinitions)
            {
                TryPrepareServerData();
            }
        }

        protected override void OnGameplayStartedServer()
        {
            if (!serverDataPrepared)
            {
                pendingGameplayStart = true;
                sharedStatusMessage.Value = new FixedString128Bytes("Esperando a que el CSV quede listo para empezar.");
                return;
            }

            StartGameplayCountdownServer();
        }

        [Rpc(SendTo.Server)]
        private void SubmitDecisionServerRpc(bool hasSeenImageInGarden, RpcParams rpcParams = default)
        {
            SubmitDecisionServer(hasSeenImageInGarden, rpcParams.Receive.SenderClientId);
        }

        private void BeginCardDefinitionLoad()
        {
            if (csvLoadingCoroutine != null || gardenImageVotingMinigameConfig == null)
            {
                return;
            }

            csvLoadingCoroutine = StartCoroutine(LoadCardDefinitionsCoroutine());
        }

        private IEnumerator LoadCardDefinitionsCoroutine()
        {
            string csvContent = null;
            string loadError = string.Empty;

            yield return GardenImageVotingExternalContentService.LoadTextAsync(
                gardenImageVotingMinigameConfig.CsvRelativePath,
                (loadedContent, error) =>
                {
                    csvContent = loadedContent;
                    loadError = error;
                });

            csvLoadingCoroutine = null;
            hasLoadedCardDefinitions = false;
            cardAssignmentsByClient.Clear();
            loadedCardDefinitions.Clear();

            if (!string.IsNullOrWhiteSpace(loadError))
            {
                dataLoadError = $"No se ha podido cargar el CSV: {loadError}";
                Debug.LogError($"[GardenImageVoting] Error cargando CSV '{gardenImageVotingMinigameConfig.CsvRelativePath}': {loadError}", this);
                HandleLoadedCardDefinitionsFailure();
                yield break;
            }

            if (!GardenImageVotingCsvService.TryParse(
                    csvContent,
                    gardenImageVotingMinigameConfig.MaxSupportedDevices,
                    gardenImageVotingMinigameConfig.CardsPerDevice,
                    gardenImageVotingMinigameConfig.AllowRepeatedImagesAcrossDevices,
                    out var definitions,
                    out var parseError))
            {
                dataLoadError = parseError;
                Debug.LogError($"[GardenImageVoting] Error parseando CSV '{gardenImageVotingMinigameConfig.CsvRelativePath}': {parseError}", this);
                HandleLoadedCardDefinitionsFailure();
                yield break;
            }

            dataLoadError = string.Empty;
            hasLoadedCardDefinitions = true;
            var normalizedDefinitions = NormalizeDefinitionImagePaths(definitions);
            loadedCardDefinitions.AddRange(normalizedDefinitions);
            BuildCardAssignments(normalizedDefinitions);
            Debug.Log($"[GardenImageVoting] CSV cargado correctamente desde '{gardenImageVotingMinigameConfig.CsvRelativePath}'. Cartas: {normalizedDefinitions.Count}.", this);

            if (IsServer)
            {
                TryPrepareServerData();
            }

            StateChanged?.Invoke();
        }

        private void HandleLoadedCardDefinitionsFailure()
        {
            hasLoadedCardDefinitions = false;
            loadedCardDefinitions.Clear();
            serverDataPrepared = false;
            serverGameplayActive = false;
            pendingGameplayStart = false;
            totalScheduledCards.Value = 0;
            remainingTimeSeconds.Value = 0f;
            sharedStatusMessage.Value = new FixedString128Bytes(string.IsNullOrWhiteSpace(dataLoadError)
                ? "No se ha podido preparar el contenido del minijuego."
                : dataLoadError);

            if (IsServer && !HasPublishedResult)
            {
                SetBlockingErrorServer(string.IsNullOrWhiteSpace(dataLoadError) ? "CSV invalido" : dataLoadError);
            }

            StateChanged?.Invoke();
        }

        private List<GardenImageVotingCardDefinition> NormalizeDefinitionImagePaths(IReadOnlyList<GardenImageVotingCardDefinition> definitions)
        {
            var normalizedDefinitions = new List<GardenImageVotingCardDefinition>(definitions.Count);
            var csvRelativePath = gardenImageVotingMinigameConfig == null ? string.Empty : gardenImageVotingMinigameConfig.CsvRelativePath;

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                var resolvedImagePath = string.IsNullOrWhiteSpace(definition.ImagePath)
                    ? string.Empty
                    : GardenImageVotingExternalContentService.ResolveConfiguredPath(definition.ImagePath, csvRelativePath);

                normalizedDefinitions.Add(new GardenImageVotingCardDefinition(
                    definition.RoundIndex,
                    definition.DeviceSlot,
                    definition.Topic,
                    definition.Title,
                    resolvedImagePath,
                    definition.IsSeenInGarden));
            }

            return normalizedDefinitions;
        }

        private void BuildCardAssignments(IReadOnlyList<GardenImageVotingCardDefinition> definitions)
        {
            cardAssignmentsByClient.Clear();

            var participantIds = GetParticipantIds();
            foreach (var participantId in participantIds)
            {
                var deviceSlot = GetDeviceSlotForClient(participantId);
                if (deviceSlot <= 0)
                {
                    continue;
                }

                var clientCards = new List<GardenImageVotingCardDefinition>();
                for (var index = 0; index < definitions.Count; index++)
                {
                    var definition = definitions[index];
                    if (definition.DeviceSlot == deviceSlot)
                    {
                        clientCards.Add(definition);
                    }
                }

                cardAssignmentsByClient[participantId] = clientCards;
            }
        }

        private void TryPrepareServerData()
        {
            if (!IsServer || !hasLoadedCardDefinitions || serverDataPrepared || HasPublishedResult)
            {
                return;
            }

            playerProgressStates.Clear();
            var assignedCardCount = 0;

            foreach (var participantId in GetParticipantIds())
            {
                var hasCards = cardAssignmentsByClient.TryGetValue(participantId, out var cards) && cards.Count > 0;
                if (hasCards)
                {
                    assignedCardCount += cards.Count;
                }

                playerProgressStates.Add(new GardenImageVotingPlayerProgressNetworkState
                {
                    ClientId = participantId,
                    CurrentCardIndex = 0,
                    CorrectAnswers = 0,
                    IncorrectAnswers = 0,
                    HasCompleted = !hasCards
                });
            }

            totalScheduledCards.Value = assignedCardCount;
            remainingTimeSeconds.Value = gardenImageVotingMinigameConfig.TimeLimitSeconds;
            sharedStatusMessage.Value = new FixedString128Bytes("Desliza a la derecha si la has visto y a la izquierda si no.");
            serverDataPrepared = true;

            if (assignedCardCount <= 0)
            {
                dataLoadError = "No hay cartas disponibles para los participantes actuales.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer(dataLoadError);
                return;
            }

            if (pendingGameplayStart || Stage == CooperativeMinigameStage.Playing)
            {
                StartGameplayCountdownServer();
            }

            StateChanged?.Invoke();
        }

        private void StartGameplayCountdownServer()
        {
            if (!IsServer || !serverDataPrepared || serverGameplayActive || gardenImageVotingMinigameConfig == null || HasPublishedResult)
            {
                return;
            }

            pendingGameplayStart = false;
            serverGameplayActive = true;
            gameplayEndServerTime = NetworkManager.ServerTime.Time + gardenImageVotingMinigameConfig.TimeLimitSeconds;
            remainingTimeSeconds.Value = gardenImageVotingMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = Mathf.CeilToInt(gardenImageVotingMinigameConfig.TimeLimitSeconds);
            sharedStatusMessage.Value = new FixedString128Bytes("La ronda ha empezado. Decide rapidamente cada imagen.");
            StateChanged?.Invoke();
        }

        private void SubmitDecisionServer(bool hasSeenImageInGarden, ulong clientId)
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing || !serverGameplayActive || !serverDataPrepared || HasPublishedResult)
            {
                return;
            }

            if (!TryGetProgressStateIndex(clientId, out var progressIndex))
            {
                return;
            }

            if (!cardAssignmentsByClient.TryGetValue(clientId, out var assignedCards))
            {
                return;
            }

            var progressState = playerProgressStates[progressIndex];
            if (progressState.HasCompleted || progressState.CurrentCardIndex < 0 || progressState.CurrentCardIndex >= assignedCards.Count)
            {
                return;
            }

            var currentCard = assignedCards[progressState.CurrentCardIndex];
            var wasCorrect = currentCard.IsSeenInGarden == hasSeenImageInGarden;

            if (wasCorrect)
            {
                progressState.CorrectAnswers += 1;
                sharedCorrectAnswers.Value += 1;
            }
            else
            {
                progressState.IncorrectAnswers += 1;
                sharedIncorrectAnswers.Value += 1;
                ApplyIncorrectAnswerPenaltyServer();
            }

            progressState.CurrentCardIndex += 1;
            progressState.HasCompleted = progressState.CurrentCardIndex >= assignedCards.Count;
            playerProgressStates[progressIndex] = progressState;

            sharedStatusMessage.Value = wasCorrect
                ? new FixedString128Bytes("Respuesta correcta. El grupo suma un punto.")
                : new FixedString128Bytes("Respuesta incorrecta. El grupo no suma en esta imagen.");

            if (remainingTimeSeconds.Value <= 0f)
            {
                CompleteMinigameServer(completedAllCards: false);
                return;
            }

            if (HaveAllPlayersCompleted())
            {
                CompleteMinigameServer(completedAllCards: true);
            }
        }

        private void ApplyIncorrectAnswerPenaltyServer()
        {
            if (gardenImageVotingMinigameConfig == null || gardenImageVotingMinigameConfig.IncorrectAnswerPenaltySeconds <= 0f)
            {
                return;
            }

            var currentServerTime = NetworkManager.ServerTime.Time;
            gameplayEndServerTime = Math.Max(currentServerTime, gameplayEndServerTime - gardenImageVotingMinigameConfig.IncorrectAnswerPenaltySeconds);
            remainingTimeSeconds.Value = Mathf.Max(0f, (float)(gameplayEndServerTime - currentServerTime));
            lastPublishedWholeSecond = Mathf.CeilToInt(remainingTimeSeconds.Value);
        }

        private void CompleteMinigameServer(bool completedAllCards)
        {
            if (!IsServer || HasPublishedResult)
            {
                return;
            }

            serverGameplayActive = false;
            remainingTimeSeconds.Value = Mathf.Max(0f, remainingTimeSeconds.Value);
            PublishResultServer(GardenImageVotingScoreService.CreateResult(
                gardenImageVotingMinigameConfig,
                sharedCorrectAnswers.Value,
                sharedIncorrectAnswers.Value,
                totalScheduledCards.Value,
                completedAllCards));
        }

        private bool HaveAllPlayersCompleted()
        {
            for (var index = 0; index < playerProgressStates.Count; index++)
            {
                if (!playerProgressStates[index].HasCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private GardenImageVotingCardDefinition GetCurrentCardForClient(ulong clientId)
        {
            if (!hasLoadedCardDefinitions || !cardAssignmentsByClient.TryGetValue(clientId, out var assignedCards))
            {
                if (hasLoadedCardDefinitions && loadedCardDefinitions.Count > 0)
                {
                    BuildCardAssignments(loadedCardDefinitions);
                }

                if (!cardAssignmentsByClient.TryGetValue(clientId, out assignedCards))
                {
                    return null;
                }
            }

            if (assignedCards == null)
            {
                return null;
            }

            if (!TryGetProgressState(clientId, out var progressState))
            {
                return null;
            }

            var currentCardIndex = progressState.CurrentCardIndex;
            return currentCardIndex >= 0 && currentCardIndex < assignedCards.Count ? assignedCards[currentCardIndex] : null;
        }

        private bool TryGetProgressState(ulong clientId, out GardenImageVotingPlayerProgressNetworkState state)
        {
            state = default;
            if (!TryGetProgressStateIndex(clientId, out var stateIndex))
            {
                return false;
            }

            state = playerProgressStates[stateIndex];
            return true;
        }

        private bool TryGetProgressStateIndex(ulong clientId, out int stateIndex)
        {
            for (var index = 0; index < playerProgressStates.Count; index++)
            {
                if (playerProgressStates[index].ClientId == clientId)
                {
                    stateIndex = index;
                    return true;
                }
            }

            stateIndex = -1;
            return false;
        }

        private int GetDeviceSlotForClient(ulong clientId)
        {
            if (SessionCoordinator != null)
            {
                var coordinatorSlot = SessionCoordinator.GetPlayerSlot(clientId);
                if (coordinatorSlot >= 0)
                {
                    return coordinatorSlot + 1;
                }
            }

            var participantIds = GetParticipantIds();
            for (var index = 0; index < participantIds.Count; index++)
            {
                if (participantIds[index] == clientId)
                {
                    return index + 1;
                }
            }

            return -1;
        }

        private void HandleStateChanged(NetworkListEvent<GardenImageVotingPlayerProgressNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleScalarValueChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleTimerChanged(float _, float __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }
    }
}
