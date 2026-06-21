using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Dialogue;

namespace SmartCampus.Coop.Minigames
{
    public abstract class CooperativeMinigameBase : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private DialogueFlowSync dialogueFlowSync;

        private readonly NetworkVariable<CooperativeMinigameStage> stage = new(CooperativeMinigameStage.Tutorial);
        private readonly NetworkVariable<MinigameResultNetworkState> resultState = new();
        private readonly NetworkVariable<FixedString512Bytes> blockingErrorMessage = new();
        private readonly NetworkList<ulong> tutorialDismissedClientIds = new();

        private bool localTutorialDismissalSubmitted;

        public CoopSessionCoordinator SessionCoordinator => coopSessionCoordinator;
        public CooperativeMinigameStage Stage => stage.Value;
        public bool HasLocalTutorialBeenDismissed => NetworkManager != null && TutorialDismissedByClient(GetLocalClientId());
        public bool IsLocalTutorialDismissalPending => localTutorialDismissalSubmitted && !HasLocalTutorialBeenDismissed;
        public int TutorialDismissedCount => tutorialDismissedClientIds.Count;
        public int ParticipantCount => GetParticipantIds().Count;
        public bool HasPublishedResult => resultState.Value.HasValue;
        public MinigameResultData CurrentResult => resultState.Value.ToData();
        public string BlockingErrorMessage => blockingErrorMessage.Value.ToString();
        public bool HasBlockingError => !string.IsNullOrWhiteSpace(BlockingErrorMessage);
        public bool CanLocalPlayerAdvanceAfterResults => NetworkManager != null && NetworkManager.IsHost;
        public bool CanLocalPlayerAbortAfterBlockingError => HasBlockingError && NetworkManager != null && NetworkManager.IsHost;
        public bool CanLocalPlayerReturnToMainMap => CanLocalPlayerAdvanceAfterResults || CanLocalPlayerAbortAfterBlockingError;
        public CooperativeMinigameConfigBase MinigameConfig => GetMinigameConfig();

        public event Action<CooperativeMinigameStage> StageChanged;
        public event Action TutorialProgressChanged;
        public event Action<MinigameResultData> ResultPublished;
        public event Action BlockingErrorChanged;

        protected abstract CooperativeMinigameConfigBase GetMinigameConfig();
        protected abstract void InitializeMinigameServer();

        protected virtual void Awake()
        {
            ResolveCoordinator();
        }

        public override void OnNetworkSpawn()
        {
            ResolveCoordinator();

            stage.OnValueChanged += HandleStageChanged;
            resultState.OnValueChanged += HandleResultChanged;
            blockingErrorMessage.OnValueChanged += HandleBlockingErrorChanged;
            tutorialDismissedClientIds.OnListChanged += HandleTutorialListChanged;

            localTutorialDismissalSubmitted = false;

            if (IsServer)
            {
                stage.Value = CooperativeMinigameStage.Tutorial;
                resultState.Value = default;
                blockingErrorMessage.Value = default;
                tutorialDismissedClientIds.Clear();
                InitializeMinigameServer();
            }

            TutorialProgressChanged?.Invoke();
            StageChanged?.Invoke(stage.Value);

            if (resultState.Value.HasValue)
            {
                ResultPublished?.Invoke(resultState.Value.ToData());
            }

            if (HasBlockingError)
            {
                BlockingErrorChanged?.Invoke();
            }
        }

        public override void OnNetworkDespawn()
        {
            stage.OnValueChanged -= HandleStageChanged;
            resultState.OnValueChanged -= HandleResultChanged;
            blockingErrorMessage.OnValueChanged -= HandleBlockingErrorChanged;
            tutorialDismissedClientIds.OnListChanged -= HandleTutorialListChanged;
        }

        public void DismissLocalTutorial()
        {
            if (HasLocalTutorialBeenDismissed || IsLocalTutorialDismissalPending)
            {
                return;
            }

            localTutorialDismissalSubmitted = true;
            TutorialProgressChanged?.Invoke();

            if (IsServer)
            {
                RegisterTutorialDismissal(GetLocalClientId());
                return;
            }

            SubmitTutorialDismissalServerRpc();
        }

        public string GetContinueAfterResultsButtonLabel()
        {
            ResolveCoordinator();
            return SessionCoordinator == null ? "Continuar" : SessionCoordinator.GetResultsContinueButtonLabel();
        }

        public string GetReturnToMainMapButtonLabel()
        {
            var configuredLabel = MinigameConfig == null ? string.Empty : MinigameConfig.ReturnToMapButtonLabel;
            return string.IsNullOrWhiteSpace(configuredLabel) ? "Volver al mapa" : configuredLabel;
        }

        public void RequestAdvanceAfterResults()
        {
            if (HasBlockingError)
            {
                RequestAbortAfterBlockingError();
                return;
            }

            if (CanLocalPlayerAdvanceAfterResults)
            {
                RequestDialogueAwareAdvanceAfterResults();
                return;
            }

            RequestAdvanceAfterResultsServerRpc();
        }

        public void RequestReturnToMainMap()
        {
            if (HasBlockingError)
            {
                RequestAbortAfterBlockingError();
                return;
            }

            RequestAdvanceAfterResults();
        }

        public void RequestAbortAfterBlockingError()
        {
            if (!HasBlockingError)
            {
                return;
            }

            if (CanLocalPlayerAbortAfterBlockingError)
            {
                AbortAfterBlockingErrorServer();
                return;
            }

            RequestAbortAfterBlockingErrorServerRpc();
        }

        public bool TryForceCompleteForTesting(MinigameResultData forcedResult)
        {
            if (!IsServer || HasPublishedResult)
            {
                return false;
            }

            PublishResultServer(forcedResult);
            return true;
        }

        protected virtual void OnGameplayStartedServer()
        {
        }

        protected void SetBlockingErrorServer(string message)
        {
            if (!IsServer)
            {
                return;
            }

            blockingErrorMessage.Value = string.IsNullOrWhiteSpace(message)
                ? default
                : new FixedString512Bytes(message);

            if (stage.Value == CooperativeMinigameStage.Playing || stage.Value == CooperativeMinigameStage.Results)
            {
                stage.Value = CooperativeMinigameStage.WaitingForPlayers;
            }
        }

        protected void ClearBlockingErrorServer()
        {
            if (!IsServer)
            {
                return;
            }

            blockingErrorMessage.Value = default;
        }

        protected void PublishResultServer(MinigameResultData result)
        {
            if (!IsServer)
            {
                return;
            }

            ResolveCoordinator();
            SessionCoordinator?.TryRegisterMiniGameResult(result);
            blockingErrorMessage.Value = default;
            resultState.Value = MinigameResultNetworkState.FromData(result);
            stage.Value = CooperativeMinigameStage.Results;
        }

        protected IReadOnlyList<ulong> GetParticipantIds()
        {
            return NetworkManager == null ? Array.Empty<ulong>() : NetworkManager.ConnectedClientsIds;
        }

        protected bool TutorialDismissedByClient(ulong clientId)
        {
            for (var index = 0; index < tutorialDismissedClientIds.Count; index++)
            {
                if (tutorialDismissedClientIds[index] == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        protected ulong GetLocalClientId()
        {
            return NetworkManager == null ? 0UL : NetworkManager.LocalClientId;
        }

        private void ResolveCoordinator()
        {
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            dialogueFlowSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.GetComponent<DialogueFlowSync>()
                : FindFirstObjectByType<DialogueFlowSync>(FindObjectsInactive.Include);
        }

        [Rpc(SendTo.Server)]
        private void SubmitTutorialDismissalServerRpc(RpcParams rpcParams = default)
        {
            RegisterTutorialDismissal(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server)]
        private void RequestAdvanceAfterResultsServerRpc(RpcParams rpcParams = default)
        {
            if (NetworkManager == null || !NetworkManager.IsHost || rpcParams.Receive.SenderClientId != NetworkManager.LocalClientId)
            {
                return;
            }

            AdvanceAfterResultsServer();
        }

        [Rpc(SendTo.Server)]
        private void RequestAbortAfterBlockingErrorServerRpc(RpcParams rpcParams = default)
        {
            if (NetworkManager == null || !NetworkManager.IsHost || rpcParams.Receive.SenderClientId != NetworkManager.LocalClientId)
            {
                return;
            }

            AbortAfterBlockingErrorServer();
        }

        private void RegisterTutorialDismissal(ulong clientId)
        {
            if (!IsServer || TutorialDismissedByClient(clientId))
            {
                return;
            }

            tutorialDismissedClientIds.Add(clientId);

            if (ParticipantCount > 0 && tutorialDismissedClientIds.Count >= ParticipantCount)
            {
                if (HasBlockingError)
                {
                    stage.Value = CooperativeMinigameStage.WaitingForPlayers;
                }
                else
                {
                    stage.Value = CooperativeMinigameStage.Playing;
                    OnGameplayStartedServer();
                }
            }
            else
            {
                stage.Value = CooperativeMinigameStage.WaitingForPlayers;
            }
        }

        private void AdvanceAfterResultsServer()
        {
            ResolveCoordinator();
            if (SessionCoordinator != null && SessionCoordinator.IsServer)
            {
                SessionCoordinator.ContinueAfterMinigameResults();
            }
        }

        private void RequestDialogueAwareAdvanceAfterResults()
        {
            ResolveCoordinator();
            if (dialogueFlowSync != null)
            {
                dialogueFlowSync.RequestContinueAfterResults();
            }
            else
            {
                AdvanceAfterResultsServer();
            }
        }

        private void AbortAfterBlockingErrorServer()
        {
            if (!IsServer || !HasBlockingError)
            {
                return;
            }

            ResolveCoordinator();
            SessionCoordinator?.ReturnToMainMap();
        }

        private void HandleStageChanged(CooperativeMinigameStage _, CooperativeMinigameStage current)
        {
            StageChanged?.Invoke(current);
        }

        private void HandleResultChanged(MinigameResultNetworkState _, MinigameResultNetworkState current)
        {
            if (current.HasValue)
            {
                ResultPublished?.Invoke(current.ToData());
            }
        }

        private void HandleBlockingErrorChanged(FixedString512Bytes _, FixedString512Bytes __)
        {
            BlockingErrorChanged?.Invoke();
        }

        private void HandleTutorialListChanged(NetworkListEvent<ulong> _)
        {
            if (HasLocalTutorialBeenDismissed)
            {
                localTutorialDismissalSubmitted = false;
            }

            TutorialProgressChanged?.Invoke();
        }
    }
}
