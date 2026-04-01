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

            var result = AudioWordConsensusScoreService.CreateResult(
                config,
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

            var result = AudioWordConsensusScoreService.CreateResult(
                config,
                correctRounds: 2,
                incorrectRounds: 1,
                totalScheduledRounds: 5,
                completedAllRounds: false);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(4f));
            Assert.That(result.Message, Is.EqualTo(config.TimeoutMessage));
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
