using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusWordAssignmentServiceTests
    {
        [Test]
        public void TryBuildAssignments_DistributesOptionsAcrossReceiversInsteadOfDuplicatingFullSet()
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
            Assert.That(assignments.Values.All(words => words.Count >= 1), Is.True);
            Assert.That(assignments.Values.All(words => words.Count == 1), Is.True);

            var allAssignedWords = assignments.Values.SelectMany(words => words).ToList();
            Assert.That(allAssignedWords, Is.EquivalentTo(new[] { "Campana", "Hoja", "Piedra", "Agua" }));
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
            var allAssignedWords = assignments.Values.SelectMany(words => words).ToList();
            Assert.That(allAssignedWords, Does.Contain("Campana"));
            Assert.That(allAssignedWords, Does.Contain("hoja"));
            Assert.That(allAssignedWords, Does.Contain("piedra"));
            Assert.That(allAssignedWords.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(allAssignedWords.Count));
        }

        [Test]
        public void TryBuildAssignments_WhenThereAreMoreReceiversThanWords_ReusesWordsButKeepsAtLeastOnePerDevice()
        {
            var receivers = new ulong[] { 1UL, 2UL, 3UL };

            var success = AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receivers,
                "Campana",
                new[] { "Hoja" },
                randomSeed: 7,
                out var assignments);

            Assert.That(success, Is.True);
            Assert.That(assignments.Keys, Is.EquivalentTo(receivers));
            Assert.That(assignments.Values.All(words => words.Count >= 1), Is.True);
            Assert.That(assignments.Values.SelectMany(words => words), Does.Contain("Campana"));
            Assert.That(assignments.Values.SelectMany(words => words), Does.Contain("Hoja"));
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
