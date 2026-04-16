using UnityEditor;

public static class CoopResponsiveLayoutRefactor
{
    [MenuItem("Tools/Coop/Apply Responsive Layout Refactor")]
    public static void ApplyAll()
    {
        ApplyAndroidOrientationDefaults();

        LobbySceneUiSetup.SetupLobbyUi();
        GardenImageVotingMinigameSetup.SetupGardenImageVotingMinigame();
        AudioWordConsensusMinigameSetup.SetupAudioWordConsensusMinigame();
        CollaborativePlantGuessMinigameSetup.SetupCollaborativePlantGuessMinigame();
        CollaborativePlantGuessMinigameSetup.RepairCollaborativePlantGuessInput();
        DistributedPairsMinigameSetup.SetupDistributedPairsMinigame();
        GardenSmellTaxonomyMinigameSetup.SetupGardenSmellTaxonomyMinigame();
        CoopMinigameSetupEditorUtility.RefreshMainMapMinigameLauncher();
        ResponsiveLayoutAudit.GenerateReport();
        MobileResponsiveValidation.RunValidation();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ApplyAndroidOrientationDefaults()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }
}
