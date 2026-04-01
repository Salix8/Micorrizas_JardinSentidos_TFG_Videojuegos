using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public static class CollaborativePlantGuessScoreService
    {
        public static MinigameResultData CreateResult(
            CollaborativePlantGuessMinigameConfig config,
            bool wasSolved,
            int attemptsUsed,
            int maxAttempts,
            string resultMessage)
        {
            var scoreSettings = config == null ? CollaborativePlantGuessScoreSettings.CreateDefault() : config.ScoreSettings;
            var validMaxAttempts = Mathf.Max(1, maxAttempts);
            var score = 0f;

            if (wasSolved)
            {
                var attemptRatio = Mathf.Clamp01((validMaxAttempts - Mathf.Max(0, attemptsUsed - 1)) / (float)validMaxAttempts);
                var minimumScore = scoreSettings.MaxScore * scoreSettings.MinimumSolvedScoreRatio;
                score = Mathf.Lerp(minimumScore, scoreSettings.MaxScore, attemptRatio);
            }

            var decimalFactor = Mathf.Pow(10f, scoreSettings.DecimalPlaces);
            score = Mathf.Round(score * decimalFactor) / decimalFactor;

            return new MinigameResultData(resultMessage, score, wasSolved ? 1 : 0, wasSolved ? Mathf.Max(0, attemptsUsed - 1) : attemptsUsed);
        }
    }
}
