using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusScoreServiceTests
    {
        [Test]
        public void CreateResult_AllRoundsCorrect_ReturnsTenOutOfTen()
        {
            var config = CreateConfig();
            var roundResults = new[]
            {
                new AudioWordConsensusRoundScoreEntry(true, 0),
                new AudioWordConsensusRoundScoreEntry(true, 0),
                new AudioWordConsensusRoundScoreEntry(true, 0),
                new AudioWordConsensusRoundScoreEntry(true, 0)
            };

            var result = AudioWordConsensusScoreService.CreateResult(
                config,
                roundResults,
                correctRounds: 4,
                incorrectRounds: 0,
                totalScheduledRounds: 4,
                completedAllRounds: true);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
            Assert.That(result.Message, Is.EqualTo(config.SuccessMessage));
        }

        [Test]
        public void CreateResult_TimeoutUsesScheduledRoundsAsBase()
        {
            var config = CreateConfig();
            var roundResults = new[]
            {
                new AudioWordConsensusRoundScoreEntry(true, 1),
                new AudioWordConsensusRoundScoreEntry(true, 2),
                new AudioWordConsensusRoundScoreEntry(false, 3)
            };

            var result = AudioWordConsensusScoreService.CreateResult(
                config,
                roundResults,
                correctRounds: 2,
                incorrectRounds: 1,
                totalScheduledRounds: 5,
                completedAllRounds: false);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(2.5f));
            Assert.That(result.Message, Is.EqualTo(config.TimeoutMessage));
        }

        [Test]
        public void CreateResult_NullConfig_UsesFallbackMessage()
        {
            var roundResults = new[]
            {
                new AudioWordConsensusRoundScoreEntry(true, 1),
                new AudioWordConsensusRoundScoreEntry(false, 3)
            };

            var result = AudioWordConsensusScoreService.CreateResult(
                config: null,
                roundResults: roundResults,
                correctRounds: 1,
                incorrectRounds: 1,
                totalScheduledRounds: 4,
                completedAllRounds: false);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(1.9f));
            Assert.That(result.Message, Is.EqualTo("Minijuego completado"));
        }

        [TestCase(true, 0, 1f)]
        [TestCase(true, 1, 0.75f)]
        [TestCase(true, 2, 0.5f)]
        [TestCase(true, 3, 0.25f)]
        [TestCase(false, 3, 0f)]
        public void CalculateRoundScoreRatio_ReturnsExpectedPenaltyByMistakes(bool wasSolved, int mistakeCount, float expectedRatio)
        {
            var ratio = AudioWordConsensusScoreService.CalculateRoundScoreRatio(
                wasSolved,
                mistakeCount,
                AudioWordConsensusMinigameConfig.DefaultMaxMistakesPerRound);

            Assert.That(ratio, Is.EqualTo(expectedRatio).Within(0.0001f));
        }

        private static AudioWordConsensusMinigameConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("successMessage").stringValue = "Rondas completadas";
            serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }
    }
}
