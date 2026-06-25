using System;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public enum CollaborativePlantGuessComparisonOutcome
    {
        Incorrect = 0,
        Close = 1,
        Exact = 2
    }

    public readonly struct CollaborativePlantGuessEvaluation
    {
        public CollaborativePlantGuessEvaluation(
            bool isExactPlantMatch,
            CollaborativePlantGuessComparisonOutcome plantTypeOutcome,
            CollaborativePlantGuessComparisonOutcome surfaceRoughnessOutcome,
            CollaborativePlantGuessComparisonOutcome leafTypeOutcome,
            CollaborativePlantGuessComparisonOutcome fruitCategoryOutcome,
            CollaborativePlantGuessComparisonOutcome fruitTypeOutcome)
        {
            IsExactPlantMatch = isExactPlantMatch;
            PlantTypeOutcome = plantTypeOutcome;
            SurfaceRoughnessOutcome = surfaceRoughnessOutcome;
            LeafTypeOutcome = leafTypeOutcome;
            FruitCategoryOutcome = fruitCategoryOutcome;
            FruitTypeOutcome = fruitTypeOutcome;
        }

        public bool IsExactPlantMatch { get; }
        public CollaborativePlantGuessComparisonOutcome PlantTypeOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome SurfaceRoughnessOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome LeafTypeOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome FruitCategoryOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome FruitTypeOutcome { get; }
    }

    public static class CollaborativePlantGuessComparisonService
    {
        public static CollaborativePlantGuessEvaluation Evaluate(
            CollaborativePlantGuessPlantDefinition targetPlant,
            CollaborativePlantGuessPlantDefinition guessedPlant)
        {
            if (targetPlant == null)
            {
                throw new ArgumentNullException(nameof(targetPlant));
            }

            if (guessedPlant == null)
            {
                throw new ArgumentNullException(nameof(guessedPlant));
            }

            var isExactPlantMatch = string.Equals(targetPlant.PlantId, guessedPlant.PlantId, StringComparison.OrdinalIgnoreCase);
            return new CollaborativePlantGuessEvaluation(
                isExactPlantMatch,
                EvaluateExactText(targetPlant.PlantType, guessedPlant.PlantType),
                EvaluateExactText(targetPlant.SurfaceRoughness, guessedPlant.SurfaceRoughness),
                EvaluateExactText(targetPlant.LeafType, guessedPlant.LeafType),
                EvaluateExactText(targetPlant.FruitCategory, guessedPlant.FruitCategory),
                EvaluateExactText(targetPlant.FruitType, guessedPlant.FruitType));
        }

        private static CollaborativePlantGuessComparisonOutcome EvaluateExactText(string targetValue, string guessedValue)
        {
            return string.Equals(
                CollaborativePlantGuessAutocompleteService.Normalize(targetValue),
                CollaborativePlantGuessAutocompleteService.Normalize(guessedValue),
                StringComparison.Ordinal)
                ? CollaborativePlantGuessComparisonOutcome.Exact
                : CollaborativePlantGuessComparisonOutcome.Incorrect;
        }
    }
}
