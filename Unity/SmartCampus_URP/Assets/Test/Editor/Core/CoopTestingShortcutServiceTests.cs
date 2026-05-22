using NUnit.Framework;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopTestingShortcutServiceTests
    {
        [Test]
        public void AreShortcutsEnabled_InEditor_RespectsEditorFlag()
        {
            Assert.That(
                CoopTestingShortcutService.AreShortcutsEnabled(
                    enableInEditor: true,
                    enableInDevelopmentBuild: false,
                    isEditor: true,
                    isDebugBuild: false),
                Is.True);

            Assert.That(
                CoopTestingShortcutService.AreShortcutsEnabled(
                    enableInEditor: false,
                    enableInDevelopmentBuild: true,
                    isEditor: true,
                    isDebugBuild: true),
                Is.False);
        }

        [Test]
        public void AreShortcutsEnabled_InDevelopmentBuild_RespectsDebugFlag()
        {
            Assert.That(
                CoopTestingShortcutService.AreShortcutsEnabled(
                    enableInEditor: false,
                    enableInDevelopmentBuild: true,
                    isEditor: false,
                    isDebugBuild: true),
                Is.True);

            Assert.That(
                CoopTestingShortcutService.AreShortcutsEnabled(
                    enableInEditor: false,
                    enableInDevelopmentBuild: true,
                    isEditor: false,
                    isDebugBuild: false),
                Is.False);
        }

        [Test]
        public void CreateForcedWinResult_UsesFivePointsByDefault()
        {
            var result = CoopTestingShortcutService.CreateForcedWinResult();

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(5f));
            Assert.That(result.Message, Is.EqualTo(CoopTestingShortcutService.DefaultForcedWinMessage));
            Assert.That(result.SuccessfulActions, Is.EqualTo(1));
            Assert.That(result.FailedActions, Is.EqualTo(0));
        }

        [Test]
        public void CreateForcedWinResult_ClampsTheForcedScore()
        {
            var result = CoopTestingShortcutService.CreateForcedWinResult(scoreOutOfTen: 25f, message: "Cheat");

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
            Assert.That(result.Message, Is.EqualTo("Cheat"));
        }
    }
}
