using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    public abstract class CooperativeMinigameBase : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;

        private readonly NetworkVariable<CooperativeMinigameStage> stage = new(CooperativeMinigameStage.Tutorial);
        private readonly NetworkVariable<MinigameResultNetworkState> resultState = new();
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
        public bool CanLocalPlayerReturnToMainMap => NetworkManager != null && NetworkManager.IsHost;
        public CooperativeMinigameConfigBase MinigameConfig => GetMinigameConfig();

        public event Action<CooperativeMinigameStage> StageChanged;
        public event Action TutorialProgressChanged;
        public event Action<MinigameResultData> ResultPublished;

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
            tutorialDismissedClientIds.OnListChanged += HandleTutorialListChanged;

            localTutorialDismissalSubmitted = false;

            if (IsServer)
            {
                stage.Value = CooperativeMinigameStage.Tutorial;
                resultState.Value = default;
                tutorialDismissedClientIds.Clear();
                InitializeMinigameServer();
            }

            TutorialProgressChanged?.Invoke();
            StageChanged?.Invoke(stage.Value);

            if (resultState.Value.HasValue)
            {
                ResultPublished?.Invoke(resultState.Value.ToData());
            }
        }

        public override void OnNetworkDespawn()
        {
            stage.OnValueChanged -= HandleStageChanged;
            resultState.OnValueChanged -= HandleResultChanged;
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

        public void RequestReturnToMainMap()
        {
            if (CanLocalPlayerReturnToMainMap)
            {
                ReturnToMainMapServer();
                return;
            }

            RequestReturnToMainMapServerRpc();
        }

        protected virtual void OnGameplayStartedServer()
        {
        }

        protected void PublishResultServer(MinigameResultData result)
        {
            if (!IsServer)
            {
                return;
            }

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
        }

        [Rpc(SendTo.Server)]
        private void SubmitTutorialDismissalServerRpc(RpcParams rpcParams = default)
        {
            RegisterTutorialDismissal(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server)]
        private void RequestReturnToMainMapServerRpc(RpcParams rpcParams = default)
        {
            if (NetworkManager == null || !NetworkManager.IsHost || rpcParams.Receive.SenderClientId != NetworkManager.LocalClientId)
            {
                return;
            }

            ReturnToMainMapServer();
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
                stage.Value = CooperativeMinigameStage.Playing;
                OnGameplayStartedServer();
            }
            else
            {
                stage.Value = CooperativeMinigameStage.WaitingForPlayers;
            }
        }

        private void ReturnToMainMapServer()
        {
            ResolveCoordinator();
            if (SessionCoordinator != null && SessionCoordinator.IsServer)
            {
                SessionCoordinator.ReturnToMainMap();
            }
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
