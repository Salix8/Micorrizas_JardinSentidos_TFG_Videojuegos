using System.Collections.Generic;

namespace SmartCampus.Coop
{
    public readonly struct CoopPlayerSnapshot
    {
        public CoopPlayerSnapshot(string deviceId, int slotIndex, bool isHost, bool isReady)
        {
            DeviceId = deviceId;
            SlotIndex = slotIndex;
            IsHost = isHost;
            IsReady = isReady;
        }

        public string DeviceId { get; }
        public int SlotIndex { get; }
        public bool IsHost { get; }
        public bool IsReady { get; }
    }

    public sealed class CoopSessionSnapshot
    {
        public CoopSessionSnapshot(
            string lobbyCode,
            string hostDeviceId,
            CoopGamePhase phase,
            CoopSessionRules rules,
            IReadOnlyList<CoopPlayerSnapshot> players)
        {
            LobbyCode = lobbyCode;
            HostDeviceId = hostDeviceId;
            Phase = phase;
            MinimumPlayers = rules.MinimumPlayers;
            MaximumPlayers = rules.MaximumPlayers;
            Players = players ?? new List<CoopPlayerSnapshot>();
        }

        public string LobbyCode { get; }
        public string HostDeviceId { get; }
        public CoopGamePhase Phase { get; }
        public int MinimumPlayers { get; }
        public int MaximumPlayers { get; }
        public IReadOnlyList<CoopPlayerSnapshot> Players { get; }
        public int PlayerCount => Players.Count;
        public bool CanStart => PlayerCount >= MinimumPlayers && PlayerCount <= MaximumPlayers;
    }
}
