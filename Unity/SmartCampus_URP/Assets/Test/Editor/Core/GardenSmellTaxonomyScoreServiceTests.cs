using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenSmellTaxonomyScoreServiceTests
    {
        [Test]
        public void CreateResult_AllPlantsResolvedCorrectly_ReturnsMaximumScore()
        {
            var config = CreateConfig();

            var result = GardenSmellTaxonomyScoreService.CreateResult(
                config,
                correctAnswers: 9,
                incorrectAnswers: 0,
                totalPlants: 9,
                completedAllPlants: true);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
            Assert.That(result.Message, Is.EqualTo(config.SuccessMessage));
        }

        [Test]
        public void CreateResult_TimeoutWithErrors_AppliesCompletionAndAccuracyWeights()
        {
            var config = CreateConfig();

            var result = GardenSmellTaxonomyScoreService.CreateResult(
                config,
                correctAnswers: 3,
                incorrectAnswers: 2,
                totalPlants: 10,
                completedAllPlants: false);

            Assert.That(result.ScoreOutOfTen, Is.InRange(5.6f, 5.7f));
            Assert.That(result.Message, Is.EqualTo(config.TimeoutMessage));
        }

        private static GardenSmellTaxonomyMinigameConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<GardenSmellTaxonomyMinigameConfig>();
            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("successMessage").stringValue = "Taxonomia completada";
            serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }
    }
}
