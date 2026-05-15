using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusWordAssignmentServiceTests
    {
        [Test]
        public void TryBuildAssignments_AssignsSameFullOptionSetToAllReceivers()
        {
            var receivers = new ulong[] { 1UL, 2UL, 3UL, 4UL };

            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receivers,
                "Campana",
                new[] { "Hoja", "Piedra", "Agua" },
                randomSeed: 25,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments.Keys, Is.EquivalentTo(receivers));

            var firstAssignment = assignments[receivers[0]];
            Assert.That(firstAssignment, Has.Count.EqualTo(4));
            Assert.That(firstAssignment, Does.Contain("Campana"));

            foreach (var receiver in receivers)
            {
                Assert.That(assignments[receiver], Is.EqualTo(firstAssignment));
            }
        }

        [Test]
        public void TryBuildAssignments_TwoPlayerRound_AssignsAllOptionsToOnlyReceiver()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 7UL },
                "Arroyo",
                new List<string> { "Hoja", "Piedra" },
                randomSeed: 1,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments.Count, Is.EqualTo(1));
            Assert.That(assignments[7UL], Is.EquivalentTo(new[] { "Arroyo", "Hoja", "Piedra" }));
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
            Assert.That(assignments[1UL], Does.Contain("Campana"));
            Assert.That(assignments[1UL], Does.Contain("hoja"));
            Assert.That(assignments[1UL], Does.Contain("piedra"));
            Assert.That(assignments[1UL].Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(assignments[1UL].Count));
        }

        [Test]
        public void TryBuildAssignments_ReturnsFalse_WhenSeveralReceiversHaveNoDistractors()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 1UL, 2UL },
                "Micorriza",
                new string[0],
                randomSeed: 9,
                out _);

            Assert.That(success, Is.False);
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

        [Test]
        public void CountDistinctOptionWords_TrimsAndDeduplicatesWords()
        {
            var count = AudioWordConsensusWordAssignmentService.CountDistinctOptionWords(
                " Campana ",
                new[] { " hoja ", "Hoja", "campana", " piedra " });

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void BuildShuffledOptionWords_UsesStableSeededOrder()
        {
            var first = AudioWordConsensusWordAssignmentService.BuildShuffledOptionWords(
                "Campana",
                new[] { "Hoja", "Piedra", "Agua" },
                randomSeed: 42);
            var second = AudioWordConsensusWordAssignmentService.BuildShuffledOptionWords(
                "Campana",
                new[] { "Hoja", "Piedra", "Agua" },
                randomSeed: 42);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Does.Contain("Campana"));
            Assert.That(first.Count, Is.EqualTo(4));
        }
    }
}
