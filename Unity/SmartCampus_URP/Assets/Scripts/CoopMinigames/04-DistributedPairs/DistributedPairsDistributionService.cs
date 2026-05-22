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
            int randomSeed,
            int guaranteedVisiblePairsOffset)
        {
            var positions = BuildPositions(participantIds, currentHandCounts, targetHandSize);
            if (positions.Count == 0 || availableCards.Count == 0)
            {
                return new Dictionary<int, ulong>();
            }

            var normalizedCounts = BuildNormalizedCounts(participantIds, currentHandCounts);
            var rng = new Random(randomSeed);
            var shuffledCards = ShuffleCards(availableCards, rng);
            var pairSets = BuildPairSets(participantIds, currentHandPairIds);

            var currentVisiblePairs = CountVisiblePairs(pairSets);
            var maxVisibleCardCount = normalizedCounts.Values.Sum() + Math.Min(positions.Count, shuffledCards.Count);
            var targetVisiblePairs = CalculateGuaranteedVisiblePairCount(
                participantIds.Count,
                maxVisibleCardCount,
                guaranteedVisiblePairsOffset);

            var guaranteedAssignments = PlanGuaranteedAssignments(
                participantIds,
                pairSets,
                normalizedCounts,
                shuffledCards,
                targetHandSize,
                targetVisiblePairs,
                currentVisiblePairs,
                rng);

            var guaranteedCardIds = guaranteedAssignments.Keys.ToHashSet();
            var remainingCards = shuffledCards
                .Where(card => !guaranteedCardIds.Contains(card.CardInstanceId))
                .ToList();

            var updatedCounts = ApplyAssignmentCounts(normalizedCounts, guaranteedAssignments);
            var updatedPairIds = BuildPairIdLists(pairSets);
            var remainingPositions = BuildPositions(participantIds, updatedCounts, targetHandSize);

            if (remainingPositions.Count == 0 || remainingCards.Count == 0)
            {
                return guaranteedAssignments;
            }

            var fillAssignments = new Dictionary<int, ulong>();
            var bestFillAssignments = new Dictionary<int, ulong>();
            var fillPairSets = BuildPairSets(participantIds, updatedPairIds);
            Search(
                0,
                remainingPositions,
                remainingCards,
                fillPairSets,
                fillAssignments,
                bestFillAssignments);

            var combinedAssignments = new Dictionary<int, ulong>(guaranteedAssignments);
            foreach (var assignment in bestFillAssignments)
            {
                combinedAssignments[assignment.Key] = assignment.Value;
            }

            return combinedAssignments;
        }

        private static Dictionary<ulong, int> BuildNormalizedCounts(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, int> currentHandCounts)
        {
            var counts = new Dictionary<ulong, int>(participantIds.Count);
            foreach (var playerId in participantIds)
            {
                counts[playerId] = currentHandCounts.TryGetValue(playerId, out var count)
                    ? Math.Max(0, count)
                    : 0;
            }

            return counts;
        }

        private static List<DistributedPairsCardModel> ShuffleCards(
            IReadOnlyList<DistributedPairsCardModel> availableCards,
            Random rng)
        {
            return availableCards
                .OrderBy(_ => rng.Next())
                .ThenBy(card => card.CardInstanceId)
                .ToList();
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
            var pairSets = new Dictionary<ulong, HashSet<int>>(participantIds.Count);
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

        private static IReadOnlyDictionary<int, ulong> PlanGuaranteedAssignments(
            IReadOnlyList<ulong> participantIds,
            Dictionary<ulong, HashSet<int>> pairSets,
            Dictionary<ulong, int> currentHandCounts,
            IReadOnlyList<DistributedPairsCardModel> availableCards,
            int targetHandSize,
            int targetVisiblePairs,
            int currentVisiblePairs,
            Random rng)
        {
            if (participantIds.Count == 0 || availableCards.Count == 0 || currentVisiblePairs >= targetVisiblePairs)
            {
                return new Dictionary<int, ulong>();
            }

            var remainingSlots = participantIds.ToDictionary(
                playerId => playerId,
                playerId => Math.Max(0, targetHandSize - currentHandCounts[playerId]));

            var cardsByPair = availableCards
                .GroupBy(card => card.PairId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(_ => rng.Next()).ThenBy(card => card.CardInstanceId).ToList());

            var candidatePairIds = cardsByPair.Keys
                .OrderBy(_ => rng.Next())
                .ThenBy(pairId => pairId)
                .ToList();

            var currentAssignments = new Dictionary<int, ulong>();
            var bestAssignments = new Dictionary<int, ulong>();
            var bestScore = new GuaranteeScore(
                cappedVisiblePairs: Math.Min(currentVisiblePairs, targetVisiblePairs),
                assignmentCount: 0);

            SearchGuaranteedAssignments(
                0,
                candidatePairIds,
                participantIds,
                pairSets,
                remainingSlots,
                cardsByPair,
                targetVisiblePairs,
                currentVisiblePairs,
                currentAssignments,
                bestAssignments,
                ref bestScore,
                rng);

            return bestAssignments;
        }

        private static void SearchGuaranteedAssignments(
            int pairIndex,
            IReadOnlyList<int> candidatePairIds,
            IReadOnlyList<ulong> participantIds,
            Dictionary<ulong, HashSet<int>> pairSets,
            Dictionary<ulong, int> remainingSlots,
            Dictionary<int, List<DistributedPairsCardModel>> cardsByPair,
            int targetVisiblePairs,
            int currentVisiblePairs,
            Dictionary<int, ulong> currentAssignments,
            Dictionary<int, ulong> bestAssignments,
            ref GuaranteeScore bestScore,
            Random rng)
        {
            var currentScore = new GuaranteeScore(
                cappedVisiblePairs: Math.Min(currentVisiblePairs, targetVisiblePairs),
                assignmentCount: currentAssignments.Count);

            if (currentScore.IsBetterThan(bestScore))
            {
                bestScore = currentScore;
                CopyAssignments(currentAssignments, bestAssignments);
            }

            if (pairIndex >= candidatePairIds.Count || currentVisiblePairs >= targetVisiblePairs)
            {
                return;
            }

            var maxPossibleVisiblePairs = currentVisiblePairs + CountPotentialVisiblePairs(
                candidatePairIds,
                pairIndex,
                participantIds,
                pairSets,
                remainingSlots,
                cardsByPair);

            if (Math.Min(maxPossibleVisiblePairs, targetVisiblePairs) < bestScore.CappedVisiblePairs)
            {
                return;
            }

            var pairId = candidatePairIds[pairIndex];
            if (TryBuildPairPlacements(
                pairId,
                participantIds,
                pairSets,
                remainingSlots,
                cardsByPair,
                rng,
                out var placements))
            {
                foreach (var placement in placements)
                {
                    ApplyPlacement(placement, pairSets, remainingSlots, cardsByPair, currentAssignments);

                    SearchGuaranteedAssignments(
                        pairIndex + 1,
                        candidatePairIds,
                        participantIds,
                        pairSets,
                        remainingSlots,
                        cardsByPair,
                        targetVisiblePairs,
                        currentVisiblePairs + 1,
                        currentAssignments,
                        bestAssignments,
                        ref bestScore,
                        rng);

                    RevertPlacement(placement, pairSets, remainingSlots, cardsByPair, currentAssignments);
                }
            }

            SearchGuaranteedAssignments(
                pairIndex + 1,
                candidatePairIds,
                participantIds,
                pairSets,
                remainingSlots,
                cardsByPair,
                targetVisiblePairs,
                currentVisiblePairs,
                currentAssignments,
                bestAssignments,
                ref bestScore,
                rng);
        }

        private static int CountPotentialVisiblePairs(
            IReadOnlyList<int> candidatePairIds,
            int startIndex,
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets,
            IReadOnlyDictionary<ulong, int> remainingSlots,
            IReadOnlyDictionary<int, List<DistributedPairsCardModel>> cardsByPair)
        {
            var count = 0;
            for (var index = startIndex; index < candidatePairIds.Count; index++)
            {
                if (CanPairBecomeVisible(candidatePairIds[index], participantIds, pairSets, remainingSlots, cardsByPair))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool CanPairBecomeVisible(
            int pairId,
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets,
            IReadOnlyDictionary<ulong, int> remainingSlots,
            IReadOnlyDictionary<int, List<DistributedPairsCardModel>> cardsByPair)
        {
            if (!cardsByPair.TryGetValue(pairId, out var cards) || cards.Count == 0)
            {
                return false;
            }

            var owners = GetOwnersForPair(participantIds, pairSets, pairId);
            if (owners.Count >= 2)
            {
                return false;
            }

            var eligiblePlayers = GetEligiblePlayersForPair(participantIds, pairSets, remainingSlots, pairId, owners);
            if (owners.Count == 1)
            {
                return cards.Count >= 1 && eligiblePlayers.Count >= 1;
            }

            return cards.Count >= 2 && eligiblePlayers.Count >= 2;
        }

        private static bool TryBuildPairPlacements(
            int pairId,
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets,
            IReadOnlyDictionary<ulong, int> remainingSlots,
            IReadOnlyDictionary<int, List<DistributedPairsCardModel>> cardsByPair,
            Random rng,
            out List<GuaranteedPairPlacement> placements)
        {
            placements = null;

            if (!cardsByPair.TryGetValue(pairId, out var cards) || cards.Count == 0)
            {
                return false;
            }

            var owners = GetOwnersForPair(participantIds, pairSets, pairId);
            if (owners.Count >= 2)
            {
                return false;
            }

            var eligiblePlayers = GetEligiblePlayersForPair(participantIds, pairSets, remainingSlots, pairId, owners);
            if (owners.Count == 1)
            {
                if (eligiblePlayers.Count == 0)
                {
                    return false;
                }

                placements = eligiblePlayers
                    .OrderBy(_ => rng.Next())
                    .ThenBy(playerId => playerId)
                    .Select(playerId => new GuaranteedPairPlacement(
                        pairId,
                        new[]
                        {
                            new CardAssignment(cards[0], playerId)
                        }))
                    .ToList();

                return placements.Count > 0;
            }

            if (cards.Count < 2 || eligiblePlayers.Count < 2)
            {
                return false;
            }

            placements = new List<GuaranteedPairPlacement>();
            var shuffledEligiblePlayers = eligiblePlayers
                .OrderBy(_ => rng.Next())
                .ThenBy(playerId => playerId)
                .ToList();

            for (var firstIndex = 0; firstIndex < shuffledEligiblePlayers.Count - 1; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < shuffledEligiblePlayers.Count; secondIndex++)
                {
                    placements.Add(new GuaranteedPairPlacement(
                        pairId,
                        new[]
                        {
                            new CardAssignment(cards[0], shuffledEligiblePlayers[firstIndex]),
                            new CardAssignment(cards[1], shuffledEligiblePlayers[secondIndex])
                        }));
                }
            }

            return placements.Count > 0;
        }

        private static List<ulong> GetOwnersForPair(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets,
            int pairId)
        {
            var owners = new List<ulong>();
            foreach (var participantId in participantIds)
            {
                if (pairSets[participantId].Contains(pairId))
                {
                    owners.Add(participantId);
                }
            }

            return owners;
        }

        private static List<ulong> GetEligiblePlayersForPair(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets,
            IReadOnlyDictionary<ulong, int> remainingSlots,
            int pairId,
            IReadOnlyCollection<ulong> excludedOwners)
        {
            var players = new List<ulong>();
            foreach (var participantId in participantIds)
            {
                if (excludedOwners.Contains(participantId))
                {
                    continue;
                }

                if (remainingSlots[participantId] <= 0 || pairSets[participantId].Contains(pairId))
                {
                    continue;
                }

                players.Add(participantId);
            }

            return players;
        }

        private static void ApplyPlacement(
            GuaranteedPairPlacement placement,
            Dictionary<ulong, HashSet<int>> pairSets,
            Dictionary<ulong, int> remainingSlots,
            Dictionary<int, List<DistributedPairsCardModel>> cardsByPair,
            Dictionary<int, ulong> currentAssignments)
        {
            foreach (var assignment in placement.Assignments)
            {
                pairSets[assignment.OwnerClientId].Add(placement.PairId);
                remainingSlots[assignment.OwnerClientId]--;
                cardsByPair[placement.PairId].RemoveAll(card => card.CardInstanceId == assignment.Card.CardInstanceId);
                currentAssignments[assignment.Card.CardInstanceId] = assignment.OwnerClientId;
            }
        }

        private static void RevertPlacement(
            GuaranteedPairPlacement placement,
            Dictionary<ulong, HashSet<int>> pairSets,
            Dictionary<ulong, int> remainingSlots,
            Dictionary<int, List<DistributedPairsCardModel>> cardsByPair,
            Dictionary<int, ulong> currentAssignments)
        {
            foreach (var assignment in placement.Assignments)
            {
                currentAssignments.Remove(assignment.Card.CardInstanceId);
                cardsByPair[placement.PairId].Add(assignment.Card);
                remainingSlots[assignment.OwnerClientId]++;
                pairSets[assignment.OwnerClientId].Remove(placement.PairId);
            }

            cardsByPair[placement.PairId].Sort((left, right) => left.CardInstanceId.CompareTo(right.CardInstanceId));
        }

        private static Dictionary<ulong, int> ApplyAssignmentCounts(
            IReadOnlyDictionary<ulong, int> currentHandCounts,
            IReadOnlyDictionary<int, ulong> assignments)
        {
            var counts = currentHandCounts.ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach (var assignment in assignments)
            {
                counts[assignment.Value] += 1;
            }

            return counts;
        }

        private static Dictionary<ulong, IReadOnlyList<int>> BuildPairIdLists(
            IReadOnlyDictionary<ulong, HashSet<int>> pairSets)
        {
            return pairSets.ToDictionary(
                entry => entry.Key,
                entry => (IReadOnlyList<int>)entry.Value.ToList());
        }

        private static int CountVisiblePairs(IReadOnlyDictionary<ulong, HashSet<int>> pairSets)
        {
            var ownerCountsByPair = new Dictionary<int, int>();
            foreach (var pairSet in pairSets.Values)
            {
                foreach (var pairId in pairSet)
                {
                    ownerCountsByPair[pairId] = ownerCountsByPair.TryGetValue(pairId, out var count)
                        ? count + 1
                        : 1;
                }
            }

            return ownerCountsByPair.Values.Count(count => count >= 2);
        }

        private static int CalculateGuaranteedVisiblePairCount(
            int deviceCount,
            int visibleCardCount,
            int guaranteedVisiblePairsOffset)
        {
            if (deviceCount <= 0 || visibleCardCount <= 1)
            {
                return 0;
            }

            var offset = Math.Max(1, guaranteedVisiblePairsOffset);
            var desiredPairCount = Math.Max(1, deviceCount - offset);
            return Math.Clamp(desiredPairCount, 1, visibleCardCount / 2);
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
                    CopyAssignments(currentAssignments, bestAssignments);
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

        private static void CopyAssignments(
            IReadOnlyDictionary<int, ulong> source,
            IDictionary<int, ulong> target)
        {
            target.Clear();
            foreach (var assignment in source)
            {
                target[assignment.Key] = assignment.Value;
            }
        }

        private readonly struct CardAssignment
        {
            public CardAssignment(DistributedPairsCardModel card, ulong ownerClientId)
            {
                Card = card;
                OwnerClientId = ownerClientId;
            }

            public DistributedPairsCardModel Card { get; }
            public ulong OwnerClientId { get; }
        }

        private readonly struct GuaranteedPairPlacement
        {
            public GuaranteedPairPlacement(int pairId, IReadOnlyList<CardAssignment> assignments)
            {
                PairId = pairId;
                Assignments = assignments;
            }

            public int PairId { get; }
            public IReadOnlyList<CardAssignment> Assignments { get; }
        }

        private readonly struct GuaranteeScore
        {
            public GuaranteeScore(int cappedVisiblePairs, int assignmentCount)
            {
                CappedVisiblePairs = cappedVisiblePairs;
                AssignmentCount = assignmentCount;
            }

            public int CappedVisiblePairs { get; }
            public int AssignmentCount { get; }

            public bool IsBetterThan(GuaranteeScore other)
            {
                if (CappedVisiblePairs != other.CappedVisiblePairs)
                {
                    return CappedVisiblePairs > other.CappedVisiblePairs;
                }

                return AssignmentCount < other.AssignmentCount;
            }
        }
    }
}
