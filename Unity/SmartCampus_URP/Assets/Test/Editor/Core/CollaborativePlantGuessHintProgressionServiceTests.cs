using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessHintProgressionServiceTests
    {
        private static CollaborativePlantGuessPlantDefinition CreatePlant()
        {
            return new CollaborativePlantGuessPlantDefinition(
                "olivo",
                "Olivo",
                "Olea europaea",
                new[] { "Aceituno" },
                string.Empty,
                "Arbol",
                "Media",
                3,
                "Lanceolada",
                "Carnoso",
                "Drupa");
        }

        private static CollaborativePlantGuessMinigameConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("leafTypeRevealAttempt").intValue = 3;
            serialized.FindProperty("fruitDetailRevealAttempt").intValue = 5;
            serialized.FindProperty("plantTypeRevealAttempt").intValue = 7;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        [Test]
        public void GetLeafTypeDisplayValue_BeforeThirdAttempt_ReturnsQuestionMark()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetLeafTypeDisplayValue(CreatePlant(), 2, CreateConfig());

            Assert.That(value, Is.EqualTo("?"));
        }

        [Test]
        public void GetLeafTypeDisplayValue_FromThirdAttempt_ReturnsLeafType()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetLeafTypeDisplayValue(CreatePlant(), 3, CreateConfig());

            Assert.That(value, Is.EqualTo("Lanceolada"));
        }

        [Test]
        public void GetFruitDisplayValue_BeforeFifthAttempt_ReturnsOnlyCategory()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetFruitDisplayValue(CreatePlant(), 4, CreateConfig());

            Assert.That(value, Is.EqualTo("Carnoso"));
        }

        [Test]
        public void GetFruitDisplayValue_FromFifthAttempt_ReturnsCategoryAndType()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetFruitDisplayValue(CreatePlant(), 5, CreateConfig());

            Assert.That(value, Is.EqualTo("Carnoso / Drupa"));
        }

        [Test]
        public void GetPlantTypeDisplayValue_BeforeSeventhAttempt_ReturnsQuestionMark()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetPlantTypeDisplayValue(CreatePlant(), 6, CreateConfig());

            Assert.That(value, Is.EqualTo("?"));
        }

        [Test]
        public void GetPlantTypeDisplayValue_FromSeventhAttempt_ReturnsPlantType()
        {
            var value = CollaborativePlantGuessHintProgressionService.GetPlantTypeDisplayValue(CreatePlant(), 7, CreateConfig());

            Assert.That(value, Is.EqualTo("Arbol"));
        }
    }
}
