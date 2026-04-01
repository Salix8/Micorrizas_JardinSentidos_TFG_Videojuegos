using UnityEngine;

namespace SmartCampus.Coop
{
    public readonly struct CoopSessionRules
    {
        public const int DefaultMinimumPlayers = 2;
        public const int DefaultMaximumPlayers = 6;

        public CoopSessionRules(int minimumPlayers, int maximumPlayers)
        {
            MinimumPlayers = ClampMinimum(minimumPlayers);
            MaximumPlayers = ClampMaximum(MinimumPlayers, maximumPlayers);
        }

        public int MinimumPlayers { get; }
        public int MaximumPlayers { get; }

        public static int ClampMinimum(int minimumPlayers)
        {
            return Mathf.Clamp(minimumPlayers, DefaultMinimumPlayers, DefaultMaximumPlayers);
        }

        public static int ClampMaximum(int minimumPlayers, int maximumPlayers)
        {
            return Mathf.Clamp(maximumPlayers, ClampMinimum(minimumPlayers), DefaultMaximumPlayers);
        }

        public bool CanStart(int connectedPlayerCount)
        {
            return connectedPlayerCount >= MinimumPlayers && connectedPlayerCount <= MaximumPlayers;
        }

        public bool CanAddPlayer(int currentPlayerCount)
        {
            return currentPlayerCount < MaximumPlayers;
        }

        public string DescribeRequirements()
        {
            return $"{MinimumPlayers}-{MaximumPlayers} players";
        }

        public string GetStartBlocker(int connectedPlayerCount)
        {
            if (connectedPlayerCount < MinimumPlayers)
            {
                return $"The co-op session needs at least {MinimumPlayers} players before it can start.";
            }

            if (connectedPlayerCount > MaximumPlayers)
            {
                return $"The co-op session cannot exceed {MaximumPlayers} players.";
            }

            return string.Empty;
        }

        public string GetJoinBlocker(int currentPlayerCount)
        {
            return currentPlayerCount >= MaximumPlayers
                ? $"The co-op lobby is full. Maximum supported players: {MaximumPlayers}."
                : string.Empty;
        }
    }
}
