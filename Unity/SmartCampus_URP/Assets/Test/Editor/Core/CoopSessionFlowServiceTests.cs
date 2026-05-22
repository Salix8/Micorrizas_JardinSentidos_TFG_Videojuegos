using NUnit.Framework;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopSessionFlowServiceTests
    {
        [Test]
        public void ResolvePostMinigamePhase_WithPendingMinigames_ReturnsWorldMap()
        {
            var nextPhase = CoopSessionFlowService.ResolvePostMinigamePhase(areAllMinigamesCompleted: false);

            Assert.That(nextPhase, Is.EqualTo(CoopGamePhase.WorldMap));
        }

        [Test]
        public void ResolvePostMinigamePhase_WhenAllMinigamesAreCompleted_ReturnsSessionSummary()
        {
            var nextPhase = CoopSessionFlowService.ResolvePostMinigamePhase(areAllMinigamesCompleted: true);

            Assert.That(nextPhase, Is.EqualTo(CoopGamePhase.SessionSummary));
        }

        [Test]
        public void CanLaunchConfiguredMinigame_RejectsCompletedSlots()
        {
            Assert.That(CoopSessionFlowService.CanLaunchConfiguredMinigame(isConfigured: true, isAlreadyCompleted: true), Is.False);
            Assert.That(CoopSessionFlowService.CanLaunchConfiguredMinigame(isConfigured: true, isAlreadyCompleted: false), Is.True);
        }
    }
}
