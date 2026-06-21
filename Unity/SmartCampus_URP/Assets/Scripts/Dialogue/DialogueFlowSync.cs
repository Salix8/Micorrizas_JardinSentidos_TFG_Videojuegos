using System;
using System.Collections.Generic;
using SmartCampus.Coop.Minigames;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueFlowSync : NetworkBehaviour
    {
        private enum PendingAction
        {
            None,
            StartMinigame,
            ContinueAfterResults
        }

        [Header("References")]
        [SerializeField] private DialogueFlowConfig flowConfig;
        [SerializeField] private CoopSessionCoordinator sessionCoordinator;
        [SerializeField] private CoopGpsStateSync gpsStateSync;
        [SerializeField] private CoopGpsMarkerController gpsMarkerController;

        [Header("Scene Flow")]
        [SerializeField] private string worldMapSceneName = "UJI";
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly Queue<string> pendingSequenceKeys = new();
        private readonly HashSet<ulong> awaitingClientIds = new();
        private readonly HashSet<ulong> activeSequenceClientIds = new();
        private readonly List<ulong> activePlayerIds = new();

        private DialogueUIController localDialogueController;
        private DialogueOpeningLoadingView localOpeningLoadingView;
        private DialogueGardenBoundary cachedGardenBoundary;
        private GameObject localDialogueCanvas;
        private Transform localSafeAreaRoot;
        private int localSequenceToken = -1;
        private int localFlowToken = -1;
        private int activeSequenceToken;
        private int nextSequenceToken = 1;
        private int localConfirmedPlayers;
        private int localTotalPlayers;
        private PendingAction pendingAction;
        private int pendingMinigameIndex = -1;
        private bool openingCompleted;
        private bool preparingOpening;
        private bool worldMapLoadCompleted;
        private float openingPreparationStartedAt;
        private float openingTransitionStartedAt;
        private float worldMapLoadCompletedAt;

        public DialogueFlowConfig FlowConfig => flowConfig;
        public bool IsFlowBusy => preparingOpening || pendingAction != PendingAction.None || awaitingClientIds.Count > 0;

        private void Awake()
        {
            ResolveReferences();
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer || openingCompleted || pendingAction != PendingAction.None || !worldMapLoadCompleted)
            {
                return;
            }

            ResolveReferences();
            if (sessionCoordinator == null ||
                sessionCoordinator.CurrentPhase != CoopGamePhase.WorldMap ||
                !string.Equals(SceneManager.GetActiveScene().name, worldMapSceneName, StringComparison.Ordinal))
            {
                return;
            }

            PrepareOpeningDialogue();
        }

        public override void OnNetworkSpawn()
        {
            ResolveReferences();
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                NetworkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            }

            ReleaseLocalDialogueController();
        }

        public void ResetSessionServer()
        {
            if (!IsServer)
            {
                return;
            }

            pendingSequenceKeys.Clear();
            awaitingClientIds.Clear();
            activeSequenceClientIds.Clear();
            pendingAction = PendingAction.None;
            pendingMinigameIndex = -1;
            openingCompleted = false;
            preparingOpening = false;
            worldMapLoadCompleted = false;
            cachedGardenBoundary = null;
            HideOpeningLoadingClientRpc();
        }

        public void BeginOpeningTransitionServer()
        {
            if (!IsServer)
            {
                return;
            }

            openingTransitionStartedAt = Time.unscaledTime;
            var showOpeningOverlay = ShouldUseDialogues() &&
                                     flowConfig != null &&
                                     flowConfig.ShowOpeningLoadingOverlay;
            var loadingText = flowConfig == null || string.IsNullOrWhiteSpace(flowConfig.OpeningLoadingText)
                ? "Cargando..."
                : flowConfig.OpeningLoadingText;
            SetOpeningLoadingClientRpc(
                showOpeningOverlay,
                new FixedString128Bytes(loadingText));
            LogOpeningTiming("Transición al mapa solicitada.");
        }

        public bool RequestStartMinigame(int minigameIndex)
        {
            ResolveReferences();
            if (!IsServer || sessionCoordinator == null || !sessionCoordinator.CanLaunchMiniGame(minigameIndex))
            {
                return false;
            }

            if (IsFlowBusy)
            {
                return false;
            }

            if (!ShouldUseDialogues() ||
                flowConfig == null ||
                !flowConfig.TryGetIntroductionKey(minigameIndex, out var introductionKey))
            {
                sessionCoordinator.StartMiniGame(minigameIndex);
                return true;
            }

            pendingSequenceKeys.Enqueue(introductionKey);
            pendingAction = PendingAction.StartMinigame;
            pendingMinigameIndex = minigameIndex;
            PlayNextSequenceOrComplete();
            return true;
        }

        public bool RequestContinueAfterResults()
        {
            ResolveReferences();
            if (!IsServer || sessionCoordinator == null)
            {
                return false;
            }

            if (IsFlowBusy)
            {
                return false;
            }

            var minigameIndex = sessionCoordinator.ActiveMiniGameIndex;
            if (ShouldUseDialogues() && flowConfig != null)
            {
                if (flowConfig.TryGetSuccessKey(minigameIndex, out var successKey))
                {
                    pendingSequenceKeys.Enqueue(successKey);
                }

                if (sessionCoordinator.AreAllConfiguredMinigamesCompleted() &&
                    !string.IsNullOrWhiteSpace(flowConfig.ReconnectionSequenceKey))
                {
                    pendingSequenceKeys.Enqueue(flowConfig.ReconnectionSequenceKey);
                }
            }

            if (pendingSequenceKeys.Count == 0)
            {
                sessionCoordinator.ContinueAfterMinigameResults();
                return true;
            }

            pendingAction = PendingAction.ContinueAfterResults;
            pendingMinigameIndex = minigameIndex;
            PlayNextSequenceOrComplete();
            return true;
        }

        private void PrepareOpeningDialogue()
        {
            if (!ShouldUseDialogues())
            {
                openingCompleted = true;
                preparingOpening = false;
                HideOpeningLoadingClientRpc();
                return;
            }

            if (!preparingOpening)
            {
                preparingOpening = true;
                openingPreparationStartedAt = Time.unscaledTime;
                LogOpeningTiming("Evaluación GPS iniciada tras LoadEventCompleted.");
            }

            ResolveWorldMapReferences();
            BuildActivePlayerIds();

            cachedGardenBoundary ??= FindFirstObjectByType<DialogueGardenBoundary>(FindObjectsInactive.Include);
            var resolvedPresence = DialogueGardenPresenceService.TryAreAllPlayersInside(
                activePlayerIds,
                gpsStateSync,
                gpsMarkerController,
                cachedGardenBoundary,
                out var areAllInside);
            var gpsTimeoutSeconds = flowConfig == null
                ? 0f
                : DialogueOpeningTimingService.ResolveGpsTimeout(
                    Application.isEditor,
                    flowConfig.GpsFixTimeoutSeconds,
                    flowConfig.EditorGpsFallbackSeconds);
            var timedOut = flowConfig == null ||
                           Time.unscaledTime - openingPreparationStartedAt >= gpsTimeoutSeconds;

            if (!resolvedPresence && !timedOut)
            {
                return;
            }

            var shouldShowReclaim = resolvedPresence
                ? !areAllInside
                : flowConfig == null || flowConfig.TreatMissingGpsAsOutsideGarden;

            if (shouldShowReclaim && flowConfig != null && !string.IsNullOrWhiteSpace(flowConfig.ReclaimSequenceKey))
            {
                pendingSequenceKeys.Enqueue(flowConfig.ReclaimSequenceKey);
            }

            if (flowConfig != null && !string.IsNullOrWhiteSpace(flowConfig.WarningSequenceKey))
            {
                pendingSequenceKeys.Enqueue(flowConfig.WarningSequenceKey);
            }

            preparingOpening = false;
            openingCompleted = true;
            HideOpeningLoadingClientRpc();
            LogOpeningTiming(
                resolvedPresence
                    ? $"Presencia resuelta. Todos dentro={areAllInside}."
                    : $"Fallback GPS aplicado tras {gpsTimeoutSeconds:0.00}s.");
            if (pendingSequenceKeys.Count > 0)
            {
                pendingAction = PendingAction.None;
                PlayNextSequenceOrComplete();
            }
            else
            {
                FinishSynchronizedFlowClientRpc();
            }
        }

        private void PlayNextSequenceOrComplete()
        {
            if (!IsServer)
            {
                return;
            }

            if (pendingSequenceKeys.Count == 0)
            {
                FinishSynchronizedFlowClientRpc();
                CompletePendingAction();
                return;
            }

            var sequenceKey = pendingSequenceKeys.Dequeue();
            if (string.IsNullOrWhiteSpace(sequenceKey))
            {
                PlayNextSequenceOrComplete();
                return;
            }

            activeSequenceToken = nextSequenceToken++;
            awaitingClientIds.Clear();
            activeSequenceClientIds.Clear();
            if (NetworkManager != null)
            {
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                {
                    awaitingClientIds.Add(clientId);
                    activeSequenceClientIds.Add(clientId);
                }
            }

            if (awaitingClientIds.Count == 0)
            {
                PlayNextSequenceOrComplete();
                return;
            }

            PlaySequenceClientRpc(
                new FixedString128Bytes(sequenceKey),
                activeSequenceToken,
                activeSequenceClientIds.Count);
        }

        [ClientRpc]
        private void PlaySequenceClientRpc(
            FixedString128Bytes sequenceKey,
            int sequenceToken,
            int totalPlayers)
        {
            HideLocalOpeningLoading();
            if (!TryGetLocalDialogueController(out var controller))
            {
                ConfirmSequenceServerRpc(sequenceToken);
                return;
            }

            localSequenceToken = sequenceToken;
            localFlowToken = sequenceToken;
            localConfirmedPlayers = 0;
            localTotalPlayers = totalPlayers;
            controller.PrepareSynchronizedSequence();
            controller.SequenceCompleted -= HandleLocalSequenceCompleted;
            controller.SequenceCompleted += HandleLocalSequenceCompleted;
            if (!controller.PlaySequence(sequenceKey.ToString()))
            {
                controller.SequenceCompleted -= HandleLocalSequenceCompleted;
                localSequenceToken = -1;
                ConfirmSequenceServerRpc(sequenceToken);
                return;
            }

            LogOpeningTiming($"Primera línea solicitada en cliente para '{sequenceKey}'.");
        }

        [ClientRpc]
        private void SetOpeningLoadingClientRpc(bool visible, FixedString128Bytes loadingText)
        {
            if (!visible)
            {
                HideLocalOpeningLoading();
                return;
            }

            EnsureLocalPresentationCanvas();
            if (localOpeningLoadingView == null && localDialogueCanvas != null)
            {
                var overlay = new GameObject(
                    "OpeningDialogueLoadingOverlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(DialogueOpeningLoadingView));
                overlay.transform.SetParent(localDialogueCanvas.transform, false);
                var overlayRect = overlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                localOpeningLoadingView = overlay.GetComponent<DialogueOpeningLoadingView>();
            }

            TryGetLocalDialogueController(out _);
            localOpeningLoadingView?.Show(loadingText.ToString());
            LogOpeningTiming("Overlay de carga mostrado en cliente.");
        }

        [ClientRpc]
        private void HideOpeningLoadingClientRpc()
        {
            HideLocalOpeningLoading();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ConfirmSequenceServerRpc(int sequenceToken, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || sequenceToken != activeSequenceToken)
            {
                return;
            }

            awaitingClientIds.Remove(rpcParams.Receive.SenderClientId);
            var confirmedPlayers = activeSequenceClientIds.Count - awaitingClientIds.Count;
            UpdateWaitingProgressClientRpc(
                sequenceToken,
                confirmedPlayers,
                activeSequenceClientIds.Count);
            if (awaitingClientIds.Count == 0)
            {
                PlayNextSequenceOrComplete();
            }
        }

        private void HandleLocalSequenceCompleted(string _)
        {
            if (localDialogueController != null)
            {
                localDialogueController.SequenceCompleted -= HandleLocalSequenceCompleted;
            }

            var completedToken = localSequenceToken;
            localSequenceToken = -1;
            if (completedToken >= 0)
            {
                localDialogueController?.ShowWaitingForPlayers(
                    Mathf.Min(localConfirmedPlayers + 1, localTotalPlayers),
                    localTotalPlayers);
                ConfirmSequenceServerRpc(completedToken);
            }
        }

        [ClientRpc]
        private void UpdateWaitingProgressClientRpc(
            int sequenceToken,
            int confirmedPlayers,
            int totalPlayers)
        {
            if (sequenceToken != localFlowToken)
            {
                return;
            }

            localConfirmedPlayers = confirmedPlayers;
            localTotalPlayers = totalPlayers;
            localDialogueController?.UpdateWaitingProgress(confirmedPlayers, totalPlayers);
        }

        [ClientRpc]
        private void FinishSynchronizedFlowClientRpc()
        {
            localDialogueController?.FinishSynchronizedFlow();
            localFlowToken = -1;
            localConfirmedPlayers = 0;
            localTotalPlayers = 0;
        }

        private void CompletePendingAction()
        {
            var action = pendingAction;
            var minigameIndex = pendingMinigameIndex;
            pendingAction = PendingAction.None;
            pendingMinigameIndex = -1;
            awaitingClientIds.Clear();
            activeSequenceClientIds.Clear();

            switch (action)
            {
                case PendingAction.StartMinigame:
                    sessionCoordinator?.StartMiniGame(minigameIndex);
                    break;
                case PendingAction.ContinueAfterResults:
                    sessionCoordinator?.ContinueAfterMinigameResults();
                    break;
            }
        }

        private bool TryGetLocalDialogueController(out DialogueUIController controller)
        {
            if (localDialogueController != null)
            {
                controller = localDialogueController;
                return true;
            }

            localDialogueController = FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
            if (localDialogueController != null)
            {
                controller = localDialogueController;
                return true;
            }

            if (flowConfig == null || flowConfig.DialoguePanelPrefab == null)
            {
                controller = null;
                return false;
            }

            EnsureLocalPresentationCanvas();
            if (localDialogueCanvas == null || localSafeAreaRoot == null)
            {
                controller = null;
                return false;
            }

            var panel = Instantiate(flowConfig.DialoguePanelPrefab, localSafeAreaRoot, false);
            panel.name = "DialoguePanel";
            if (panel.transform is RectTransform panelRect)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }

            localDialogueController = panel.GetComponent<DialogueUIController>();
            controller = localDialogueController;
            return controller != null && controller.enabled;
        }

        private void EnsureLocalPresentationCanvas()
        {
            if (localDialogueCanvas != null)
            {
                return;
            }

            localDialogueCanvas = new GameObject(
                "DialogueFlowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(localDialogueCanvas);
            }

            var canvas = localDialogueCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = localDialogueCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeAreaRoot = new GameObject(
                "SafeAreaRoot",
                typeof(RectTransform));
            safeAreaRoot.transform.SetParent(localDialogueCanvas.transform, false);
            var safeAreaRect = safeAreaRoot.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaRoot.AddComponent<SafeAreaFitter>();
            localSafeAreaRoot = safeAreaRoot.transform;
        }

        private bool ShouldUseDialogues()
        {
            return flowConfig != null && flowConfig.DialoguesEnabled;
        }

        private void ResolveReferences()
        {
            sessionCoordinator ??= GetComponent<CoopSessionCoordinator>();
            sessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            gpsStateSync ??= GetComponent<CoopGpsStateSync>();
            gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        }

        private void ResolveWorldMapReferences()
        {
            gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
            gpsMarkerController ??= FindFirstObjectByType<CoopGpsMarkerController>(FindObjectsInactive.Include);
            cachedGardenBoundary ??= FindFirstObjectByType<DialogueGardenBoundary>(FindObjectsInactive.Include);
        }

        private void HandleLoadEventCompleted(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut)
        {
            if (!IsServer ||
                !string.Equals(sceneName, worldMapSceneName, StringComparison.Ordinal))
            {
                return;
            }

            worldMapLoadCompleted = true;
            worldMapLoadCompletedAt = Time.unscaledTime;
            cachedGardenBoundary = null;
            LogOpeningTiming(
                $"LoadEventCompleted recibido. Completados={clientsCompleted?.Count ?? 0}, " +
                $"timeout={clientsTimedOut?.Count ?? 0}.");
        }

        private void BuildActivePlayerIds()
        {
            activePlayerIds.Clear();
            if (sessionCoordinator == null)
            {
                return;
            }

            for (var slotIndex = 0; slotIndex < sessionCoordinator.RegisteredPlayerCount; slotIndex++)
            {
                if (sessionCoordinator.TryGetPlayerClientIdAtSlot(slotIndex, out var clientId))
                {
                    activePlayerIds.Add(clientId);
                }
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!activeSequenceClientIds.Remove(clientId))
            {
                return;
            }

            awaitingClientIds.Remove(clientId);
            UpdateWaitingProgressClientRpc(
                activeSequenceToken,
                activeSequenceClientIds.Count - awaitingClientIds.Count,
                activeSequenceClientIds.Count);
            if (awaitingClientIds.Count == 0)
            {
                PlayNextSequenceOrComplete();
            }
        }

        private void ReleaseLocalDialogueController()
        {
            if (localDialogueController != null)
            {
                localDialogueController.SequenceCompleted -= HandleLocalSequenceCompleted;
                localDialogueController.FinishSynchronizedFlow();
            }

            localDialogueController = null;
            localOpeningLoadingView = null;
            localSafeAreaRoot = null;
            if (localDialogueCanvas != null)
            {
                Destroy(localDialogueCanvas);
            }

            localDialogueCanvas = null;
            localSequenceToken = -1;
            localFlowToken = -1;
            localConfirmedPlayers = 0;
            localTotalPlayers = 0;
        }

        private void HideLocalOpeningLoading()
        {
            localOpeningLoadingView?.Hide();
        }

        private void LogOpeningTiming(string message)
        {
            if (flowConfig == null || !flowConfig.LogOpeningTiming)
            {
                return;
            }

            var transitionElapsed = openingTransitionStartedAt <= 0f
                ? 0f
                : Time.unscaledTime - openingTransitionStartedAt;
            var loadElapsed = worldMapLoadCompletedAt <= 0f
                ? 0f
                : Time.unscaledTime - worldMapLoadCompletedAt;
            Debug.Log(
                $"[DialogueOpening] {message} Transition={transitionElapsed:0.000}s " +
                $"AfterLoad={loadElapsed:0.000}s",
                this);
        }
    }
}
