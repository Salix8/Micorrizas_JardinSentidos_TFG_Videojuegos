using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    public enum AudioWordConsensusRoundPhase
    {
        Guessing = 0,
        AwaitingEmitterContinue = 1
    }

    public enum AudioWordConsensusSubmissionOutcomeType
    {
        Rejected = 0,
        IncorrectKeepGuessing = 1,
        IncorrectRoundFailed = 2,
        CorrectRoundSolved = 3
    }

    public readonly struct AudioWordConsensusSubmissionOutcome
    {
        public AudioWordConsensusSubmissionOutcome(
            AudioWordConsensusSubmissionOutcomeType outcomeType,
            int nextMistakeCount,
            int nextRevealStageIndex,
            AudioWordConsensusRoundPhase nextPhase)
        {
            OutcomeType = outcomeType;
            NextMistakeCount = nextMistakeCount;
            NextRevealStageIndex = nextRevealStageIndex;
            NextPhase = nextPhase;
        }

        public AudioWordConsensusSubmissionOutcomeType OutcomeType { get; }
        public int NextMistakeCount { get; }
        public int NextRevealStageIndex { get; }
        public AudioWordConsensusRoundPhase NextPhase { get; }
        public bool WasAccepted => OutcomeType != AudioWordConsensusSubmissionOutcomeType.Rejected;
        public bool WasCorrect => OutcomeType == AudioWordConsensusSubmissionOutcomeType.CorrectRoundSolved;
        public bool ShouldEnterClosure => NextPhase == AudioWordConsensusRoundPhase.AwaitingEmitterContinue;
        public bool ShouldCountAsSolvedRound => OutcomeType == AudioWordConsensusSubmissionOutcomeType.CorrectRoundSolved;
        public bool ShouldCountAsFailedRound => OutcomeType == AudioWordConsensusSubmissionOutcomeType.IncorrectRoundFailed;
    }

    public readonly struct AudioWordConsensusRoundScoreEntry
    {
        public AudioWordConsensusRoundScoreEntry(bool wasSolved, int mistakeCount)
        {
            WasSolved = wasSolved;
            MistakeCount = mistakeCount;
        }

        public bool WasSolved { get; }
        public int MistakeCount { get; }
    }

    public static class AudioWordConsensusGameplayRules
    {
        public static AudioWordConsensusSubmissionOutcome EvaluateSubmission(
            bool wasCorrect,
            int currentMistakeCount,
            int maxMistakesPerRound,
            int revealStageCount)
        {
            var validMaxMistakes = Mathf.Max(1, maxMistakesPerRound);
            var validRevealStageCount = Mathf.Max(1, revealStageCount);
            var finalRevealStageIndex = validRevealStageCount - 1;
            currentMistakeCount = Mathf.Clamp(currentMistakeCount, 0, validMaxMistakes);

            if (wasCorrect)
            {
                return new AudioWordConsensusSubmissionOutcome(
                    AudioWordConsensusSubmissionOutcomeType.CorrectRoundSolved,
                    currentMistakeCount,
                    finalRevealStageIndex,
                    AudioWordConsensusRoundPhase.AwaitingEmitterContinue);
            }

            var nextMistakeCount = Mathf.Clamp(currentMistakeCount + 1, 0, validMaxMistakes);
            if (nextMistakeCount >= validMaxMistakes)
            {
                return new AudioWordConsensusSubmissionOutcome(
                    AudioWordConsensusSubmissionOutcomeType.IncorrectRoundFailed,
                    nextMistakeCount,
                    finalRevealStageIndex,
                    AudioWordConsensusRoundPhase.AwaitingEmitterContinue);
            }

            return new AudioWordConsensusSubmissionOutcome(
                AudioWordConsensusSubmissionOutcomeType.IncorrectKeepGuessing,
                nextMistakeCount,
                Mathf.Min(nextMistakeCount, finalRevealStageIndex),
                AudioWordConsensusRoundPhase.Guessing);
        }

        public static bool CanAdvanceFromRoundClosure(
            CooperativeMinigameStage stage,
            AudioWordConsensusRoundPhase roundPhase,
            bool isLocalEmitter)
        {
            return stage == CooperativeMinigameStage.Playing &&
                   roundPhase == AudioWordConsensusRoundPhase.AwaitingEmitterContinue &&
                   isLocalEmitter;
        }
    }

    public static class AudioWordConsensusScoreService
    {
        public static float CalculateRoundScoreRatio(bool wasSolved, int mistakeCount, int maxMistakesPerRound)
        {
            if (!wasSolved)
            {
                return 0f;
            }

            var validMaxMistakes = Mathf.Max(1, maxMistakesPerRound);
            var clampedMistakeCount = Mathf.Clamp(mistakeCount, 0, validMaxMistakes);
            return Mathf.Clamp01((validMaxMistakes + 1f - clampedMistakeCount) / (validMaxMistakes + 1f));
        }

        public static float CalculateScoreRatio(
            System.Collections.Generic.IReadOnlyList<AudioWordConsensusRoundScoreEntry> roundResults,
            int totalScheduledRounds,
            int maxMistakesPerRound)
        {
            var validRoundCount = Mathf.Max(1, totalScheduledRounds);
            if (roundResults == null || roundResults.Count == 0)
            {
                return 0f;
            }

            var accumulatedRatio = 0f;
            for (var index = 0; index < roundResults.Count; index++)
            {
                var roundResult = roundResults[index];
                accumulatedRatio += CalculateRoundScoreRatio(roundResult.WasSolved, roundResult.MistakeCount, maxMistakesPerRound);
            }

            return Mathf.Clamp01(accumulatedRatio / validRoundCount);
        }

        public static MinigameResultData CreateResult(
            AudioWordConsensusMinigameConfig config,
            System.Collections.Generic.IReadOnlyList<AudioWordConsensusRoundScoreEntry> roundResults,
            int correctRounds,
            int incorrectRounds,
            int totalScheduledRounds,
            bool completedAllRounds)
        {
            var scoreSettings = config == null ? AudioWordConsensusScoreSettings.CreateDefault() : config.ScoreSettings;
            var scoreRatio = CalculateScoreRatio(
                roundResults,
                totalScheduledRounds,
                config == null ? AudioWordConsensusMinigameConfig.DefaultMaxMistakesPerRound : config.MaxMistakesPerRound);
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
