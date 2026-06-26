using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessHintProgressionServiceTests
    {
        private static CollaborativePlantGuessMinigameConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("leafTypeRevealAttempt").intValue = 1;
            serialized.FindProperty("fruitDetailRevealAttempt").intValue = 2;
            serialized.FindProperty("plantTypeRevealAttempt").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        [Test]
        public void LockedValue_RemainsQuestionMark()
        {
            Assert.That(CollaborativePlantGuessHintProgressionService.LockedValue, Is.EqualTo("?"));
        }

        [Test]
        public void FruitType_RevealsFromSecondAttempt()
        {
            var config = CreateConfig();

            Assert.That(CollaborativePlantGuessHintProgressionService.ShouldRevealFruitType(1, config), Is.False);
            Assert.That(CollaborativePlantGuessHintProgressionService.ShouldRevealFruitType(2, config), Is.True);
        }

        [Test]
        public void FruitCategory_RevealsFromFirstAttempt()
        {
            var config = CreateConfig();

            Assert.That(CollaborativePlantGuessHintProgressionService.ShouldRevealFruitCategory(1, config), Is.True);
        }

        [Test]
        public void PlantType_RevealsFromFourthAttempt()
        {
            var config = CreateConfig();

            Assert.That(CollaborativePlantGuessHintProgressionService.ShouldRevealPlantType(3, config), Is.False);
            Assert.That(CollaborativePlantGuessHintProgressionService.ShouldRevealPlantType(4, config), Is.True);
        }
    }
}
