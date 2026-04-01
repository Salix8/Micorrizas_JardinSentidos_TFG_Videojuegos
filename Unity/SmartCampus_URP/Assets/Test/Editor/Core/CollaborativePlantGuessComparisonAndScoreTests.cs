using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessComparisonAndScoreTests
    {
        [Test]
        public void Evaluate_CloseLeafSizeAndFruitCategory_ReturnsCloseOutcomes()
        {
            var targetPlant = new CollaborativePlantGuessPlantDefinition("laurel", "Laurel", new string[0], string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Drupa", "Carnoso");
            var guessedPlant = new CollaborativePlantGuessPlantDefinition("madrono", "Madroño", new string[0], string.Empty, "Perenne", "Grande", 3, "Lisa", 1, "Baya", "Carnoso");

            var evaluation = CollaborativePlantGuessComparisonService.Evaluate(targetPlant, guessedPlant);

            Assert.That(evaluation.IsExactPlantMatch, Is.False);
            Assert.That(evaluation.LeafPersistenceOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Exact));
            Assert.That(evaluation.LeafSizeOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Close));
            Assert.That(evaluation.FruitTypeOutcome, Is.EqualTo(CollaborativePlantGuessComparisonOutcome.Close));
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
    }
}
