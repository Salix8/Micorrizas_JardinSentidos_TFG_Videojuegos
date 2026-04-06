using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenImageVoting;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenImageVotingExternalContentServiceTests
    {
        [Test]
        public void ResolveConfiguredPath_WhenImageIsRelativeToCsv_ReturnsAbsolutePathInsideCsvFolder()
        {
            var resolvedPath = GardenImageVotingExternalContentService.ResolveConfiguredPath(
                "Check-Image.jpg",
                "CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv");

            var normalizedPath = resolvedPath.Replace('\\', '/');

            Assert.That(normalizedPath, Does.EndWith("/StreamingAssets/CoopMinigames/01-GardenImagenVotingCards/Check-Image.jpg"));
        }

        [Test]
        public void ResolveConfiguredPath_WhenImageIsAlreadyStreamingAssetsRelative_KeepsWorking()
        {
            var resolvedPath = GardenImageVotingExternalContentService.ResolveConfiguredPath(
                "CoopMinigames/01-GardenImagenVotingCards/Cross-Image.jpg");

            var normalizedPath = resolvedPath.Replace('\\', '/');

            Assert.That(normalizedPath, Does.EndWith("/StreamingAssets/CoopMinigames/01-GardenImagenVotingCards/Cross-Image.jpg"));
        }
    }
}
