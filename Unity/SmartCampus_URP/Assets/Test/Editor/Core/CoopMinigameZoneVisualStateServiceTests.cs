using NUnit.Framework;

public sealed class CoopMinigameZoneVisualStateServiceTests
{
    [Test]
    public void ShouldUseCompletedSprite_ReturnsTrue_WhenCompletedScoreMatchesMinimum()
    {
        var shouldUseCompletedSprite = CoopMinigameZoneVisualStateService.ShouldUseCompletedSprite(
            isCompleted: true,
            scoreOutOfTen: 5f,
            minimumScore: 5f);

        Assert.That(shouldUseCompletedSprite, Is.True);
    }

    [Test]
    public void ShouldUseCompletedSprite_ReturnsFalse_WhenCompletedScoreIsBelowMinimum()
    {
        var shouldUseCompletedSprite = CoopMinigameZoneVisualStateService.ShouldUseCompletedSprite(
            isCompleted: true,
            scoreOutOfTen: 4.99f,
            minimumScore: 5f);

        Assert.That(shouldUseCompletedSprite, Is.False);
    }

    [Test]
    public void ShouldUseCompletedSprite_ReturnsFalse_WhenMinigameIsNotCompleted()
    {
        var shouldUseCompletedSprite = CoopMinigameZoneVisualStateService.ShouldUseCompletedSprite(
            isCompleted: false,
            scoreOutOfTen: 10f,
            minimumScore: 5f);

        Assert.That(shouldUseCompletedSprite, Is.False);
    }
}
