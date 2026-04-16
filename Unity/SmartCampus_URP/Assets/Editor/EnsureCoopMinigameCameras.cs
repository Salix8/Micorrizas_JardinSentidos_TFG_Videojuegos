using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class EnsureCoopMinigameCameras
{
    private static readonly string ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "minigame-camera-report.txt");

    private static readonly (string Path, Color BackgroundColor)[] MinigameScenes =
    {
        ("Assets/Scenes/GardenImageVotingMinigame.unity", new Color(0.92f, 0.96f, 0.9f, 1f)),
        ("Assets/Scenes/AudioWordConsensusMinigame.unity", new Color(0.86f, 0.91f, 0.94f, 1f)),
        ("Assets/Scenes/CollaborativePlantGuessMinigame.unity", new Color(0.9f, 0.95f, 0.89f, 1f)),
        ("Assets/Scenes/DistributedPairsMinigame.unity", new Color(0.94f, 0.97f, 0.93f, 1f))
    };

    [MenuItem("Tools/Coop/Ensure Minigame Cameras")]
    public static void Apply()
    {
        using var writer = new StreamWriter(ReportPath, false);

        foreach (var sceneDefinition in MinigameScenes)
        {
            var scene = EditorSceneManager.OpenScene(sceneDefinition.Path, OpenSceneMode.Single);
            var camera = MinigameSceneCameraUtility.EnsureFixedCamera(scene, sceneDefinition.BackgroundColor);
            var saved = EditorSceneManager.SaveScene(scene);
            writer.WriteLine($"{sceneDefinition.Path} | camera={camera != null} | saved={saved} | position={camera.transform.position} | ortho={camera.orthographic}");
        }
    }
}
