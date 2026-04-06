using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsHandSlotServiceTests
    {
        [Test]
        public void BuildSlots_AlwaysReturnsFixedSlotCountAndLeavesEmptyHoles()
        {
            var handStates = new[]
            {
                new DistributedPairsCardNetworkState { CardInstanceId = 10, PairId = 2, HandOrder = 0, Zone = DistributedPairsCardZone.Hand },
                new DistributedPairsCardNetworkState { CardInstanceId = 11, PairId = 5, HandOrder = 2, Zone = DistributedPairsCardZone.Hand }
            };

            var slots = DistributedPairsHandSlotService.BuildSlots(handStates, slotCount: 4);

            Assert.That(slots.Count, Is.EqualTo(4));
            Assert.That(slots[0].HasCard, Is.True);
            Assert.That(slots[0].CardState.CardInstanceId, Is.EqualTo(10));
            Assert.That(slots[1].HasCard, Is.False);
            Assert.That(slots[2].HasCard, Is.True);
            Assert.That(slots[2].CardState.CardInstanceId, Is.EqualTo(11));
            Assert.That(slots[3].HasCard, Is.False);
        }

        [Test]
        public void BuildSlots_FallsBackWhenHandOrderIsInvalidOrDuplicated()
        {
            var handStates = new[]
            {
                new DistributedPairsCardNetworkState { CardInstanceId = 20, PairId = 1, HandOrder = 9, Zone = DistributedPairsCardZone.Hand },
                new DistributedPairsCardNetworkState { CardInstanceId = 21, PairId = 2, HandOrder = 0, Zone = DistributedPairsCardZone.Hand },
                new DistributedPairsCardNetworkState { CardInstanceId = 22, PairId = 3, HandOrder = 0, Zone = DistributedPairsCardZone.Hand }
            };

            var slots = DistributedPairsHandSlotService.BuildSlots(handStates, slotCount: 4);

            Assert.That(slots[0].CardState.CardInstanceId, Is.EqualTo(21));
            Assert.That(slots[1].CardState.CardInstanceId, Is.EqualTo(20));
            Assert.That(slots[2].CardState.CardInstanceId, Is.EqualTo(22));
            Assert.That(slots[3].HasCard, Is.False);
        }
    }
}
