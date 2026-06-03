using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    [DisallowMultipleComponent]
    public sealed class PlantPhotoRelayMinigameSession : CooperativeMinigameBase
    {
        [SerializeField] private PlantPhotoRelayMinigameConfig plantPhotoRelayMinigameConfig;

        private readonly NetworkVariable<int> currentRoundIndex = new();
        private readonly NetworkVariable<int> successCount = new();
        private readonly NetworkVariable<int> failureCount = new();
        private readonly NetworkVariable<float> remainingPhaseTimeSeconds = new();
        private readonly NetworkVariable<ulong> photographerClientId = new();
        private readonly NetworkVariable<ulong> guesserClientId = new();
        private readonly NetworkVariable<int> photoWidth = new();
        private readonly NetworkVariable<int> photoHeight = new();
        private readonly NetworkVariable<PlantPhotoRelayPhase> activePhase = new();
        private readonly NetworkVariable<PlantPhotoRelayRoundResultNetworkState> currentRoundResult = new();
        private readonly NetworkVariable<FixedString512Bytes> clueText = new();
        private readonly NetworkVariable<FixedString512Bytes> statusText = new();
        private readonly NetworkVariable<FixedString128Bytes> photographerConfirmedCommonName = new();
        private readonly NetworkVariable<FixedString128Bytes> guesserConfirmedCommonName = new();
        private readonly NetworkVariable<FixedString128Bytes> targetCanonicalCommonName = new();
        private readonly NetworkVariable<FixedString512Bytes> dataLoadError = new();
        private readonly NetworkList<byte> sharedPhotoBytes = new();

        private readonly List<PlantPhotoRelayPlantDefinition> loadedPlantDefinitions = new();

        private Coroutine loadingCoroutine;
        private Coroutine localPhotoPreviewCoroutine;
        private Texture2D cachedSharedPhotoTexture;
        private IPlantPhotoCaptureService photoCaptureService;
        private bool hasLoadedCatalog;
        private bool serverGameplayReady;
        private bool pendingGameplayStart;
        private int selectedPlantIndexForRounds;
        private int lastWholeSecond = -1;
        private double currentPhaseEndServerTime;
        private float accumulatedScore;
        private bool localPhotoCaptureInProgress;

        public bool HasLoadedCatalog => hasLoadedCatalog;
        public string DataLoadError => dataLoadError.Value.ToString();
        public int CurrentRoundIndex => currentRoundIndex.Value;
        public int SuccessCount => successCount.Value;
        public int FailureCount => failureCount.Value;
        public float RemainingPhaseTimeSeconds => remainingPhaseTimeSeconds.Value;
        public ulong PhotographerClientId => photographerClientId.Value;
        public ulong GuesserClientId => guesserClientId.Value;
        public PlantPhotoRelayPhase ActivePhase => activePhase.Value;
        public string ClueText => clueText.Value.ToString();
        public string StatusText => statusText.Value.ToString();
        public string PhotographerConfirmedCommonName => photographerConfirmedCommonName.Value.ToString();
        public string GuesserConfirmedCommonName => guesserConfirmedCommonName.Value.ToString();
        public string TargetCanonicalCommonName => targetCanonicalCommonName.Value.ToString();
        public bool HasSharedPhoto => sharedPhotoBytes.Count > 0 && photoWidth.Value > 0 && photoHeight.Value > 0;
        public bool IsLocalPhotoCaptureInProgress => localPhotoCaptureInProgress;
        public Texture2D CachedSharedPhotoTexture => cachedSharedPhotoTexture;
        public PlantPhotoRelayRoundResultNetworkState CurrentRoundResult => currentRoundResult.Value;

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return plantPhotoRelayMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            currentRoundIndex.OnValueChanged += HandleStateChanged;
            successCount.OnValueChanged += HandleStateChanged;
            failureCount.OnValueChanged += HandleStateChanged;
            remainingPhaseTimeSeconds.OnValueChanged += HandleStateChanged;
            photographerClientId.OnValueChanged += HandleStateChanged;
            guesserClientId.OnValueChanged += HandleStateChanged;
            activePhase.OnValueChanged += HandleStateChanged;
            clueText.OnValueChanged += HandleStateChanged;
            statusText.OnValueChanged += HandleStateChanged;
            photographerConfirmedCommonName.OnValueChanged += HandleStateChanged;
            guesserConfirmedCommonName.OnValueChanged += HandleStateChanged;
            targetCanonicalCommonName.OnValueChanged += HandleStateChanged;
            dataLoadError.OnValueChanged += HandleStateChanged;
            photoWidth.OnValueChanged += HandleStateChanged;
            photoHeight.OnValueChanged += HandleStateChanged;
            currentRoundResult.OnValueChanged += HandleStateChanged;
            sharedPhotoBytes.OnListChanged += HandlePhotoBytesChanged;

            photoCaptureService = PlantPhotoRelayPhotoCaptureServiceFactory.CreateDefault();

            base.OnNetworkSpawn();
            BeginCatalogLoad();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            currentRoundIndex.OnValueChanged -= HandleStateChanged;
            successCount.OnValueChanged -= HandleStateChanged;
            failureCount.OnValueChanged -= HandleStateChanged;
            remainingPhaseTimeSeconds.OnValueChanged -= HandleStateChanged;
            photographerClientId.OnValueChanged -= HandleStateChanged;
            guesserClientId.OnValueChanged -= HandleStateChanged;
            activePhase.OnValueChanged -= HandleStateChanged;
            clueText.OnValueChanged -= HandleStateChanged;
            statusText.OnValueChanged -= HandleStateChanged;
            photographerConfirmedCommonName.OnValueChanged -= HandleStateChanged;
            guesserConfirmedCommonName.OnValueChanged -= HandleStateChanged;
            targetCanonicalCommonName.OnValueChanged -= HandleStateChanged;
            dataLoadError.OnValueChanged -= HandleStateChanged;
            photoWidth.OnValueChanged -= HandleStateChanged;
            photoHeight.OnValueChanged -= HandleStateChanged;
            currentRoundResult.OnValueChanged -= HandleStateChanged;
            sharedPhotoBytes.OnListChanged -= HandlePhotoBytesChanged;

            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }

            if (localPhotoPreviewCoroutine != null)
            {
                StopCoroutine(localPhotoPreviewCoroutine);
                localPhotoPreviewCoroutine = null;
            }

            DestroyCachedTexture();
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing || HasPublishedResult || !serverGameplayReady)
            {
                return;
            }

            var remaining = Mathf.Max(0f, (float)(currentPhaseEndServerTime - NetworkManager.ServerTime.Time));
            var wholeSecond = Mathf.CeilToInt(remaining);
            if (wholeSecond != lastWholeSecond)
            {
                remainingPhaseTimeSeconds.Value = remaining;
                lastWholeSecond = wholeSecond;
            }

            if (remaining > 0f)
            {
                return;
            }

            switch (activePhase.Value)
            {
                case PlantPhotoRelayPhase.Clue:
                    StartCapturePhaseServer();
                    break;
                case PlantPhotoRelayPhase.Capture:
                    FailRoundServer(PlantPhotoRelayRoundOutcome.FailedTimeout, plantPhotoRelayMinigameConfig.TimeoutMessage);
                    break;
                case PlantPhotoRelayPhase.Guess:
                    FailRoundServer(PlantPhotoRelayRoundOutcome.FailedTimeout, plantPhotoRelayMinigameConfig.TimeoutMessage);
                    break;
                case PlantPhotoRelayPhase.RoundResults:
                    AdvanceAfterRoundResultsServer();
                    break;
            }
        }

        protected override void InitializeMinigameServer()
        {
            currentRoundIndex.Value = 0;
            successCount.Value = 0;
            failureCount.Value = 0;
            remainingPhaseTimeSeconds.Value = 0f;
            activePhase.Value = PlantPhotoRelayPhase.Clue;
            statusText.Value = new FixedString512Bytes("Preparando el catalogo compartido.");
            clueText.Value = default;
            photographerConfirmedCommonName.Value = default;
            guesserConfirmedCommonName.Value = default;
            targetCanonicalCommonName.Value = default;
            dataLoadError.Value = default;
            currentRoundResult.Value = default;
            photoWidth.Value = 0;
            photoHeight.Value = 0;
            sharedPhotoBytes.Clear();
            selectedPlantIndexForRounds = 0;
            accumulatedScore = 0f;
            serverGameplayReady = false;
            pendingGameplayStart = false;
        }

        protected override void OnGameplayStartedServer()
        {
            if (!hasLoadedCatalog)
            {
                pendingGameplayStart = true;
                statusText.Value = new FixedString512Bytes("Esperando a que el catalogo quede listo.");
                return;
            }

            TryStartGameplayServer();
        }

        public IReadOnlyList<PlantPhotoRelayPlantDefinition> GetLoadedPlantDefinitions()
        {
            return loadedPlantDefinitions;
        }

        public bool IsLocalPhotographer()
        {
            return NetworkManager != null && NetworkManager.LocalClientId == photographerClientId.Value;
        }

        public bool IsLocalGuesser()
        {
            return NetworkManager != null && NetworkManager.LocalClientId == guesserClientId.Value;
        }

        public bool TryResolveLocalSelection(string rawInput, out PlantPhotoRelayPlantDefinition plantDefinition)
        {
            return PlantPhotoRelayAutocompleteService.TryResolvePlant(loadedPlantDefinitions, rawInput, out plantDefinition);
        }

        public IReadOnlyList<PlantPhotoRelayPlantDefinition> BuildLocalSuggestions(string rawInput)
        {
            return PlantPhotoRelayAutocompleteService.BuildSuggestions(
                loadedPlantDefinitions,
                rawInput,
                plantPhotoRelayMinigameConfig == null ? 6 : plantPhotoRelayMinigameConfig.MaxAutocompleteSuggestionCount);
        }

        public bool CanLocalCapturePhoto()
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   activePhase.Value == PlantPhotoRelayPhase.Capture &&
                   IsLocalPhotographer() &&
                   !localPhotoCaptureInProgress &&
                   !HasSharedPhoto;
        }

        public bool CanLocalConfirmPhotographerSelection(string rawInput)
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   activePhase.Value == PlantPhotoRelayPhase.Capture &&
                   IsLocalPhotographer() &&
                   HasSharedPhoto &&
                   string.IsNullOrWhiteSpace(PhotographerConfirmedCommonName) &&
                   PlantPhotoRelayCommonNameResolverService.TryResolveCanonicalCommonName(loadedPlantDefinitions, rawInput, out _);
        }

        public bool CanLocalSubmitGuess(string rawInput)
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   activePhase.Value == PlantPhotoRelayPhase.Guess &&
                   IsLocalGuesser() &&
                   string.IsNullOrWhiteSpace(GuesserConfirmedCommonName) &&
                   PlantPhotoRelayCommonNameResolverService.TryResolveCanonicalCommonName(loadedPlantDefinitions, rawInput, out _);
        }

        public void CapturePhotoLocally()
        {
            if (!CanLocalCapturePhoto())
            {
                return;
            }

            localPhotoCaptureInProgress = true;
            StateChanged?.Invoke();
            StartCoroutine(CapturePhotoCoroutine());
        }

        public void SubmitLocalPhotographerSelection(string rawInput)
        {
            if (!CanLocalConfirmPhotographerSelection(rawInput))
            {
                return;
            }

            PlantPhotoRelayCommonNameResolverService.TryResolveCanonicalCommonName(loadedPlantDefinitions, rawInput, out var canonicalName);
            SubmitPhotographerSelectionServerRpc(canonicalName);
        }

        public void SubmitLocalGuess(string rawInput)
        {
            if (!CanLocalSubmitGuess(rawInput))
            {
                return;
            }

            PlantPhotoRelayCommonNameResolverService.TryResolveCanonicalCommonName(loadedPlantDefinitions, rawInput, out var canonicalName);
            SubmitGuesserSelectionServerRpc(canonicalName);
        }

        private IEnumerator CapturePhotoCoroutine()
        {
            var request = new PlantPhotoRelayPhotoCaptureRequest(
                plantPhotoRelayMinigameConfig.TargetPhotoMaxDimension,
                plantPhotoRelayMinigameConfig.JpegQuality);
            PlantPhotoRelayPhotoCaptureResult? captureResult = null;
            yield return photoCaptureService.CapturePhotoAsync(request, result => captureResult = result);
            localPhotoCaptureInProgress = false;

            if (!captureResult.HasValue || !captureResult.Value.Success)
            {
                ReportCameraUnavailableServerRpc(captureResult.HasValue ? captureResult.Value.ErrorMessage : plantPhotoRelayMinigameConfig.CameraUnavailableMessage);
                StateChanged?.Invoke();
                yield break;
            }

            UploadSharedPhotoServerRpc(captureResult.Value.ImageBytes, captureResult.Value.Width, captureResult.Value.Height);
            StateChanged?.Invoke();
        }

        private void BeginCatalogLoad()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
            }

            loadingCoroutine = StartCoroutine(CooperativeLoadCatalogCoroutine());
        }

        private IEnumerator CooperativeLoadCatalogCoroutine()
        {
            string csvContent = null;
            string errorMessage = string.Empty;
            yield return CoopMinigameExternalContentService.LoadTextAsync(
                plantPhotoRelayMinigameConfig == null ? string.Empty : plantPhotoRelayMinigameConfig.CatalogCsvRelativePath,
                (text, error) =>
                {
                    csvContent = text;
                    errorMessage = error;
                });

            loadingCoroutine = null;
            loadedPlantDefinitions.Clear();

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                dataLoadError.Value = new FixedString512Bytes(errorMessage);
                StateChanged?.Invoke();
                yield break;
            }

            if (!PlantPhotoRelayCatalogService.TryParse(csvContent, out var definitions, out errorMessage))
            {
                dataLoadError.Value = new FixedString512Bytes(errorMessage);
                StateChanged?.Invoke();
                yield break;
            }

            loadedPlantDefinitions.AddRange(definitions);
            hasLoadedCatalog = true;
            dataLoadError.Value = default;

            if (IsServer && pendingGameplayStart)
            {
                TryStartGameplayServer();
            }

            StateChanged?.Invoke();
        }

        private void TryStartGameplayServer()
        {
            var participantIds = GetParticipantIds();
            if (plantPhotoRelayMinigameConfig == null)
            {
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            if (participantIds.Count < plantPhotoRelayMinigameConfig.MinimumSupportedPlayers ||
                participantIds.Count > plantPhotoRelayMinigameConfig.MaxSupportedDevices)
            {
                PublishResultServer(new MinigameResultData("Numero de jugadores no compatible", 0f, 0, 0));
                return;
            }

            serverGameplayReady = true;
            pendingGameplayStart = false;
            StartRoundServer(0);
        }

        private void StartRoundServer(int roundIndex)
        {
            currentRoundIndex.Value = roundIndex;
            currentRoundResult.Value = default;
            photographerConfirmedCommonName.Value = default;
            guesserConfirmedCommonName.Value = default;
            ClearSharedPhotoServer();

            var participants = GetParticipantIds();
            var roleAssignment = PlantPhotoRelayRoundAssignmentService.CreateAssignment(participants, roundIndex);
            photographerClientId.Value = roleAssignment.PhotographerId;
            guesserClientId.Value = roleAssignment.GuesserId;

            selectedPlantIndexForRounds = (roundIndex * 2) % loadedPlantDefinitions.Count;
            var targetPlant = loadedPlantDefinitions[selectedPlantIndexForRounds];
            targetCanonicalCommonName.Value = new FixedString128Bytes(targetPlant.CommonNameCanonical);
            clueText.Value = new FixedString512Bytes(PlantPhotoRelayPromptService.BuildPrompt(targetPlant));
            statusText.Value = new FixedString512Bytes($"Ronda {roundIndex + 1}/{plantPhotoRelayMinigameConfig.RoundCount}. Observad la pista antes de fotografiar.");
            BeginPhaseServer(PlantPhotoRelayPhase.Clue, plantPhotoRelayMinigameConfig.CluePhaseDurationSeconds);
        }

        private void StartCapturePhaseServer()
        {
            statusText.Value = new FixedString512Bytes("El dispositivo fotografo debe hacer la foto y confirmar la planta.");
            BeginPhaseServer(PlantPhotoRelayPhase.Capture, plantPhotoRelayMinigameConfig.CapturePhaseDurationSeconds);
        }

        private void StartGuessPhaseServer()
        {
            statusText.Value = new FixedString512Bytes("El dispositivo adivinador debe elegir el nombre comun de la foto.");
            BeginPhaseServer(PlantPhotoRelayPhase.Guess, plantPhotoRelayMinigameConfig.GuessPhaseDurationSeconds);
        }

        private void BeginRoundResultsPhaseServer(PlantPhotoRelayRoundOutcome outcome, string statusMessage, bool photographerMatchedPrompt)
        {
            currentRoundResult.Value = new PlantPhotoRelayRoundResultNetworkState
            {
                RoundIndex = currentRoundIndex.Value,
                Outcome = outcome,
                TargetCanonicalCommonName = new FixedString128Bytes(TargetCanonicalCommonName),
                PhotographerCanonicalCommonName = new FixedString128Bytes(PhotographerConfirmedCommonName),
                GuesserCanonicalCommonName = new FixedString128Bytes(GuesserConfirmedCommonName),
                PhotographerMatchedPrompt = photographerMatchedPrompt
            };

            if (outcome == PlantPhotoRelayRoundOutcome.Success)
            {
                successCount.Value++;
                accumulatedScore += PlantPhotoRelayScoreService.ComputeRoundScore(true, photographerMatchedPrompt, plantPhotoRelayMinigameConfig);
            }
            else
            {
                failureCount.Value++;
            }

            statusText.Value = new FixedString512Bytes(statusMessage);
            BeginPhaseServer(PlantPhotoRelayPhase.RoundResults, plantPhotoRelayMinigameConfig.ResultsRevealDurationSeconds);
        }

        private void FailRoundServer(PlantPhotoRelayRoundOutcome outcome, string statusMessage)
        {
            BeginRoundResultsPhaseServer(outcome, statusMessage, false);
        }

        private void AdvanceAfterRoundResultsServer()
        {
            var nextRoundIndex = currentRoundIndex.Value + 1;
            if (nextRoundIndex >= plantPhotoRelayMinigameConfig.RoundCount)
            {
                var finalScore = PlantPhotoRelayScoreService.ComputeFinalScore(plantPhotoRelayMinigameConfig.RoundCount, accumulatedScore, plantPhotoRelayMinigameConfig);
                PublishResultServer(new MinigameResultData(
                    plantPhotoRelayMinigameConfig.SuccessMessage,
                    finalScore,
                    successCount.Value,
                    plantPhotoRelayMinigameConfig.RoundCount));
                return;
            }

            StartRoundServer(nextRoundIndex);
        }

        private void BeginPhaseServer(PlantPhotoRelayPhase phase, float durationSeconds)
        {
            activePhase.Value = phase;
            remainingPhaseTimeSeconds.Value = durationSeconds;
            currentPhaseEndServerTime = NetworkManager.ServerTime.Time + durationSeconds;
            lastWholeSecond = -1;
        }

        private void ClearSharedPhotoServer()
        {
            sharedPhotoBytes.Clear();
            photoWidth.Value = 0;
            photoHeight.Value = 0;
        }

        [Rpc(SendTo.Server)]
        private void UploadSharedPhotoServerRpc(byte[] imageBytes, int width, int height, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != photographerClientId.Value ||
                activePhase.Value != PlantPhotoRelayPhase.Capture ||
                imageBytes == null ||
                imageBytes.Length == 0)
            {
                return;
            }

            sharedPhotoBytes.Clear();
            for (var index = 0; index < imageBytes.Length; index++)
            {
                sharedPhotoBytes.Add(imageBytes[index]);
            }

            photoWidth.Value = width;
            photoHeight.Value = height;
            statusText.Value = new FixedString512Bytes("Foto recibida. Falta confirmar la planta fotografiada.");
        }

        [Rpc(SendTo.Server)]
        private void SubmitPhotographerSelectionServerRpc(string canonicalCommonName, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != photographerClientId.Value ||
                activePhase.Value != PlantPhotoRelayPhase.Capture ||
                string.IsNullOrWhiteSpace(canonicalCommonName))
            {
                return;
            }

            photographerConfirmedCommonName.Value = new FixedString128Bytes(canonicalCommonName);
            StartGuessPhaseServer();
        }

        [Rpc(SendTo.Server)]
        private void SubmitGuesserSelectionServerRpc(string canonicalCommonName, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != guesserClientId.Value ||
                activePhase.Value != PlantPhotoRelayPhase.Guess ||
                string.IsNullOrWhiteSpace(canonicalCommonName))
            {
                return;
            }

            guesserConfirmedCommonName.Value = new FixedString128Bytes(canonicalCommonName);
            var exactMatch = string.Equals(canonicalCommonName, PhotographerConfirmedCommonName, StringComparison.OrdinalIgnoreCase);
            var photographerMatchedPrompt = string.Equals(PhotographerConfirmedCommonName, TargetCanonicalCommonName, StringComparison.OrdinalIgnoreCase);
            var outcome = exactMatch ? PlantPhotoRelayRoundOutcome.Success : PlantPhotoRelayRoundOutcome.FailedMismatch;
            var statusMessage = exactMatch
                ? "Acierto compartido: ambos dispositivos eligieron la misma planta."
                : "Fallo compartido: la foto y la adivinanza no coinciden.";
            BeginRoundResultsPhaseServer(outcome, statusMessage, photographerMatchedPrompt);
        }

        [Rpc(SendTo.Server)]
        private void ReportCameraUnavailableServerRpc(string message, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != photographerClientId.Value ||
                activePhase.Value != PlantPhotoRelayPhase.Capture)
            {
                return;
            }

            var statusMessage = string.IsNullOrWhiteSpace(message)
                ? plantPhotoRelayMinigameConfig.CameraUnavailableMessage
                : message;
            BeginRoundResultsPhaseServer(PlantPhotoRelayRoundOutcome.FailedCameraUnavailable, statusMessage, false);
        }

        private void HandleStateChanged<T>(T _, T __)
        {
            StateChanged?.Invoke();
        }

        private void HandlePhotoBytesChanged(NetworkListEvent<byte> _)
        {
            RefreshSharedPhotoTexture();
            StateChanged?.Invoke();
        }

        private void RefreshSharedPhotoTexture()
        {
            DestroyCachedTexture();
            if (!HasSharedPhoto)
            {
                return;
            }

            var bytes = new byte[sharedPhotoBytes.Count];
            for (var index = 0; index < sharedPhotoBytes.Count; index++)
            {
                bytes[index] = sharedPhotoBytes[index];
            }

            cachedSharedPhotoTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!cachedSharedPhotoTexture.LoadImage(bytes, true))
            {
                DestroyCachedTexture();
            }
        }

        private void DestroyCachedTexture()
        {
            if (cachedSharedPhotoTexture != null)
            {
                Destroy(cachedSharedPhotoTexture);
                cachedSharedPhotoTexture = null;
            }
        }
    }
}
