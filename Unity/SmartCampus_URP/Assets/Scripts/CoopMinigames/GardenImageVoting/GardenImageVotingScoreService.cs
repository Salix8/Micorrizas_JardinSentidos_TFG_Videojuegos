using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    public static class GardenImageVotingScoreService
    {
        public static MinigameResultData CreateResult(
            GardenImageVotingMinigameConfig config,
            int correctAnswers,
            int incorrectAnswers,
            int totalScheduledCards,
            bool completedAllCards)
        {
            var scoreSettings = config == null ? GardenImageVotingScoreSettings.CreateDefault() : config.ScoreSettings;
            var validTotalCards = Mathf.Max(1, totalScheduledCards);
            var scoreRatio = Mathf.Clamp01(correctAnswers / (float)validTotalCards);
            var score = Mathf.Clamp(scoreRatio * scoreSettings.MaxScore, scoreSettings.MinimumScore, scoreSettings.MaxScore);

            var decimalFactor = Mathf.Pow(10f, scoreSettings.DecimalPlaces);
            score = Mathf.Round(score * decimalFactor) / decimalFactor;

            var message = completedAllCards || config == null
                ? (config == null ? "Minijuego completado" : config.SuccessMessage)
                : config.TimeoutMessage;

            return new MinigameResultData(message, score, correctAnswers, incorrectAnswers);
        }
    }
}
