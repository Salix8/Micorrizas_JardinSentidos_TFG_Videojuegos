using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsDistributionServiceTests
    {
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
                randomSeed: 42);

            Assert.That(assignments.Count, Is.EqualTo(12));

            var cardsById = deck.ToDictionary(card => card.CardInstanceId, card => card);
            foreach (var participantId in participantIds)
            {
                var pairIds = assignments
                    .Where(assignment => assignment.Value == participantId)
                    .Select(assignment => cardsById[assignment.Key].PairId)
                    .ToList();

                Assert.That(pairIds.Count, Is.EqualTo(pairIds.Distinct().Count()));
            }
        }

        [Test]
        public void TopUpDistribution_LeavesPlayerUnderTargetWhenNoValidCardExists()
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
                new(100, 0)
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                availableCards,
                targetHandSize: 4,
                randomSeed: 7);

            Assert.That(assignments.Count, Is.EqualTo(1));
            Assert.That(assignments[100], Is.EqualTo(2UL));
        }

        [Test]
        public void PlanAssignments_NeverAssignsMoreCardsThanMissingSlots()
        {
            var participantIds = new ulong[] { 1UL, 2UL, 3UL };
            var currentHandPairIds = participantIds.ToDictionary(id => id, _ => (IReadOnlyList<int>)new List<int>());
            var currentHandCounts = new Dictionary<ulong, int>
            {
                [1UL] = 3,
                [2UL] = 1,
                [3UL] = 0
            };

            var assignments = DistributedPairsDistributionService.PlanAssignments(
                participantIds,
                currentHandPairIds,
                currentHandCounts,
                BuildDeck(pairCount: 8),
                targetHandSize: 4,
                randomSeed: 11);

            Assert.That(assignments.Values.Count(id => id == 1UL), Is.LessThanOrEqualTo(1));
            Assert.That(assignments.Values.Count(id => id == 2UL), Is.LessThanOrEqualTo(3));
            Assert.That(assignments.Values.Count(id => id == 3UL), Is.LessThanOrEqualTo(4));
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
                randomSeed: 1);

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
                randomSeed: 1);

            Assert.That(assignmentsWithoutPlayers, Is.Empty);
            Assert.That(assignmentsWithoutCards, Is.Empty);
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
