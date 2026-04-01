using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusWordAssignmentServiceTests
    {
        [Test]
        public void TryBuildAssignments_AssignsCorrectWordToExactlyOneReceiver()
        {
            var receivers = new ulong[] { 1UL, 2UL, 3UL, 4UL };

            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receivers,
                "Campana",
                new[] { "Hoja", "Piedra", "Agua" },
                randomSeed: 25,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments.Count, Is.EqualTo(receivers.Length));
            Assert.That(assignments.Values.Count(word => word == "Campana"), Is.EqualTo(1));
            Assert.That(assignments.Values.Distinct().Count(), Is.EqualTo(receivers.Length));
        }

        [Test]
        public void TryBuildAssignments_TwoPlayerRound_DoesNotRequireDistractors()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 7UL },
                "Arroyo",
                new List<string>(),
                randomSeed: 1,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments[7UL], Is.EqualTo("Arroyo"));
        }

        [Test]
        public void TryBuildAssignments_ReturnsFalse_WhenDistractorsAreInsufficient()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 1UL, 2UL, 3UL },
                "Micorriza",
                new[] { "Suelo" },
                randomSeed: 9,
                out _);

            Assert.That(success, Is.False);
        }

        [Test]
        public void TryBuildAssignments_TrimsAndDeduplicatesDistractors()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 1UL, 2UL, 3UL },
                " Campana ",
                new[] { " hoja ", "Hoja", "campana", " piedra " },
                randomSeed: 3,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments.Values.Count(word => word == "Campana"), Is.EqualTo(1));
            Assert.That(assignments.Values, Does.Contain("hoja"));
            Assert.That(assignments.Values.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(assignments.Count));
        }

        [Test]
        public void TryBuildAssignments_InvalidInput_ReturnsFalse()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receiverClientIds: new ulong[0],
                correctWord: "Campana",
                distractorWords: new[] { "Hoja" },
                randomSeed: 0,
                out _);

            Assert.That(success, Is.False);
        }
    }
}
