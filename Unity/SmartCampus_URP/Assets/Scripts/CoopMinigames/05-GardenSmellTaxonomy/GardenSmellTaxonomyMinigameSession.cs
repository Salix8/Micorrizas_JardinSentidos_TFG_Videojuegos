using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    [DisallowMultipleComponent]
    public sealed class GardenSmellTaxonomyMinigameSession : CooperativeMinigameBase
    {
        [SerializeField] private GardenSmellTaxonomyMinigameConfig gardenSmellTaxonomyMinigameConfig;

        private readonly NetworkList<GardenSmellTaxonomyClassificationEntryNetworkState> classificationHistory = new();
        private readonly NetworkVariable<int> correctAnswerCount = new();
        private readonly NetworkVariable<int> incorrectAnswerCount = new();
        private readonly NetworkVariable<int> totalScheduledPlants = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();
        private readonly NetworkVariable<FixedString64Bytes> currentPlantId = new();
        private readonly NetworkList<ulong> contentReadyClientIds = new();

        private readonly List<GardenSmellTaxonomyPlantDefinition> loadedDefinitions = new();
        private readonly List<GardenSmellTaxonomyPlantDefinition> scheduledDefinitions = new();

        private Coroutine csvLoadingCoroutine;
        private bool hasLoadedDefinitions;
        private bool serverDataPrepared;
        private bool serverGameplayActive;
        private bool pendingGameplayStart;
        private string dataLoadError = string.Empty;
        private int currentScheduledIndex;
        private double gameplayEndServerTime;
        private int lastPublishedWholeSecond = -1;
        private int schedulingSeed;

        public bool HasLoadedDefinitions => hasLoadedDefinitions;
        public string DataLoadError => dataLoadError;
        public int CorrectAnswerCount => correctAnswerCount.Value;
        public int IncorrectAnswerCount => incorrectAnswerCount.Value;
        public int AnsweredPlantCount => correctAnswerCount.Value + incorrectAnswerCount.Value;
        public int TotalScheduledPlants => totalScheduledPlants.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();
        public string CurrentPlantId => currentPlantId.Value.ToString();
        public int ContentReadyCount => contentReadyClientIds.Count;

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return gardenSmellTaxonomyMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            classificationHistory.OnListChanged += HandleHistoryChanged;
            correctAnswerCount.OnValueChanged += HandleScalarChanged;
            incorrectAnswerCount.OnValueChanged += HandleScalarChanged;
            totalScheduledPlants.OnValueChanged += HandleScalarChanged;
            remainingTimeSeconds.OnValueChanged += HandleScalarChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusChanged;
            currentPlantId.OnValueChanged += HandleCurrentPlantChanged;
            contentReadyClientIds.OnListChanged += HandleContentReadyChanged;

            base.OnNetworkSpawn();
            BeginPlantDefinitionLoad();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            classificationHistory.OnListChanged -= HandleHistoryChanged;
            correctAnswerCount.OnValueChanged -= HandleScalarChanged;
            incorrectAnswerCount.OnValueChanged -= HandleScalarChanged;
            totalScheduledPlants.OnValueChanged -= HandleScalarChanged;
            remainingTimeSeconds.OnValueChanged -= HandleScalarChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusChanged;
            currentPlantId.OnValueChanged -= HandleCurrentPlantChanged;
            contentReadyClientIds.OnListChanged -= HandleContentReadyChanged;

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
                CompleteMinigameServer(completedAllPlants: false);
            }
        }

        public IReadOnlyList<GardenSmellTaxonomyClassificationEntryNetworkState> GetClassificationHistory()
        {
            var historyEntries = new List<GardenSmellTaxonomyClassificationEntryNetworkState>(classificationHistory.Count);
            for (var index = 0; index < classificationHistory.Count; index++)
            {
                historyEntries.Add(classificationHistory[index]);
            }

            return historyEntries;
        }

        public bool TryGetPlantDefinition(string plantId, out GardenSmellTaxonomyPlantDefinition plantDefinition)
        {
            for (var index = 0; index < loadedDefinitions.Count; index++)
            {
                var currentDefinition = loadedDefinitions[index];
                if (string.Equals(currentDefinition.PlantId, plantId, StringComparison.OrdinalIgnoreCase))
                {
                    plantDefinition = currentDefinition;
                    return true;
                }
            }

            plantDefinition = null;
            return false;
        }

        public GardenSmellTaxonomyPlantDefinition GetCurrentPlantDefinition()
        {
            return TryGetPlantDefinition(CurrentPlantId, out var definition) ? definition : null;
        }

        public bool CanLocalSubmitClassification()
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   hasLoadedDefinitions &&
                   string.IsNullOrWhiteSpace(dataLoadError) &&
                   !string.IsNullOrWhiteSpace(CurrentPlantId);
        }

        public void SubmitLocalClassification(GardenSmellTaxonomyCategory selectedCategory)
        {
            if (!CanLocalSubmitClassification())
            {
                return;
            }

            var plantId = CurrentPlantId;
            if (IsServer)
            {
                SubmitClassificationServer(plantId, selectedCategory, GetLocalClientId());
                return;
            }

            SubmitClassificationServerRpc(plantId, selectedCategory);
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
            classificationHistory.Clear();
            correctAnswerCount.Value = 0;
            incorrectAnswerCount.Value = 0;
            totalScheduledPlants.Value = 0;
            remainingTimeSeconds.Value = gardenSmellTaxonomyMinigameConfig == null ? 0f : gardenSmellTaxonomyMinigameConfig.TimeLimitSeconds;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando la taxonomia compartida.");
            currentPlantId.Value = default;
            currentScheduledIndex = 0;
            serverDataPrepared = false;
            serverGameplayActive = false;
            pendingGameplayStart = false;
            schedulingSeed = Environment.TickCount;
            scheduledDefinitions.Clear();
            contentReadyClientIds.Clear();

            var participantCount = GetParticipantIds().Count;
            if (gardenSmellTaxonomyMinigameConfig == null)
            {
                dataLoadError = "Configuracion invalida.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer("Configuracion invalida");
                return;
            }

            if (participantCount < gardenSmellTaxonomyMinigameConfig.MinimumSupportedPlayers ||
                participantCount > gardenSmellTaxonomyMinigameConfig.MaxSupportedDevices)
            {
                dataLoadError = "Numero de jugadores no compatible.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer("Numero de jugadores no compatible");
                return;
            }

            RegisterLocalContentReadyIfPossible();
            TryPrepareServerData();
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
        private void SubmitClassificationServerRpc(string plantId, GardenSmellTaxonomyCategory selectedCategory, RpcParams rpcParams = default)
        {
            SubmitClassificationServer(plantId, selectedCategory, rpcParams.Receive.SenderClientId);
        }

        private void BeginPlantDefinitionLoad()
        {
            if (csvLoadingCoroutine != null || gardenSmellTaxonomyMinigameConfig == null)
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
                gardenSmellTaxonomyMinigameConfig.CsvRelativePath,
                (loadedContent, error) =>
                {
                    csvContent = loadedContent;
                    loadError = error;
                });

            csvLoadingCoroutine = null;
            loadedDefinitions.Clear();
            hasLoadedDefinitions = false;

            if (!string.IsNullOrWhiteSpace(loadError))
            {
                dataLoadError = $"No se ha podido cargar el CSV: {loadError}";
                HandleLoadedDefinitionsFailure();
                yield break;
            }

            if (!GardenSmellTaxonomyCsvService.TryParse(csvContent, out var parsedDefinitions, out var parseError))
            {
                dataLoadError = parseError;
                HandleLoadedDefinitionsFailure();
                yield break;
            }

            dataLoadError = string.Empty;
            hasLoadedDefinitions = true;

            for (var index = 0; index < parsedDefinitions.Count; index++)
            {
                var definition = parsedDefinitions[index];
                var resolvedImagePath = CoopMinigameExternalContentService.ResolveConfiguredPath(
                    definition.ImagePath,
                    gardenSmellTaxonomyMinigameConfig.CsvRelativePath);

                loadedDefinitions.Add(new GardenSmellTaxonomyPlantDefinition(
                    definition.PlantId,
                    definition.CommonName,
                    definition.ScientificName,
                    resolvedImagePath,
                    definition.CorrectCategory));
            }

            RegisterLocalContentReadyIfPossible();

            StateChanged?.Invoke();
        }

        private void HandleLoadedDefinitionsFailure()
        {
            hasLoadedDefinitions = false;
            loadedDefinitions.Clear();

            if (IsServer && !HasPublishedResult)
            {
                sharedStatusMessage.Value = new FixedString128Bytes(string.IsNullOrWhiteSpace(dataLoadError) ? "CSV invalido." : dataLoadError);
                SetBlockingErrorServer(string.IsNullOrWhiteSpace(dataLoadError) ? "CSV invalido" : dataLoadError);
            }

            StateChanged?.Invoke();
        }

        private void TryPrepareServerData()
        {
            if (!IsServer || !hasLoadedDefinitions || serverDataPrepared || HasPublishedResult || HasBlockingError || gardenSmellTaxonomyMinigameConfig == null)
            {
                return;
            }

            if (!AreAllParticipantsContentReady())
            {
                sharedStatusMessage.Value = new FixedString128Bytes($"Esperando imagenes locales: {contentReadyClientIds.Count}/{GetParticipantIds().Count}");
                return;
            }

            if (loadedDefinitions.Count < gardenSmellTaxonomyMinigameConfig.MinimumRequiredPlants)
            {
                dataLoadError = "CSV insuficiente.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer(dataLoadError);
                return;
            }

            scheduledDefinitions.Clear();
            scheduledDefinitions.AddRange(GardenSmellTaxonomySequenceService.BuildSchedule(
                loadedDefinitions,
                gardenSmellTaxonomyMinigameConfig.MaxPlantsPerMatch,
                gardenSmellTaxonomyMinigameConfig.ShufflePlants,
                schedulingSeed++));

            if (scheduledDefinitions.Count < gardenSmellTaxonomyMinigameConfig.MinimumRequiredPlants)
            {
                dataLoadError = "CSV insuficiente.";
                sharedStatusMessage.Value = new FixedString128Bytes(dataLoadError);
                SetBlockingErrorServer(dataLoadError);
                return;
            }

            currentScheduledIndex = 0;
            totalScheduledPlants.Value = scheduledDefinitions.Count;
            serverDataPrepared = true;
            sharedStatusMessage.Value = new FixedString128Bytes("Secuencia compartida lista. Arrastra cada planta hacia su uso principal.");

            if (pendingGameplayStart)
            {
                StartGameplayServer();
            }
        }

        private void StartGameplayServer()
        {
            if (!serverDataPrepared || gardenSmellTaxonomyMinigameConfig == null || scheduledDefinitions.Count == 0 || HasBlockingError)
            {
                return;
            }

            pendingGameplayStart = false;
            serverGameplayActive = true;
            gameplayEndServerTime = NetworkManager.ServerTime.Time + gardenSmellTaxonomyMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = -1;
            remainingTimeSeconds.Value = gardenSmellTaxonomyMinigameConfig.TimeLimitSeconds;
            SetCurrentPlantServer(0);
        }

        private void SubmitClassificationServer(string submittedPlantId, GardenSmellTaxonomyCategory selectedCategory, ulong senderClientId)
        {
            if (!IsServer ||
                Stage != CooperativeMinigameStage.Playing ||
                !serverDataPrepared ||
                !serverGameplayActive ||
                currentScheduledIndex < 0 ||
                currentScheduledIndex >= scheduledDefinitions.Count)
            {
                return;
            }

            var currentDefinition = scheduledDefinitions[currentScheduledIndex];
            if (!string.Equals(currentDefinition.PlantId, submittedPlantId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var isCorrect = currentDefinition.CorrectCategory == selectedCategory;
            if (isCorrect)
            {
                correctAnswerCount.Value += 1;
            }
            else
            {
                incorrectAnswerCount.Value += 1;
            }

            classificationHistory.Add(new GardenSmellTaxonomyClassificationEntryNetworkState
            {
                PlantId = new FixedString64Bytes(currentDefinition.PlantId),
                ScientificName = new FixedString128Bytes(currentDefinition.ScientificName),
                ChosenCategory = selectedCategory,
                CorrectCategory = currentDefinition.CorrectCategory,
                IsCorrect = isCorrect,
                SubmittedByClientId = senderClientId
            });

            var playerSlot = GetPlayerDisplaySlot(senderClientId);
            var correctCategoryLabel = GardenSmellTaxonomyCategoryLabels.GetDisplayName(currentDefinition.CorrectCategory);
            sharedStatusMessage.Value = new FixedString128Bytes(
                isCorrect
                    ? $"Jugador {Mathf.Max(1, playerSlot)} acierta: {currentDefinition.ScientificName} en {correctCategoryLabel}."
                    : $"Jugador {Mathf.Max(1, playerSlot)} falla: {currentDefinition.ScientificName} pertenece a {correctCategoryLabel}.");

            currentScheduledIndex++;
            if (currentScheduledIndex >= scheduledDefinitions.Count)
            {
                CompleteMinigameServer(completedAllPlants: true);
                return;
            }

            SetCurrentPlantServer(currentScheduledIndex);
        }

        private void SetCurrentPlantServer(int scheduledIndex)
        {
            if (!IsServer || scheduledIndex < 0 || scheduledIndex >= scheduledDefinitions.Count)
            {
                currentPlantId.Value = default;
                return;
            }

            currentPlantId.Value = new FixedString64Bytes(scheduledDefinitions[scheduledIndex].PlantId);
        }

        private void RegisterLocalContentReadyIfPossible()
        {
            if (!hasLoadedDefinitions || !string.IsNullOrWhiteSpace(dataLoadError))
            {
                return;
            }

            if (IsServer)
            {
                RegisterContentReadyServer(GetLocalClientId());
                return;
            }

            ReportContentReadyServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void ReportContentReadyServerRpc(RpcParams rpcParams = default)
        {
            RegisterContentReadyServer(rpcParams.Receive.SenderClientId);
        }

        private void RegisterContentReadyServer(ulong clientId)
        {
            if (!IsServer || IsContentReadyByClient(clientId))
            {
                return;
            }

            contentReadyClientIds.Add(clientId);
            TryPrepareServerData();
        }

        private bool AreAllParticipantsContentReady()
        {
            var participantIds = GetParticipantIds();
            if (participantIds.Count <= 0)
            {
                return false;
            }

            for (var index = 0; index < participantIds.Count; index++)
            {
                if (!IsContentReadyByClient(participantIds[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsContentReadyByClient(ulong clientId)
        {
            for (var index = 0; index < contentReadyClientIds.Count; index++)
            {
                if (contentReadyClientIds[index] == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        private void CompleteMinigameServer(bool completedAllPlants)
        {
            if (!IsServer || HasPublishedResult)
            {
                return;
            }

            serverGameplayActive = false;
            currentPlantId.Value = default;
            PublishResultServer(GardenSmellTaxonomyScoreService.CreateResult(
                gardenSmellTaxonomyMinigameConfig,
                correctAnswerCount.Value,
                incorrectAnswerCount.Value,
                totalScheduledPlants.Value,
                completedAllPlants));
        }

        private void HandleHistoryChanged(NetworkListEvent<GardenSmellTaxonomyClassificationEntryNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleScalarChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleScalarChanged(float _, float __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }

        private void HandleCurrentPlantChanged(FixedString64Bytes _, FixedString64Bytes __)
        {
            StateChanged?.Invoke();
        }

        private void HandleContentReadyChanged(NetworkListEvent<ulong> _)
        {
            if (IsServer)
            {
                TryPrepareServerData();
            }

            StateChanged?.Invoke();
        }
    }
}
