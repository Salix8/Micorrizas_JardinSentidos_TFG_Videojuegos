using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessMinigameSession : CooperativeMinigameBase
    {
        private const ulong NoClientId = ulong.MaxValue;

        [SerializeField] private CollaborativePlantGuessMinigameConfig collaborativePlantGuessMinigameConfig;

        private readonly NetworkList<CollaborativePlantGuessHistoryEntryNetworkState> guessHistory = new();
        private readonly NetworkVariable<int> attemptsUsed = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<bool> isLeafPersistenceHintUnlocked = new();
        private readonly NetworkVariable<ulong> lastGuessingClientId = new(NoClientId);
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();
        private readonly NetworkVariable<FixedString128Bytes> revealedLeafPersistenceHint = new();
        private readonly NetworkVariable<FixedString128Bytes> revealedTargetPlantId = new();

        private readonly List<CollaborativePlantGuessPlantDefinition> loadedPlantDefinitions = new();

        private Coroutine csvLoadingCoroutine;
        private bool hasLoadedPlantDefinitions;
        private bool serverDataPrepared;
        private bool serverGameplayActive;
        private bool pendingGameplayStart;
        private string dataLoadError = string.Empty;
        private string targetPlantId = string.Empty;
        private double gameplayEndServerTime;
        private int assignmentSeed;
        private int lastPublishedWholeSecond = -1;

        public bool HasLoadedPlantDefinitions => hasLoadedPlantDefinitions;
        public string DataLoadError => dataLoadError;
        public int AttemptsUsed => attemptsUsed.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public bool IsLeafPersistenceHintUnlocked => isLeafPersistenceHintUnlocked.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();
        public string RevealedLeafPersistenceHint => revealedLeafPersistenceHint.Value.ToString();
        public string RevealedTargetPlantId => revealedTargetPlantId.Value.ToString();

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return collaborativePlantGuessMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            guessHistory.OnListChanged += HandleHistoryChanged;
            attemptsUsed.OnValueChanged += HandleIntChanged;
            remainingTimeSeconds.OnValueChanged += HandleFloatChanged;
            isLeafPersistenceHintUnlocked.OnValueChanged += HandleBoolChanged;
            lastGuessingClientId.OnValueChanged += HandleLastGuessingClientChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusChanged;
            revealedLeafPersistenceHint.OnValueChanged += HandleStringChanged;
            revealedTargetPlantId.OnValueChanged += HandleStringChanged;

            base.OnNetworkSpawn();
            BeginPlantDefinitionLoad();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            guessHistory.OnListChanged -= HandleHistoryChanged;
            attemptsUsed.OnValueChanged -= HandleIntChanged;
            remainingTimeSeconds.OnValueChanged -= HandleFloatChanged;
            isLeafPersistenceHintUnlocked.OnValueChanged -= HandleBoolChanged;
            lastGuessingClientId.OnValueChanged -= HandleLastGuessingClientChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusChanged;
            revealedLeafPersistenceHint.OnValueChanged -= HandleStringChanged;
            revealedTargetPlantId.OnValueChanged -= HandleStringChanged;

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
                RevealTargetPlantServer();
                CompleteMinigameServer(false, collaborativePlantGuessMinigameConfig == null ? "Tiempo agotado" : collaborativePlantGuessMinigameConfig.TimeoutMessage);
            }
        }

        public IReadOnlyList<CollaborativePlantGuessPlantDefinition> GetLoadedPlantDefinitions()
        {
            return loadedPlantDefinitions;
        }

        public IReadOnlyList<CollaborativePlantGuessHistoryEntryNetworkState> GetGuessHistory()
        {
            var historyEntries = new List<CollaborativePlantGuessHistoryEntryNetworkState>(guessHistory.Count);
            for (var index = 0; index < guessHistory.Count; index++)
            {
                historyEntries.Add(guessHistory[index]);
            }

            return historyEntries;
        }

        public bool TryGetPlantDefinition(string plantId, out CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            for (var index = 0; index < loadedPlantDefinitions.Count; index++)
            {
                if (string.Equals(loadedPlantDefinitions[index].PlantId, plantId, StringComparison.OrdinalIgnoreCase))
                {
                    plantDefinition = loadedPlantDefinitions[index];
                    return true;
                }
            }

            plantDefinition = null;
            return false;
        }

        public bool TryResolveLocalPlant(string rawInput, out CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            return CollaborativePlantGuessAutocompleteService.TryResolvePlant(loadedPlantDefinitions, rawInput, out plantDefinition);
        }

        public bool CanLocalSubmitGuess(string rawInput)
        {
            if (Stage != CooperativeMinigameStage.Playing ||
                !hasLoadedPlantDefinitions ||
                !string.IsNullOrWhiteSpace(dataLoadError) ||
                collaborativePlantGuessMinigameConfig == null ||
                attemptsUsed.Value >= collaborativePlantGuessMinigameConfig.MaxAttempts)
            {
                return false;
            }

            if (lastGuessingClientId.Value == GetLocalClientId() && attemptsUsed.Value > 0)
            {
                return false;
            }

            return TryResolveLocalPlant(rawInput, out _);
        }

        public void SubmitLocalGuess(string rawInput)
        {
            if (!CanLocalSubmitGuess(rawInput))
            {
                return;
            }

            if (IsServer)
            {
                SubmitGuessServer(rawInput, GetLocalClientId());
                return;
            }

            SubmitGuessServerRpc(rawInput);
        }

        public int GetPlayerDisplaySlot(ulong clientId)
        {
            if (SessionCoordinator != null)
            {
                var slot = SessionCoordinator.GetPlayerSlot(clientId);
                if (slot >= 0)
                {
                    return slot + 1;
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

        protected override void InitializeMinigameServer()
        {
            assignmentSeed = Environment.TickCount;
            attemptsUsed.Value = 0;
            remainingTimeSeconds.Value = collaborativePlantGuessMinigameConfig == null ? 0f : collaborativePlantGuessMinigameConfig.TimeLimitSeconds;
            isLeafPersistenceHintUnlocked.Value = false;
            lastGuessingClientId.Value = NoClientId;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando el conjunto de plantas compartidas.");
            revealedLeafPersistenceHint.Value = default;
            revealedTargetPlantId.Value = default;
            guessHistory.Clear();
            targetPlantId = string.Empty;
            serverDataPrepared = false;
            serverGameplayActive = false;
            pendingGameplayStart = false;

            var participantCount = GetParticipantIds().Count;
            if (collaborativePlantGuessMinigameConfig == null)
            {
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            if (participantCount < collaborativePlantGuessMinigameConfig.MinimumSupportedPlayers ||
                participantCount > collaborativePlantGuessMinigameConfig.MaxSupportedDevices)
            {
                PublishResultServer(new MinigameResultData("Numero de jugadores no compatible", 0f, 0, 0));
                return;
            }

            if (hasLoadedPlantDefinitions)
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

            StartGameplayServer();
        }

        [Rpc(SendTo.Server)]
        private void SubmitGuessServerRpc(string rawGuess, RpcParams rpcParams = default)
        {
            SubmitGuessServer(rawGuess, rpcParams.Receive.SenderClientId);
        }

        private void BeginPlantDefinitionLoad()
        {
            if (csvLoadingCoroutine != null || collaborativePlantGuessMinigameConfig == null)
            {
                return;
            }

            csvLoadingCoroutine = StartCoroutine(LoadPlantDefinitionsCoroutine());
        }

        private IEnumerator LoadPlantDefinitionsCoroutine()
        {
            string csvContent = null;
            string loadError = string.Empty;

            yield return CoopMinigameExternalContentService.LoadTextAsync(
                collaborativePlantGuessMinigameConfig.CsvRelativePath,
                (loadedContent, error) =>
                {
                    csvContent = loadedContent;
                    loadError = error;
                });

            csvLoadingCoroutine = null;
            loadedPlantDefinitions.Clear();
            hasLoadedPlantDefinitions = false;

            if (!string.IsNullOrWhiteSpace(loadError))
            {
                dataLoadError = $"No se ha podido cargar el CSV: {loadError}";
                HandleLoadedPlantDefinitionsFailure();
                yield break;
            }

            if (!CollaborativePlantGuessCsvService.TryParse(csvContent, out var plantDefinitions, out var parseError))
            {
                dataLoadError = parseError;
                HandleLoadedPlantDefinitionsFailure();
                yield break;
            }

            dataLoadError = string.Empty;
            hasLoadedPlantDefinitions = true;
            loadedPlantDefinitions.AddRange(plantDefinitions);

            if (IsServer)
            {
                TryPrepareServerData();
            }

            StateChanged?.Invoke();
        }

        private void HandleLoadedPlantDefinitionsFailure()
        {
            if (IsServer && !HasPublishedResult)
            {
                PublishResultServer(new MinigameResultData("CSV invalido", 0f, 0, 0));
            }

            StateChanged?.Invoke();
        }

        private void TryPrepareServerData()
        {
            if (!IsServer || !hasLoadedPlantDefinitions || serverDataPrepared || HasPublishedResult)
            {
                return;
            }

            var random = new System.Random(assignmentSeed++);
            targetPlantId = loadedPlantDefinitions[random.Next(loadedPlantDefinitions.Count)].PlantId;
            sharedStatusMessage.Value = new FixedString128Bytes("La partida ha empezado. Coordinaos para proponer plantas.");
            serverDataPrepared = true;

            if (pendingGameplayStart || Stage == CooperativeMinigameStage.Playing)
            {
                StartGameplayServer();
            }

            StateChanged?.Invoke();
        }

        private void StartGameplayServer()
        {
            if (!IsServer || !serverDataPrepared || serverGameplayActive || collaborativePlantGuessMinigameConfig == null || HasPublishedResult)
            {
                return;
            }

            pendingGameplayStart = false;
            serverGameplayActive = true;
            gameplayEndServerTime = NetworkManager.ServerTime.Time + collaborativePlantGuessMinigameConfig.TimeLimitSeconds;
            remainingTimeSeconds.Value = collaborativePlantGuessMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = Mathf.CeilToInt(collaborativePlantGuessMinigameConfig.TimeLimitSeconds);
            sharedStatusMessage.Value = new FixedString128Bytes("La partida ha empezado. Un dispositivo escribe y el resto observa el historial compartido.");
            StateChanged?.Invoke();
        }

        private void SubmitGuessServer(string rawGuess, ulong senderClientId)
        {
            if (!IsServer ||
                Stage != CooperativeMinigameStage.Playing ||
                !serverGameplayActive ||
                !serverDataPrepared ||
                HasPublishedResult ||
                collaborativePlantGuessMinigameConfig == null)
            {
                return;
            }

            if (attemptsUsed.Value > 0 && senderClientId == lastGuessingClientId.Value)
            {
                return;
            }

            if (!CollaborativePlantGuessAutocompleteService.TryResolvePlant(loadedPlantDefinitions, rawGuess, out var guessedPlant))
            {
                sharedStatusMessage.Value = new FixedString128Bytes(collaborativePlantGuessMinigameConfig.InvalidGuessMessage);
                return;
            }

            for (var index = 0; index < guessHistory.Count; index++)
            {
                if (string.Equals(guessHistory[index].PlantId.ToString(), guessedPlant.PlantId, StringComparison.OrdinalIgnoreCase))
                {
                    sharedStatusMessage.Value = new FixedString128Bytes("Esa planta ya se ha probado. Buscad otra opcion.");
                    return;
                }
            }

            if (!TryGetPlantDefinition(targetPlantId, out var targetPlant))
            {
                PublishResultServer(new MinigameResultData("Objetivo invalido", 0f, 0, attemptsUsed.Value));
                return;
            }

            var evaluation = CollaborativePlantGuessComparisonService.Evaluate(targetPlant, guessedPlant);
            attemptsUsed.Value += 1;
            lastGuessingClientId.Value = senderClientId;

            guessHistory.Add(new CollaborativePlantGuessHistoryEntryNetworkState
            {
                AttemptIndex = attemptsUsed.Value,
                GuessingClientId = senderClientId,
                PlantId = new FixedString128Bytes(guessedPlant.PlantId),
                LeafPersistenceOutcome = evaluation.LeafPersistenceOutcome,
                LeafSizeOutcome = evaluation.LeafSizeOutcome,
                LeafTextureOutcome = evaluation.LeafTextureOutcome,
                FruitTypeOutcome = evaluation.FruitTypeOutcome,
                IsExactPlantMatch = evaluation.IsExactPlantMatch
            });

            if (!isLeafPersistenceHintUnlocked.Value && attemptsUsed.Value >= collaborativePlantGuessMinigameConfig.HintRevealAttempt)
            {
                isLeafPersistenceHintUnlocked.Value = true;
                revealedLeafPersistenceHint.Value = new FixedString128Bytes(targetPlant.LeafPersistence);
            }

            if (evaluation.IsExactPlantMatch)
            {
                RevealTargetPlantServer();
                CompleteMinigameServer(true, collaborativePlantGuessMinigameConfig.SuccessMessage);
                return;
            }

            if (attemptsUsed.Value >= collaborativePlantGuessMinigameConfig.MaxAttempts)
            {
                RevealTargetPlantServer();
                CompleteMinigameServer(false, collaborativePlantGuessMinigameConfig.AttemptsExhaustedMessage);
                return;
            }

            sharedStatusMessage.Value = isLeafPersistenceHintUnlocked.Value
                ? new FixedString128Bytes($"Intento {attemptsUsed.Value}/{collaborativePlantGuessMinigameConfig.MaxAttempts}. Ya teneis la pista de hoja.")
                : new FixedString128Bytes($"Intento {attemptsUsed.Value}/{collaborativePlantGuessMinigameConfig.MaxAttempts}. Ahora debe responder otro dispositivo.");
        }

        private void RevealTargetPlantServer()
        {
            if (!TryGetPlantDefinition(targetPlantId, out var targetPlant))
            {
                return;
            }

            revealedTargetPlantId.Value = new FixedString128Bytes(targetPlant.PlantId);
            revealedLeafPersistenceHint.Value = new FixedString128Bytes(targetPlant.LeafPersistence);
            isLeafPersistenceHintUnlocked.Value = true;
        }

        private void CompleteMinigameServer(bool wasSolved, string resultMessage)
        {
            if (!IsServer || HasPublishedResult || collaborativePlantGuessMinigameConfig == null)
            {
                return;
            }

            serverGameplayActive = false;
            var decoratedResultMessage = resultMessage;
            if (TryGetPlantDefinition(targetPlantId, out var targetPlant))
            {
                decoratedResultMessage = $"{resultMessage}: {targetPlant.DisplayName}";
            }

            PublishResultServer(CollaborativePlantGuessScoreService.CreateResult(
                collaborativePlantGuessMinigameConfig,
                wasSolved,
                attemptsUsed.Value,
                collaborativePlantGuessMinigameConfig.MaxAttempts,
                decoratedResultMessage));
        }

        private void HandleHistoryChanged(NetworkListEvent<CollaborativePlantGuessHistoryEntryNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleIntChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleFloatChanged(float _, float __)
        {
            StateChanged?.Invoke();
        }

        private void HandleBoolChanged(bool _, bool __)
        {
            StateChanged?.Invoke();
        }

        private void HandleLastGuessingClientChanged(ulong _, ulong __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStringChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }
    }
}
