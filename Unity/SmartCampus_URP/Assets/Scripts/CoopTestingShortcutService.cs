using SmartCampus.Coop.Minigames;
using UnityEngine;

public static class CoopTestingShortcutService
{
    public const float DefaultForcedScore = 5f;
    public const string DefaultForcedWinMessage = "Victoria rapida de prueba";

    public static bool AreShortcutsEnabled(
        bool enableInEditor,
        bool enableInDevelopmentBuild,
        bool isEditor,
        bool isDebugBuild)
    {
        return (isEditor && enableInEditor) || (!isEditor && isDebugBuild && enableInDevelopmentBuild);
    }

    public static MinigameResultData CreateForcedWinResult(
        float scoreOutOfTen = DefaultForcedScore,
        string message = DefaultForcedWinMessage)
    {
        var safeMessage = string.IsNullOrWhiteSpace(message) ? DefaultForcedWinMessage : message;
        return new MinigameResultData(safeMessage, Mathf.Clamp(scoreOutOfTen, 0f, 10f), 1, 0);
    }
}
