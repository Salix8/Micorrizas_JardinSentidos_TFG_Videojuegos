using System;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public static class PlantPhotoRelayScoreService
    {
        public static float ComputeRoundScore(bool exactCommonNameMatch, bool photographerMatchedPrompt, PlantPhotoRelayMinigameConfig config)
        {
            if (config == null || !exactCommonNameMatch)
            {
                return 0f;
            }

            return config.ScoreExactMatch + (photographerMatchedPrompt ? config.ScorePromptMatchBonus : 0f);
        }

        public static float ComputeFinalScore(int totalRounds, float accumulatedScore, PlantPhotoRelayMinigameConfig config)
        {
            if (config == null || totalRounds <= 0)
            {
                return 0f;
            }

            var maxPerRound = Math.Max(0.01f, config.ScoreExactMatch + config.ScorePromptMatchBonus);
            var maxTotal = totalRounds * maxPerRound;
            return (float)Math.Round((accumulatedScore / maxTotal) * 10f, 1);
        }
    }
}
