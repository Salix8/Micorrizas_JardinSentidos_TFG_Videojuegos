using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsDistributionServiceTests
    {
        private const int GuaranteedVisiblePairsOffset = 1;

        [Test]
        public void InitialDistribution_NeverAssignsBothCardsOfSamePairToSamePlayer()
        {
            var participantIds = new ulong[] { 1UL, 2UL, 3UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = participantIds.ToDictionary(id => id, _ => 0);
            var deck = BuildDeck(pairCount: 10);

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                deck,
                targetHandSize: 4,
                randomSeed: 42,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments.Count, Is.EqualTo(12));
            AssertNoPlayerReceivesDuplicatePair(assignments, deck);
        }

        [Test]
        public void InitialDistribution_TwoDevices_GuaranteesAtLeastOneVisiblePair()
        {
            var participantIds = new ulong[] { 1UL, 2UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = participantIds.ToDictionary(id => id, _ => 0);
            var deck = BuildDeck(pairCount: 10);

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                deck,
                targetHandSize: 4,
                randomSeed: 7,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments.Count, Is.EqualTo(8));
            Assert.That(CountVisiblePairs(assignments, deck), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void InitialDistribution_FourDevices_GuaranteesAtLeastThreeVisiblePairs()
        {
            var participantIds = new ulong[] { 1UL, 2UL, 3UL, 4UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = participantIds.ToDictionary(id => id, _ => 0);
            var deck = BuildDeck(pairCount: 10);

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                deck,
                targetHandSize: 4,
                randomSeed: 23,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments.Count, Is.EqualTo(16));
            Assert.That(CountVisiblePairs(assignments, deck), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void PlanAssignments_UsesSeedToVaryVisibleHands()
        {
            var participantIds = new ulong[] { 1UL, 2UL, 3UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = participantIds.ToDictionary(id => id, _ => 0);
            var deck = BuildDeck(pairCount: 10);

            var snapshots = new HashSet<string>();
            for (var seed = 0; seed < 8; seed++)
            {
                var assignments = DistributedPairsDistributionService.PlanAssignments(
                    participantIds,
                    currentHandPairIds,
                    currentHandCounts,
                    deck,
                    targetHandSize: 4,
                    randomSeed: seed,
                    guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

                snapshots.Add(string.Join(
                    "|",
                    assignments
                        .OrderBy(entry => entry.Key)
                        .Select(entry => $"{entry.Key}:{entry.Value}")));
            }

            Assert.That(snapshots.Count, Is.GreaterThan(1));
        }

        [Test]
        public void PlanAssignments_UsesOnlyCardsFromAvailablePool()
        {
            var participantIds = new ulong[] { 1UL, 2UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = participantIds.ToDictionary(id => id, _ => 0);
            var availableCards = new List<DistributedPairsCardModel>
            {
                new(10, 0),
                new(11, 0),
                new(20, 1),
                new(21, 1),
                new(30, 2),
                new(31, 2),
                new(40, 3),
                new(41, 3)
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                availableCards,
                targetHandSize: 4,
                randomSeed: 19,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            var availableCardIds = availableCards.Select(card => card.CardInstanceId).ToHashSet();
            Assert.That(assignments.Keys.All(availableCardIds.Contains), Is.True);
        }

        [Test]
        public void TopUpDistribution_LeavesPlayerUnderTargetWhenNoValidCardExists()
        {
            var participantIds = new ulong[] { 1UL, 2UL };
            var currentHandPairIds = new Dictionary<ulong, IReadOnlyList<int>>
            {
                [1UL] = new List<int> { 0, 1, 2, 3 },
                [2UL] = new List<int> { 0, 4, 5 }
            };
            var currentHandCounts = new Dictionary<ulong, int>
            {
                [1UL] = 4,
                [2UL] = 3
            };

            var availableCards = new List<DistributedPairsCardModel>
            {
                new(100, 0)
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                availableCards,
                targetHandSize: 4,
                randomSeed: 7,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments, Is.Empty);
        }

        [Test]
        public void TopUpDistribution_MaintainsMinimumVisiblePairsWhenDeckAllows()
        {
            var participantIds = new ulong[] { 1UL, 2UL };
            var currentHandPairIds = new Dictionary<ulong, IReadOnlyList<int>>
            {
                [1UL] = new List<int> { 0, 1, 2 },
                [2UL] = new List<int> { 3, 4, 5 }
            };
            var currentHandCounts = new Dictionary<ulong, int>
            {
                [1UL] = 3,
                [2UL] = 3
            };
            var availableCards = new List<DistributedPairsCardModel>
            {
                new(100, 6),
                new(101, 6)
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                availableCards,
                targetHandSize: 4,
                randomSeed: 11,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(CountVisiblePairs(assignments, availableCards), Is.EqualTo(1));
        }

        [Test]
        public void PlanAssignments_ReturnsBestValidDistributionWhenTargetCannotBeMet()
        {
            var participantIds = new ulong[] { 1UL, 2UL, 3UL };
            var currentHandPairIds = new Dictionary<ulong, IReadOnlyList<int>>
            {
                [1UL] = new List<int> { 0, 1, 2 },
                [2UL] = new List<int> { 3, 4, 5 },
                [3UL] = new List<int> { 6, 7, 8 }
            };
            var currentHandCounts = new Dictionary<ulong, int>
            {
                [1UL] = 3,
                [2UL] = 3,
                [3UL] = 3
            };
            var availableCards = new List<DistributedPairsCardModel>
            {
                new(100, 9),
                new(101, 9),
                new(102, 10)
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                availableCards,
                targetHandSize: 4,
                randomSeed: 13,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignments.Count, Is.EqualTo(3));
            Assert.That(CountVisiblePairs(assignments, availableCards), Is.EqualTo(1));
            AssertNoPlayerReceivesDuplicatePair(assignments, availableCards);
        }

        [Test]
        public void PlanAssignments_WithoutParticipantsOrCards_ReturnsEmptyAssignments()
        {
            var assignmentsWithoutPlayers = DistributedPairsDistributionService.PlanAssignments(
                Array.Empty<ulong>(),
                new Dictionary<ulong, IReadOnlyList<int>>(),
                new Dictionary<ulong, int>(),
                BuildDeck(pairCount: 2),
                targetHandSize: 4,
                randomSeed: 1,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            var assignmentsWithoutCards = DistributedPairsDistributionService.PlanAssignments(
                new ulong[] { 1UL, 2UL },
                new Dictionary<ulong, IReadOnlyList<int>>
                {
                    [1UL] = new List<int>(),
                    [2UL] = new List<int>()
                },
                new Dictionary<ulong, int>
                {
                    [1UL] = 0,
                    [2UL] = 0
                },
                new List<DistributedPairsCardModel>(),
                targetHandSize: 4,
                randomSeed: 1,
                guaranteedVisiblePairsOffset: GuaranteedVisiblePairsOffset);

            Assert.That(assignmentsWithoutPlayers, Is.Empty);
            Assert.That(assignmentsWithoutCards, Is.Empty);
        }

        private static int CountVisiblePairs(
            IReadOnlyDictionary<int, ulong> assignments,
            IReadOnlyList<DistributedPairsCardModel> cards)
        {
            var cardsById = cards.ToDictionary(card => card.CardInstanceId, card => card.PairId);
            return assignments
                .GroupBy(entry => cardsById[entry.Key])
                .Count(group => group.Select(entry => entry.Value).Distinct().Count() >= 2);
        }

        private static void AssertNoPlayerReceivesDuplicatePair(
            IReadOnlyDictionary<int, ulong> assignments,
            IReadOnlyList<DistributedPairsCardModel> cards)
        {
            var cardsById = cards.ToDictionary(card => card.CardInstanceId, card => card);
            foreach (var participantGroup in assignments.GroupBy(entry => entry.Value))
            {
                var pairIds = participantGroup
                    .Select(entry => cardsById[entry.Key].PairId)
                    .ToList();

                Assert.That(pairIds.Count, Is.EqualTo(pairIds.Distinct().Count()));
            }
        }

        private static List<DistributedPairsCardModel> BuildDeck(int pairCount)
        {
            var cards = new List<DistributedPairsCardModel>();
            var cardId = 0;
            for (var pairId = 0; pairId < pairCount; pairId++)
            {
                cards.Add(new DistributedPairsCardModel(cardId++, pairId));
                cards.Add(new DistributedPairsCardModel(cardId++, pairId));
            }

            return cards;
        }
    }
}
