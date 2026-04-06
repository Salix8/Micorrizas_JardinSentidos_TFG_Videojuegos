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
            CollaborativePlantGuessComparisonOutcome fruitOutcome)
        {
            IsExactPlantMatch = isExactPlantMatch;
            PlantTypeOutcome = plantTypeOutcome;
            SurfaceRoughnessOutcome = surfaceRoughnessOutcome;
            LeafTypeOutcome = leafTypeOutcome;
            FruitOutcome = fruitOutcome;
        }

        public bool IsExactPlantMatch { get; }
        public CollaborativePlantGuessComparisonOutcome PlantTypeOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome SurfaceRoughnessOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome LeafTypeOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome FruitOutcome { get; }
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
                EvaluateOrderedValue(targetPlant.SurfaceRoughnessOrder, guessedPlant.SurfaceRoughnessOrder, targetPlant.SurfaceRoughness, guessedPlant.SurfaceRoughness),
                EvaluateExactText(targetPlant.LeafType, guessedPlant.LeafType),
                EvaluateFruit(targetPlant, guessedPlant));
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

        private static CollaborativePlantGuessComparisonOutcome EvaluateOrderedValue(int targetOrder, int guessedOrder, string targetValue, string guessedValue)
        {
            if (string.Equals(
                    CollaborativePlantGuessAutocompleteService.Normalize(targetValue),
                    CollaborativePlantGuessAutocompleteService.Normalize(guessedValue),
                    StringComparison.Ordinal))
            {
                return CollaborativePlantGuessComparisonOutcome.Exact;
            }

            return Math.Abs(targetOrder - guessedOrder) == 1
                ? CollaborativePlantGuessComparisonOutcome.Close
                : CollaborativePlantGuessComparisonOutcome.Incorrect;
        }

        private static CollaborativePlantGuessComparisonOutcome EvaluateFruit(
            CollaborativePlantGuessPlantDefinition targetPlant,
            CollaborativePlantGuessPlantDefinition guessedPlant)
        {
            if (string.Equals(
                    CollaborativePlantGuessAutocompleteService.Normalize(targetPlant.FruitType),
                    CollaborativePlantGuessAutocompleteService.Normalize(guessedPlant.FruitType),
                    StringComparison.Ordinal))
            {
                return CollaborativePlantGuessComparisonOutcome.Exact;
            }

            return string.Equals(
                CollaborativePlantGuessAutocompleteService.Normalize(targetPlant.FruitCategory),
                CollaborativePlantGuessAutocompleteService.Normalize(guessedPlant.FruitCategory),
                StringComparison.Ordinal)
                ? CollaborativePlantGuessComparisonOutcome.Close
                : CollaborativePlantGuessComparisonOutcome.Incorrect;
        }
    }
}
