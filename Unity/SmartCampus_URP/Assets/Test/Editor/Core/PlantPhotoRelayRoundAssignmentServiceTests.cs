using NUnit.Framework;
using SmartCampus.Coop.Minigames.PlantPhotoRelay;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class PlantPhotoRelayRoundAssignmentServiceTests
    {
        [Test]
        public void CreateAssignment_RotatesPhotographerAndGuesser()
        {
            var participants = new ulong[] { 10UL, 20UL, 30UL };

            var assignmentRound0 = PlantPhotoRelayRoundAssignmentService.CreateAssignment(participants, 0);
            var assignmentRound1 = PlantPhotoRelayRoundAssignmentService.CreateAssignment(participants, 1);

            Assert.That(assignmentRound0.PhotographerId, Is.EqualTo(10UL));
            Assert.That(assignmentRound0.GuesserId, Is.EqualTo(20UL));
            Assert.That(assignmentRound1.PhotographerId, Is.EqualTo(20UL));
            Assert.That(assignmentRound1.GuesserId, Is.EqualTo(30UL));
        }
    }
}
