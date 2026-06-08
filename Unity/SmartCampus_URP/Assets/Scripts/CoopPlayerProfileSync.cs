using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopPlayerProfileSync : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private LocalPlayerMarkerProfileService localProfileService;
        [SerializeField] private PlayerMarkerAppearanceCatalogConfig appearanceCatalog;

        private readonly NetworkList<CoopPlayerProfileNetworkState> playerProfiles = new();

        public NetworkList<CoopPlayerProfileNetworkState> PlayerProfiles => playerProfiles;
        public PlayerMarkerAppearanceCatalogConfig AppearanceCatalog => appearanceCatalog;

        public event Action ProfilesChanged;

        private void Awake()
        {
            ResolveReferences();
        }

        public override void OnNetworkSpawn()
        {
            ResolveReferences();
            playerProfiles.OnListChanged += HandleProfilesChanged;

            if (localProfileService != null)
            {
                localProfileService.ProfileChanged += HandleLocalProfileChanged;
            }

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            SubmitLocalProfile();
            ProfilesChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            playerProfiles.OnListChanged -= HandleProfilesChanged;

            if (localProfileService != null)
            {
                localProfileService.ProfileChanged -= HandleLocalProfileChanged;
            }

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public bool TryGetProfile(ulong clientId, out CoopPlayerProfileNetworkState profile)
        {
            for (var index = 0; index < playerProfiles.Count; index++)
            {
                if (playerProfiles[index].ClientId == clientId)
                {
                    profile = playerProfiles[index];
                    return true;
                }
            }

            profile = default;
            return false;
        }

        private void HandleLocalProfileChanged()
        {
            SubmitLocalProfile();
        }

        private void SubmitLocalProfile()
        {
            ResolveReferences();
            if (!IsSpawned || NetworkManager == null || !NetworkManager.IsListening || localProfileService == null)
            {
                return;
            }

            localProfileService.EnsureInitialized();
            SubmitProfileServerRpc(localProfileService.CurrentAvatarId, localProfileService.CurrentDisplayName);
        }

        [Rpc(SendTo.Server)]
        private void SubmitProfileServerRpc(string avatarId, string displayName, RpcParams rpcParams = default)
        {
            var senderClientId = rpcParams.Receive.SenderClientId;
            var nextState = new CoopPlayerProfileNetworkState(
                senderClientId,
                ResolveAvatarIdOrDefault(avatarId),
                NormalizeDisplayName(displayName));

            for (var index = 0; index < playerProfiles.Count; index++)
            {
                if (playerProfiles[index].ClientId != senderClientId)
                {
                    continue;
                }

                playerProfiles[index] = nextState;
                return;
            }

            playerProfiles.Add(nextState);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            for (var index = playerProfiles.Count - 1; index >= 0; index--)
            {
                if (playerProfiles[index].ClientId == clientId)
                {
                    playerProfiles.RemoveAt(index);
                }
            }
        }

        private void HandleProfilesChanged(NetworkListEvent<CoopPlayerProfileNetworkState> _)
        {
            ProfilesChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            localProfileService ??= FindFirstObjectByType<LocalPlayerMarkerProfileService>(FindObjectsInactive.Include);
            if (appearanceCatalog == null && localProfileService != null)
            {
                appearanceCatalog = localProfileService.AppearanceCatalog;
            }
        }

        private string ResolveAvatarIdOrDefault(string avatarId)
        {
            return appearanceCatalog != null
                ? appearanceCatalog.ResolveAvatarIdOrDefault(avatarId)
                : NormalizeIdentifier(avatarId);
        }

        private static string NormalizeDisplayName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? "Aventurero" : displayName.Trim();
        }

        private static string NormalizeIdentifier(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public struct CoopPlayerProfileNetworkState : INetworkSerializable, IEquatable<CoopPlayerProfileNetworkState>
    {
        public ulong ClientId;
        public FixedString64Bytes AvatarId;
        public FixedString64Bytes DisplayName;

        public CoopPlayerProfileNetworkState(ulong clientId, string avatarId, string displayName)
        {
            ClientId = clientId;
            AvatarId = avatarId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref AvatarId);
            serializer.SerializeValue(ref DisplayName);
        }

        public bool Equals(CoopPlayerProfileNetworkState other)
        {
            return ClientId == other.ClientId &&
                   AvatarId.Equals(other.AvatarId) &&
                   DisplayName.Equals(other.DisplayName);
        }

        public override bool Equals(object obj)
        {
            return obj is CoopPlayerProfileNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, AvatarId, DisplayName);
        }
    }
}
