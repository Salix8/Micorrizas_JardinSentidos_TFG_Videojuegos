using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    public static class GardenSmellTaxonomyScoreService
    {
        public static MinigameResultData CreateResult(
            GardenSmellTaxonomyMinigameConfig config,
            int correctAnswers,
            int incorrectAnswers,
            int totalPlants,
            bool completedAllPlants)
        {
            var scoreSettings = config == null ? GardenSmellTaxonomyScoreSettings.CreateDefault() : config.ScoreSettings;
            var validTotalPlants = Mathf.Max(1, totalPlants);
            var answeredPlants = Mathf.Max(0, correctAnswers + incorrectAnswers);

            var completionRatio = Mathf.Clamp01(answeredPlants / (float)validTotalPlants);
            var accuracyRatio = answeredPlants <= 0
                ? 0f
                : Mathf.Clamp01(correctAnswers / (float)answeredPlants);

            var weightTotal = Mathf.Max(0.0001f, scoreSettings.CompletionWeight + scoreSettings.AccuracyWeight);
            var weightedScore = ((completionRatio * scoreSettings.CompletionWeight) + (accuracyRatio * scoreSettings.AccuracyWeight)) / weightTotal;
            var score = Mathf.Clamp(weightedScore * scoreSettings.MaxScore, scoreSettings.MinimumScore, scoreSettings.MaxScore);

            var decimalFactor = Mathf.Pow(10f, scoreSettings.DecimalPlaces);
            score = Mathf.Round(score * decimalFactor) / decimalFactor;

            var message = completedAllPlants || config == null
                ? (config == null ? "Taxonomia completada" : config.SuccessMessage)
                : config.TimeoutMessage;

            return new MinigameResultData(message, score, correctAnswers, incorrectAnswers);
        }
    }
}
