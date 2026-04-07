using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusWordAssignmentServiceTests
    {
        [Test]
        public void TryBuildAssignments_AssignsCorrectWordExactlyOnceAcrossAllReceivers()
        {
            var receivers = new ulong[] { 1UL, 2UL, 3UL, 4UL };

            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receivers,
                "Campana",
                new[] { "Hoja", "Piedra", "Agua" },
                randomSeed: 25,
                out var assignments);

            var assignedWords = assignments.Values.SelectMany(words => words).ToList();

            Assert.That(success, Is.True);
            Assert.That(assignments.Keys, Is.EquivalentTo(receivers));
            Assert.That(assignedWords.Count(word => word == "Campana"), Is.EqualTo(1));
            Assert.That(assignedWords.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(assignedWords.Count));
        }

        [Test]
        public void TryBuildAssignments_UsesAllConfiguredDistractorsAcrossReceiverDevices()
        {
            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                new ulong[] { 10UL, 20UL },
                "Arroyo",
                new[] { "Hoja", "Piedra", "Agua", "Musgo" },
                randomSeed: 4,
                out var assignments);

            var assignedWords = assignments.Values.SelectMany(words => words).ToList();

            Assert.That(success, Is.True);
            Assert.That(assignedWords, Does.Contain("Arroyo"));
            Assert.That(assignedWords, Does.Contain("Hoja"));
            Assert.That(assignedWords, Does.Contain("Piedra"));
            Assert.That(assignedWords, Does.Contain("Agua"));
            Assert.That(assignedWords, Does.Contain("Musgo"));
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

            var assignedWords = assignments.Values.SelectMany(words => words).ToList();

            Assert.That(success, Is.True);
            Assert.That(assignedWords.Count(word => word == "Campana"), Is.EqualTo(1));
            Assert.That(assignedWords, Does.Contain("hoja"));
            Assert.That(assignedWords, Does.Contain("piedra"));
            Assert.That(assignedWords.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(assignedWords.Count));
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
