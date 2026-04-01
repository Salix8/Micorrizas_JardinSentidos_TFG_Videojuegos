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

        [Test]
        public void Evaluate_NullPlants_ThrowsArgumentNullException()
        {
            var validPlant = new CollaborativePlantGuessPlantDefinition("laurel", "Laurel", new string[0], string.Empty, "Perenne", "Mediana", 2, "Lisa", 1, "Drupa", "Carnoso");

            Assert.That(() => CollaborativePlantGuessComparisonService.Evaluate(null, validPlant), Throws.ArgumentNullException);
            Assert.That(() => CollaborativePlantGuessComparisonService.Evaluate(validPlant, null), Throws.ArgumentNullException);
        }

        [Test]
        public void CreateResult_UnsolvedRun_ReturnsZeroScore()
        {
            var result = CollaborativePlantGuessScoreService.CreateResult(
                config: null,
                wasSolved: false,
                attemptsUsed: 8,
                maxAttempts: 8,
                resultMessage: "No encontrada");

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(0f));
            Assert.That(result.FailedActions, Is.EqualTo(8));
        }

        [Test]
        public void CreateResult_LateSolveScoresLessThanEarlySolve()
        {
            var config = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();

            var earlyResult = CollaborativePlantGuessScoreService.CreateResult(
                config,
                wasSolved: true,
                attemptsUsed: 2,
                maxAttempts: 8,
                resultMessage: "Resuelta");

            var lateResult = CollaborativePlantGuessScoreService.CreateResult(
                config,
                wasSolved: true,
                attemptsUsed: 7,
                maxAttempts: 8,
                resultMessage: "Resuelta");

            Assert.That(lateResult.ScoreOutOfTen, Is.LessThan(earlyResult.ScoreOutOfTen));
        }
    }
}
