using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    public static class DistributedPairsScoreService
    {
        public static MinigameResultData CreateResult(DistributedPairsMinigameConfig config, int matchedPairs, int failedAttempts)
        {
            var totalPairs = Mathf.Max(1, config == null ? 1 : config.ActivePairCount);
            var scoreSettings = config == null ? DistributedPairsScoreSettings.CreateDefault() : config.ScoreSettings;

            var completionRatio = Mathf.Clamp01(matchedPairs / (float)totalPairs);
            var efficiencyRatio = matchedPairs + failedAttempts <= 0
                ? 1f
                : Mathf.Clamp01(matchedPairs / (float)(matchedPairs + failedAttempts));

            var weightTotal = Mathf.Max(0.0001f, scoreSettings.CompletionWeight + scoreSettings.EfficiencyWeight);
            var weightedScore = ((completionRatio * scoreSettings.CompletionWeight) + (efficiencyRatio * scoreSettings.EfficiencyWeight)) / weightTotal;
            var score = Mathf.Clamp(weightedScore * scoreSettings.MaxScore, scoreSettings.MinimumScore, scoreSettings.MaxScore);

            var decimalFactor = Mathf.Pow(10f, scoreSettings.DecimalPlaces);
            score = Mathf.Round(score * decimalFactor) / decimalFactor;

            var message = config == null ? "Lo habeis conseguido" : config.SuccessMessage;
            return new MinigameResultData(message, score, matchedPairs, failedAttempts);
        }
    }
}
