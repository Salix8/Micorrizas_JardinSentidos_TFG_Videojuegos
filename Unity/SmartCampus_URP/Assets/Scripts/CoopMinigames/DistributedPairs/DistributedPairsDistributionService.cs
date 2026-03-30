using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    public readonly struct DistributedPairsCardModel
    {
        public DistributedPairsCardModel(int cardInstanceId, int pairId)
        {
            CardInstanceId = cardInstanceId;
            PairId = pairId;
        }

        public int CardInstanceId { get; }
        public int PairId { get; }
    }

    public static class DistributedPairsDistributionService
    {
        public static IReadOnlyDictionary<int, ulong> PlanAssignments(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, IReadOnlyList<int>> currentHandPairIds,
            IReadOnlyDictionary<ulong, int> currentHandCounts,
            IReadOnlyList<DistributedPairsCardModel> availableCards,
            int targetHandSize,
            int randomSeed)
        {
            var positions = BuildPositions(participantIds, currentHandCounts, targetHandSize);
            if (positions.Count == 0 || availableCards.Count == 0)
            {
                return new Dictionary<int, ulong>();
            }

            var rng = new Random(randomSeed);
            var shuffledCards = availableCards.OrderBy(_ => rng.Next()).ToList();
            var pairSets = BuildPairSets(participantIds, currentHandPairIds);

            var currentAssignments = new Dictionary<int, ulong>();
            var bestAssignments = new Dictionary<int, ulong>();
            Search(0, positions, shuffledCards, pairSets, currentAssignments, bestAssignments);
            return bestAssignments;
        }

        private static List<ulong> BuildPositions(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, int> currentHandCounts,
            int targetHandSize)
        {
            var orderedPlayers = participantIds
                .OrderBy(id => currentHandCounts.TryGetValue(id, out var count) ? count : 0)
                .ThenBy(id => id)
                .ToList();

            var positions = new List<ulong>();
            foreach (var playerId in orderedPlayers)
            {
                var currentCount = currentHandCounts.TryGetValue(playerId, out var count) ? count : 0;
                var missingCards = Math.Max(0, targetHandSize - currentCount);
                for (var index = 0; index < missingCards; index++)
                {
                    positions.Add(playerId);
                }
            }

            return positions;
        }

        private static Dictionary<ulong, HashSet<int>> BuildPairSets(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, IReadOnlyList<int>> currentHandPairIds)
        {
            var pairSets = new Dictionary<ulong, HashSet<int>>();
            foreach (var playerId in participantIds)
            {
                if (currentHandPairIds.TryGetValue(playerId, out var pairIds))
                {
                    pairSets[playerId] = new HashSet<int>(pairIds);
                }
                else
                {
                    pairSets[playerId] = new HashSet<int>();
                }
            }

            return pairSets;
        }

        private static void Search(
            int positionIndex,
            IReadOnlyList<ulong> positions,
            List<DistributedPairsCardModel> remainingCards,
            Dictionary<ulong, HashSet<int>> pairSets,
            Dictionary<int, ulong> currentAssignments,
            Dictionary<int, ulong> bestAssignments)
        {
            if (currentAssignments.Count + Math.Min(remainingCards.Count, positions.Count - positionIndex) <= bestAssignments.Count)
            {
                return;
            }

            if (positionIndex >= positions.Count || remainingCards.Count == 0)
            {
                if (currentAssignments.Count > bestAssignments.Count)
                {
                    bestAssignments.Clear();
                    foreach (var assignment in currentAssignments)
                    {
                        bestAssignments[assignment.Key] = assignment.Value;
                    }
                }

                return;
            }

            var playerId = positions[positionIndex];
            var playerPairs = pairSets[playerId];
            var exploredPairs = new HashSet<int>();

            for (var cardIndex = 0; cardIndex < remainingCards.Count; cardIndex++)
            {
                var candidate = remainingCards[cardIndex];
                if (playerPairs.Contains(candidate.PairId) || !exploredPairs.Add(candidate.PairId))
                {
                    continue;
                }

                remainingCards.RemoveAt(cardIndex);
                playerPairs.Add(candidate.PairId);
                currentAssignments[candidate.CardInstanceId] = playerId;

                Search(positionIndex + 1, positions, remainingCards, pairSets, currentAssignments, bestAssignments);

                currentAssignments.Remove(candidate.CardInstanceId);
                playerPairs.Remove(candidate.PairId);
                remainingCards.Insert(cardIndex, candidate);
            }

            Search(positionIndex + 1, positions, remainingCards, pairSets, currentAssignments, bestAssignments);
        }
    }
}
