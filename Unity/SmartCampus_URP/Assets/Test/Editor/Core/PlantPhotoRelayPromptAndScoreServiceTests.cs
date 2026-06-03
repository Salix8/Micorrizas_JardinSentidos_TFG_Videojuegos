using NUnit.Framework;
using SmartCampus.Coop.Minigames.PlantPhotoRelay;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class PlantPhotoRelayPromptAndScoreServiceTests
    {
        [Test]
        public void BuildPrompt_UsesConfiguredAttributes()
        {
            var definition = new PlantPhotoRelayPlantDefinition("rosal", "Rosal", new string[0], "Arbusto", "rugosa", true, true, "compuesta", "mediano");

            var prompt = PlantPhotoRelayPromptService.BuildPrompt(definition);

            Assert.That(prompt, Does.Contain("Arbusto"));
            Assert.That(prompt, Does.Contain("rugosa"));
            Assert.That(prompt, Does.Contain("con pinchos"));
        }

        [Test]
        public void ComputeRoundScore_AddsPromptBonusOnlyOnExactMatch()
        {
            var config = ScriptableObject.CreateInstance<PlantPhotoRelayMinigameConfig>();
            var serializedObject = new UnityEditor.SerializedObject(config);
            serializedObject.FindProperty("scoreExactMatch").floatValue = 2.5f;
            serializedObject.FindProperty("scorePromptMatchBonus").floatValue = 0.5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var exactScore = PlantPhotoRelayScoreService.ComputeRoundScore(true, true, config);
            var mismatchScore = PlantPhotoRelayScoreService.ComputeRoundScore(false, true, config);

            Assert.That(exactScore, Is.EqualTo(3f));
            Assert.That(mismatchScore, Is.EqualTo(0f));

            Object.DestroyImmediate(config);
        }
    }
}
