using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessComparisonAndScoreTests
    {
        private static CollaborativePlantGuessPlantDefinition CreatePlant(
            string plantId,
            string commonName,
            string scientificName,
            string plantType = "Arbol",
            string surfaceRoughness = "Media",
            string leafType = "Simple",
            string fruitCategory = "Carnoso",
            string fruitType = "Baya")
        {
            return new CollaborativePlantGuessPlantDefinition(
                plantId,
                commonName,
                scientificName,
                new string[0],
                string.Empty,
                plantType,
                surfaceRoughness,
                leafType,
                fruitCategory,
                fruitType);
        }

        [Test]
        public void Evaluate_CloseRoughnessAndFruitCategory_ReturnsExpectedOutcomes()
        {
            var targetPlant = CreatePlant(
                "laurel",
                "Laurel",
                "Laurus nobilis",
                plantType: "Arbusto",
                surfaceRoughness: "Rugosa",
                leafType: "Lanceolada",
                fruitCategory: "Carnoso",
                fruitType: "Drupa");
            var guessedPlant = CreatePlant(
                "madrono",
                "Madrono",
                "Arbutus unedo",
                plantType: "Arbol",
                surfaceRoughness: "Media",
                leafType: "Ovalada",
                fruitCategory: "Carnoso",
                fruitType: "Baya");

            var evaluation = CollaborativePlantGuessComparisonService.Evaluate(targetPlant, guessedPlant);

            Assert.That(evaluation.IsExactPlantMatch, Is.False);
            Assert.That(evaluation.PlantTypeOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Incorrect));
            Assert.That(evaluation.SurfaceRoughnessOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Incorrect));
            Assert.That(evaluation.LeafTypeOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Incorrect));
            Assert.That(evaluation.FruitCategoryOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Exact));
            Assert.That(evaluation.FruitTypeOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Incorrect));
        }

        [Test]
        public void CreateResult_SolvedOnFirstTry_ReturnsMaximumScore()
        {
            var config = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();
            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("successMessage").stringValue = "Planta encontrada";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = CollaborativePlantGuessScoreService.CreateResult(
                config,
                wasSolved: true,
                attemptsUsed: 1,
                maxAttempts: 8,
                resultMessage: "Planta encontrada: Encina");

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
        }

        [Test]
        public void Evaluate_NullPlants_ThrowsArgumentNullException()
        {
            var validPlant = CreatePlant("laurel", "Laurel", "Laurus nobilis");

            Assert.That(() => CollaborativePlantGuessComparisonService.Evaluate(null, validPlant), Throws.ArgumentNullException);
            Assert.That(() => CollaborativePlantGuessComparisonService.Evaluate(validPlant, null), Throws.ArgumentNullException);
        }
    }
}
