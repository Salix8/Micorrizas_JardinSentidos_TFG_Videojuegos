public static class CoopMinigameZoneVisualStateService
{
    public static bool ShouldUseCompletedSprite(bool isCompleted, float scoreOutOfTen, float minimumScore)
    {
        return isCompleted && scoreOutOfTen >= minimumScore;
    }
}
