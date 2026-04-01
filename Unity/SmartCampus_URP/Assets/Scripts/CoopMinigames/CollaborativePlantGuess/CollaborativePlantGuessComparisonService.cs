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
            CollaborativePlantGuessComparisonOutcome leafPersistenceOutcome,
            CollaborativePlantGuessComparisonOutcome leafSizeOutcome,
            CollaborativePlantGuessComparisonOutcome leafTextureOutcome,
            CollaborativePlantGuessComparisonOutcome fruitTypeOutcome)
        {
            IsExactPlantMatch = isExactPlantMatch;
            LeafPersistenceOutcome = leafPersistenceOutcome;
            LeafSizeOutcome = leafSizeOutcome;
            LeafTextureOutcome = leafTextureOutcome;
            FruitTypeOutcome = fruitTypeOutcome;
        }

        public bool IsExactPlantMatch { get; }
        public CollaborativePlantGuessComparisonOutcome LeafPersistenceOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome LeafSizeOutcome { get; }
        public CollaborativePlantGuessComparisonOutcome LeafTextureOutcome { get; }
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
                EvaluateExactText(targetPlant.LeafPersistence, guessedPlant.LeafPersistence),
                EvaluateOrderedValue(targetPlant.LeafSizeOrder, guessedPlant.LeafSizeOrder, targetPlant.LeafSize, guessedPlant.LeafSize),
                EvaluateOrderedValue(targetPlant.LeafTextureOrder, guessedPlant.LeafTextureOrder, targetPlant.LeafTexture, guessedPlant.LeafTexture),
                EvaluateFruitType(targetPlant, guessedPlant));
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

        private static CollaborativePlantGuessComparisonOutcome EvaluateFruitType(
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

            var targetCategory = CollaborativePlantGuessAutocompleteService.Normalize(targetPlant.FruitCategory);
            var guessedCategory = CollaborativePlantGuessAutocompleteService.Normalize(guessedPlant.FruitCategory);
            return !string.IsNullOrWhiteSpace(targetCategory) && targetCategory == guessedCategory
                ? CollaborativePlantGuessComparisonOutcome.Close
                : CollaborativePlantGuessComparisonOutcome.Incorrect;
        }
    }
}
