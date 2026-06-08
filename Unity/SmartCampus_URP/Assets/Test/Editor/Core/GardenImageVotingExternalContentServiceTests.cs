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

        [Test]
        public void ResolveConfiguredPath_WithAndroidStreamingAssetsBase_BuildsJarUriForCsv()
        {
            const string androidStreamingAssetsPath = "jar:file:///data/app/com.smartcampus/base.apk!/assets";

            var resolvedPath = GardenImageVotingExternalContentService.ResolveConfiguredPath(
                "CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv",
                null,
                androidStreamingAssetsPath);

            Assert.That(
                resolvedPath,
                Is.EqualTo("jar:file:///data/app/com.smartcampus/base.apk!/assets/CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv"));
        }

        [Test]
        public void ResolveConfiguredPath_WithAndroidStreamingAssetsBase_ResolvesSiblingImageFromCsvUri()
        {
            const string androidStreamingAssetsPath = "jar:file:///data/app/com.smartcampus/base.apk!/assets";

            var resolvedPath = GardenImageVotingExternalContentService.ResolveConfiguredPath(
                "Check-Image.jpg",
                "CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv",
                androidStreamingAssetsPath);

            Assert.That(
                resolvedPath,
                Is.EqualTo("jar:file:///data/app/com.smartcampus/base.apk!/assets/CoopMinigames/01-GardenImagenVotingCards/Check-Image.jpg"));
        }
    }
}
