using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    public static class AudioWordConsensusScoreService
    {
        public static MinigameResultData CreateResult(
            AudioWordConsensusMinigameConfig config,
            int correctRounds,
            int incorrectRounds,
            int totalScheduledRounds,
            bool completedAllRounds)
        {
            var scoreSettings = config == null ? AudioWordConsensusScoreSettings.CreateDefault() : config.ScoreSettings;
            var validRoundCount = Mathf.Max(1, totalScheduledRounds);
            var scoreRatio = Mathf.Clamp01(correctRounds / (float)validRoundCount);
            var score = Mathf.Clamp(scoreRatio * scoreSettings.MaxScore, scoreSettings.MinimumScore, scoreSettings.MaxScore);

            var decimalFactor = Mathf.Pow(10f, scoreSettings.DecimalPlaces);
            score = Mathf.Round(score * decimalFactor) / decimalFactor;

            var message = completedAllRounds || config == null
                ? (config == null ? "Minijuego completado" : config.SuccessMessage)
                : config.TimeoutMessage;

            return new MinigameResultData(message, score, correctRounds, incorrectRounds);
        }
    }
}
