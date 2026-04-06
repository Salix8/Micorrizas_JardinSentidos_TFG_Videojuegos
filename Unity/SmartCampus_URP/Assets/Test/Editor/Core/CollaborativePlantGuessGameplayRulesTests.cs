using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessGameplayRulesTests
    {
        [Test]
        public void GetLocalSubmissionBlockReason_WhenSameClientSubmittedLastGuess_ReturnsWaitingForAnotherPlayer()
        {
            var reason = CollaborativePlantGuessGameplayRules.GetLocalSubmissionBlockReason(
                CooperativeMinigameStage.Playing,
                hasLoadedPlantDefinitions: true,
                dataLoadError: string.Empty,
                attemptsUsed: 2,
                maxAttempts: 8,
                localClientId: 3,
                lastGuessingClientId: 3,
                canResolveGuess: true);

            Assert.That(reason, Is.EqualTo(CollaborativePlantGuessSubmissionBlockReason.WaitingForAnotherPlayer));
        }

        [Test]
        public void GetLocalSubmissionBlockReason_WhenOtherClientPlayedLast_AndGuessIsValid_ReturnsNone()
        {
            var reason = CollaborativePlantGuessGameplayRules.GetLocalSubmissionBlockReason(
                CooperativeMinigameStage.Playing,
                hasLoadedPlantDefinitions: true,
                dataLoadError: string.Empty,
                attemptsUsed: 2,
                maxAttempts: 8,
                localClientId: 3,
                lastGuessingClientId: 4,
                canResolveGuess: true);

            Assert.That(reason, Is.EqualTo(CollaborativePlantGuessSubmissionBlockReason.None));
        }
    }
}
