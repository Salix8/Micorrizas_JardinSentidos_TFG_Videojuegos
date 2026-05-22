namespace SmartCampus.Coop.Minigames
{
    public static class CoopSessionFlowService
    {
        public static CoopGamePhase ResolvePostMinigamePhase(bool areAllMinigamesCompleted)
        {
            return areAllMinigamesCompleted ? CoopGamePhase.SessionSummary : CoopGamePhase.WorldMap;
        }

        public static bool CanLaunchConfiguredMinigame(bool isConfigured, bool isAlreadyCompleted)
        {
            return isConfigured && !isAlreadyCompleted;
        }
    }
}
