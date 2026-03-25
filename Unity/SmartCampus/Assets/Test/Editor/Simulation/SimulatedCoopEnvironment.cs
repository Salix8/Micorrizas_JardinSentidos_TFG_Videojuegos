using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCampus.Coop
{
    public enum SimulatedCoopFailureKind
    {
        None,
        InvalidCode,
        Timeout,
        ConnectionError,
        LobbyFull,
        DuplicateDevice,
        MissingLobby,
        Unauthorized
    }

    public sealed class SimulatedCoopOperationResult
    {
        private SimulatedCoopOperationResult(
            bool success,
            SimulatedCoopFailureKind failureKind,
            string message,
            string lobbyCode,
            CoopSessionSnapshot snapshot)
        {
            Success = success;
            FailureKind = failureKind;
            Message = message ?? string.Empty;
            LobbyCode = lobbyCode ?? string.Empty;
            Snapshot = snapshot;
        }

        public bool Success { get; }
        public SimulatedCoopFailureKind FailureKind { get; }
        public string Message { get; }
        public string LobbyCode { get; }
        public CoopSessionSnapshot Snapshot { get; }

        public static SimulatedCoopOperationResult Successful(string message, string lobbyCode, CoopSessionSnapshot snapshot)
        {
            return new SimulatedCoopOperationResult(true, SimulatedCoopFailureKind.None, message, lobbyCode, snapshot);
        }

        public static SimulatedCoopOperationResult Failed(
            SimulatedCoopFailureKind failureKind,
            string message,
            string lobbyCode = "",
            CoopSessionSnapshot snapshot = null)
        {
            return new SimulatedCoopOperationResult(false, failureKind, message, lobbyCode, snapshot);
        }
    }

    public sealed class SimulatedCoopEnvironment
    {
        private readonly Dictionary<string, SimulatedLobby> lobbies = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> deviceLobbyCodes = new(StringComparer.OrdinalIgnoreCase);
        private int nextLobbyCodeSeed = 1000;
        private SimulatedCoopFailureKind nextJoinFailureKind;
        private string nextJoinFailureMessage = string.Empty;

        public SimulatedCoopEnvironment(CoopSessionRules? rules = null)
        {
            Rules = rules ?? new CoopSessionRules(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers);
        }

        public CoopSessionRules Rules { get; }

        public void ConfigureNextJoinFailure(SimulatedCoopFailureKind failureKind, string message)
        {
            nextJoinFailureKind = failureKind;
            nextJoinFailureMessage = message ?? string.Empty;
        }

        public SimulatedCoopOperationResult CreateHost(string deviceId)
        {
            var sanitizedDeviceId = SanitizeDeviceId(deviceId);
            if (string.IsNullOrWhiteSpace(sanitizedDeviceId))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.InvalidCode,
                    "A host device identifier is required.");
            }

            if (deviceLobbyCodes.ContainsKey(sanitizedDeviceId))
            {
                Leave(sanitizedDeviceId);
            }

            var joinCode = GenerateJoinCode();
            var lobby = new SimulatedLobby(joinCode, sanitizedDeviceId, Rules);
            lobbies[joinCode] = lobby;
            deviceLobbyCodes[sanitizedDeviceId] = joinCode;
            return SimulatedCoopOperationResult.Successful(
                $"Host '{sanitizedDeviceId}' created lobby {joinCode}.",
                joinCode,
                lobby.CreateSnapshot());
        }

        public SimulatedCoopOperationResult Join(string deviceId, string joinCode)
        {
            var sanitizedDeviceId = SanitizeDeviceId(deviceId);
            if (string.IsNullOrWhiteSpace(sanitizedDeviceId))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.InvalidCode,
                    "A client device identifier is required.");
            }

            if (nextJoinFailureKind != SimulatedCoopFailureKind.None)
            {
                var forcedFailure = SimulatedCoopOperationResult.Failed(
                    nextJoinFailureKind,
                    string.IsNullOrWhiteSpace(nextJoinFailureMessage) ? "A forced join failure occurred." : nextJoinFailureMessage,
                    SanitizeJoinCode(joinCode));
                nextJoinFailureKind = SimulatedCoopFailureKind.None;
                nextJoinFailureMessage = string.Empty;
                return forcedFailure;
            }

            var sanitizedJoinCode = SanitizeJoinCode(joinCode);
            if (string.IsNullOrWhiteSpace(sanitizedJoinCode) || !lobbies.TryGetValue(sanitizedJoinCode, out var lobby))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.InvalidCode,
                    $"Join code '{sanitizedJoinCode}' is not valid.",
                    sanitizedJoinCode);
            }

            if (deviceLobbyCodes.ContainsKey(sanitizedDeviceId))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.DuplicateDevice,
                    $"Device '{sanitizedDeviceId}' is already connected to a lobby.",
                    sanitizedJoinCode,
                    lobby.CreateSnapshot());
            }

            if (!Rules.CanAddPlayer(lobby.PlayerCount))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.LobbyFull,
                    Rules.GetJoinBlocker(lobby.PlayerCount),
                    sanitizedJoinCode,
                    lobby.CreateSnapshot());
            }

            lobby.AddPlayer(sanitizedDeviceId);
            deviceLobbyCodes[sanitizedDeviceId] = sanitizedJoinCode;
            return SimulatedCoopOperationResult.Successful(
                $"Device '{sanitizedDeviceId}' joined lobby {sanitizedJoinCode}.",
                sanitizedJoinCode,
                lobby.CreateSnapshot());
        }

        public SimulatedCoopOperationResult StartMainMap(string requesterDeviceId)
        {
            if (!TryGetLobby(requesterDeviceId, out var lobby))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.MissingLobby,
                    $"Device '{requesterDeviceId}' is not attached to any lobby.");
            }

            if (!string.Equals(lobby.HostDeviceId, requesterDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.Unauthorized,
                    $"Device '{requesterDeviceId}' is not the lobby host.",
                    lobby.Code,
                    lobby.CreateSnapshot());
            }

            if (!Rules.CanStart(lobby.PlayerCount))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.ConnectionError,
                    Rules.GetStartBlocker(lobby.PlayerCount),
                    lobby.Code,
                    lobby.CreateSnapshot());
            }

            lobby.SetPhase(CoopGamePhase.WorldMap);
            return SimulatedCoopOperationResult.Successful(
                $"Lobby {lobby.Code} transitioned to the main map with {lobby.PlayerCount} players.",
                lobby.Code,
                lobby.CreateSnapshot());
        }

        public SimulatedCoopOperationResult Leave(string deviceId)
        {
            if (!TryGetLobby(deviceId, out var lobby))
            {
                return SimulatedCoopOperationResult.Failed(
                    SimulatedCoopFailureKind.MissingLobby,
                    $"Device '{deviceId}' is not attached to any lobby.");
            }

            var sanitizedDeviceId = SanitizeDeviceId(deviceId);
            var isHost = string.Equals(lobby.HostDeviceId, sanitizedDeviceId, StringComparison.OrdinalIgnoreCase);
            lobby.RemovePlayer(sanitizedDeviceId);
            deviceLobbyCodes.Remove(sanitizedDeviceId);

            if (isHost || lobby.PlayerCount == 0)
            {
                foreach (var participant in lobby.GetDeviceIds())
                {
                    deviceLobbyCodes.Remove(participant);
                }

                lobbies.Remove(lobby.Code);
                return SimulatedCoopOperationResult.Successful(
                    $"Lobby {lobby.Code} closed after '{sanitizedDeviceId}' left.",
                    lobby.Code,
                    null);
            }

            return SimulatedCoopOperationResult.Successful(
                $"Device '{sanitizedDeviceId}' left lobby {lobby.Code}.",
                lobby.Code,
                lobby.CreateSnapshot());
        }

        public CoopSessionSnapshot GetSnapshot(string deviceId)
        {
            return TryGetLobby(deviceId, out var lobby) ? lobby.CreateSnapshot() : null;
        }

        public bool AreSynchronized(params string[] deviceIds)
        {
            if (deviceIds == null || deviceIds.Length == 0)
            {
                return false;
            }

            var firstSnapshot = GetSnapshot(deviceIds[0]);
            if (firstSnapshot == null)
            {
                return false;
            }

            for (var index = 1; index < deviceIds.Length; index++)
            {
                var nextSnapshot = GetSnapshot(deviceIds[index]);
                if (nextSnapshot == null ||
                    nextSnapshot.LobbyCode != firstSnapshot.LobbyCode ||
                    nextSnapshot.Phase != firstSnapshot.Phase ||
                    nextSnapshot.PlayerCount != firstSnapshot.PlayerCount ||
                    nextSnapshot.HostDeviceId != firstSnapshot.HostDeviceId)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetLobby(string deviceId, out SimulatedLobby lobby)
        {
            lobby = null;
            var sanitizedDeviceId = SanitizeDeviceId(deviceId);
            if (string.IsNullOrWhiteSpace(sanitizedDeviceId) ||
                !deviceLobbyCodes.TryGetValue(sanitizedDeviceId, out var joinCode))
            {
                return false;
            }

            return lobbies.TryGetValue(joinCode, out lobby);
        }

        private string GenerateJoinCode()
        {
            var joinCode = $"SC{nextLobbyCodeSeed:0000}";
            nextLobbyCodeSeed++;
            return joinCode;
        }

        private static string SanitizeDeviceId(string deviceId)
        {
            return (deviceId ?? string.Empty).Trim();
        }

        private static string SanitizeJoinCode(string joinCode)
        {
            return (joinCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private sealed class SimulatedLobby
        {
            private readonly List<string> deviceIds = new();
            private readonly CoopSessionRules rules;

            public SimulatedLobby(string code, string hostDeviceId, CoopSessionRules rules)
            {
                Code = code;
                HostDeviceId = hostDeviceId;
                this.rules = rules;
                Phase = CoopGamePhase.Lobby;
                deviceIds.Add(hostDeviceId);
            }

            public string Code { get; }
            public string HostDeviceId { get; }
            public CoopGamePhase Phase { get; private set; }
            public int PlayerCount => deviceIds.Count;

            public void AddPlayer(string deviceId)
            {
                deviceIds.Add(deviceId);
            }

            public void RemovePlayer(string deviceId)
            {
                deviceIds.RemoveAll(entry => string.Equals(entry, deviceId, StringComparison.OrdinalIgnoreCase));
            }

            public void SetPhase(CoopGamePhase phase)
            {
                Phase = phase;
            }

            public IReadOnlyList<string> GetDeviceIds()
            {
                return deviceIds.ToArray();
            }

            public CoopSessionSnapshot CreateSnapshot()
            {
                var players = deviceIds
                    .Select((deviceId, index) => new CoopPlayerSnapshot(
                        deviceId,
                        index,
                        string.Equals(deviceId, HostDeviceId, StringComparison.OrdinalIgnoreCase),
                        false))
                    .ToArray();
                return new CoopSessionSnapshot(Code, HostDeviceId, Phase, rules, players);
            }
        }
    }
}
