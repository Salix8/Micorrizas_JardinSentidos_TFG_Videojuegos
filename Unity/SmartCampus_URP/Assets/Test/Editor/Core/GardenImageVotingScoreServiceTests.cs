using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenImageVoting;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenImageVotingScoreServiceTests
    {
        [Test]
        public void CreateResult_AllCorrectAnswers_ReturnsMaximumScore()
        {
            var config = CreateConfig();

            var result = GardenImageVotingScoreService.CreateResult(
                config,
                correctAnswers: 10,
                incorrectAnswers: 0,
                totalScheduledCards: 10,
                completedAllCards: true);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
            Assert.That(result.Message, Is.EqualTo(config.SuccessMessage));
        }

        [Test]
        public void CreateResult_PartialSuccessOnTimeout_ScalesScoreToBaseTen()
        {
            var config = CreateConfig();

            var result = GardenImageVotingScoreService.CreateResult(
                config,
                correctAnswers: 3,
                incorrectAnswers: 2,
                totalScheduledCards: 10,
                completedAllCards: false);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(3f));
            Assert.That(result.Message, Is.EqualTo(config.TimeoutMessage));
        }

        private static GardenImageVotingMinigameConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<GardenImageVotingMinigameConfig>();
            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("successMessage").stringValue = "Secuencia completada";
            serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }
    }
}
