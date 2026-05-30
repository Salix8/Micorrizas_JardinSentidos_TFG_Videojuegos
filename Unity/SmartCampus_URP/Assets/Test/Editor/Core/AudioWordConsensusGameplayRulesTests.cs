using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.AudioWordConsensus;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusGameplayRulesTests
    {
        [Test]
        public void EvaluateSubmission_KeepsRoundOpen_WhenIncorrectAttemptIsBelowLimit()
        {
            var outcome = AudioWordConsensusGameplayRules.EvaluateSubmission(
                wasCorrect: false,
                currentMistakeCount: 1,
                maxMistakesPerRound: AudioWordConsensusMinigameConfig.DefaultMaxMistakesPerRound,
                revealStageCount: AudioWordConsensusMinigameConfig.DefaultRevealStageCount);

            Assert.That(outcome.OutcomeType, Is.EqualTo(AudioWordConsensusSubmissionOutcomeType.IncorrectKeepGuessing));
            Assert.That(outcome.NextMistakeCount, Is.EqualTo(2));
            Assert.That(outcome.NextRevealStageIndex, Is.EqualTo(2));
            Assert.That(outcome.NextPhase, Is.EqualTo(AudioWordConsensusRoundPhase.Guessing));
        }

        [Test]
        public void EvaluateSubmission_ClosesRound_WhenThirdMistakeIsReached()
        {
            var outcome = AudioWordConsensusGameplayRules.EvaluateSubmission(
                wasCorrect: false,
                currentMistakeCount: 2,
                maxMistakesPerRound: AudioWordConsensusMinigameConfig.DefaultMaxMistakesPerRound,
                revealStageCount: AudioWordConsensusMinigameConfig.DefaultRevealStageCount);

            Assert.That(outcome.OutcomeType, Is.EqualTo(AudioWordConsensusSubmissionOutcomeType.IncorrectRoundFailed));
            Assert.That(outcome.NextMistakeCount, Is.EqualTo(3));
            Assert.That(outcome.NextRevealStageIndex, Is.EqualTo(3));
            Assert.That(outcome.NextPhase, Is.EqualTo(AudioWordConsensusRoundPhase.AwaitingEmitterContinue));
        }

        [Test]
        public void EvaluateSubmission_ClosesRound_WhenAnswerIsCorrect()
        {
            var outcome = AudioWordConsensusGameplayRules.EvaluateSubmission(
                wasCorrect: true,
                currentMistakeCount: 1,
                maxMistakesPerRound: AudioWordConsensusMinigameConfig.DefaultMaxMistakesPerRound,
                revealStageCount: AudioWordConsensusMinigameConfig.DefaultRevealStageCount);

            Assert.That(outcome.OutcomeType, Is.EqualTo(AudioWordConsensusSubmissionOutcomeType.CorrectRoundSolved));
            Assert.That(outcome.NextMistakeCount, Is.EqualTo(1));
            Assert.That(outcome.NextRevealStageIndex, Is.EqualTo(3));
            Assert.That(outcome.NextPhase, Is.EqualTo(AudioWordConsensusRoundPhase.AwaitingEmitterContinue));
        }

        [Test]
        public void CanAdvanceFromRoundClosure_ReturnsTrue_OnlyForEmitterDuringClosure()
        {
            Assert.That(AudioWordConsensusGameplayRules.CanAdvanceFromRoundClosure(
                CooperativeMinigameStage.Playing,
                AudioWordConsensusRoundPhase.AwaitingEmitterContinue,
                isLocalEmitter: true), Is.True);

            Assert.That(AudioWordConsensusGameplayRules.CanAdvanceFromRoundClosure(
                CooperativeMinigameStage.Playing,
                AudioWordConsensusRoundPhase.AwaitingEmitterContinue,
                isLocalEmitter: false), Is.False);

            Assert.That(AudioWordConsensusGameplayRules.CanAdvanceFromRoundClosure(
                CooperativeMinigameStage.Playing,
                AudioWordConsensusRoundPhase.Guessing,
                isLocalEmitter: true), Is.False);
        }
    }
}
