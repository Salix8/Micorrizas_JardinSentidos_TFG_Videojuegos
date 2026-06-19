using SmartCampus.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DialogueFlowSetup
{
    private const string ConfigFolder = "Assets/Dialogue/Config";
    private const string DialogueSystemConfigPath = ConfigFolder + "/DialogueSystemConfig.asset";
    private const string DialogueFlowConfigPath = ConfigFolder + "/DialogueFlowConfig.asset";
    private const string DialoguePanelPrefabPath = "Assets/Prefabs/Dialogue/DialoguePanel.prefab";
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string WorldMapScenePath = "Assets/Scenes/UJI.unity";
    private const string GardenBoundaryName = "DialogueGardenBoundary";

    [MenuItem("SmartCampus/Dialogue/Setup/Flow Integration")]
    public static void SetupFlowIntegration()
    {
        DialogueSystemSetup.EnsureDialogueAssetsAndPrefab();
        var config = EnsureDialogueFlowConfig();
        ConfigureLobbyScene(config);
        ConfigureWorldMapScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static DialogueFlowConfig EnsureDialogueFlowConfig()
    {
        EnsureFolder(ConfigFolder);
        var config = AssetDatabase.LoadAssetAtPath<DialogueFlowConfig>(DialogueFlowConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DialogueFlowConfig>();
            AssetDatabase.CreateAsset(config, DialogueFlowConfigPath);
        }

        var serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("dialoguesEnabled").boolValue = true;
        serializedConfig.FindProperty("dialogueSystemConfig").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(DialogueSystemConfigPath);
        serializedConfig.FindProperty("dialoguePanelPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePanelPrefabPath);
        serializedConfig.FindProperty("reclaimSequenceKey").stringValue = "Reclaim";
        serializedConfig.FindProperty("warningSequenceKey").stringValue = "Act I – Warning";
        serializedConfig.FindProperty("reconnectionSequenceKey").stringValue = "Act III – Reconnection";
        serializedConfig.FindProperty("gpsFixTimeoutSeconds").floatValue = 8f;
        serializedConfig.FindProperty("editorGpsFallbackSeconds").floatValue = 1f;
        serializedConfig.FindProperty("treatMissingGpsAsOutsideGarden").boolValue = true;
        serializedConfig.FindProperty("showOpeningLoadingOverlay").boolValue = true;
        serializedConfig.FindProperty("openingLoadingText").stringValue = "Cargando...";

        var entries = serializedConfig.FindProperty("minigameEntries");
        entries.arraySize = 5;
        ConfigureEntry(entries, 0, 0, "Garden of Sight", "Garden of Sight Succes");
        ConfigureEntry(entries, 1, 1, "Garden of Sound", "Garden of Sound Succes");
        ConfigureEntry(entries, 2, 2, "Garden of Touch", "Garden of Touch Succes");
        ConfigureEntry(entries, 3, 3, "Garden of Taste", "Garden of Taste Succes");
        ConfigureEntry(entries, 4, 4, "Garden of Smell", "Garden of Smell Succes");

        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return config;
    }

    private static void ConfigureEntry(
        SerializedProperty entries,
        int arrayIndex,
        int minigameIndex,
        string introductionKey,
        string successKey)
    {
        var entry = entries.GetArrayElementAtIndex(arrayIndex);
        entry.FindPropertyRelative("minigameIndex").intValue = minigameIndex;
        entry.FindPropertyRelative("introductionSequenceKey").stringValue = introductionKey;
        entry.FindPropertyRelative("successSequenceKey").stringValue = successKey;
    }

    private static void ConfigureLobbyScene(DialogueFlowConfig config)
    {
        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        var coopSession = GameObject.Find("CoopSession");
        if (coopSession == null)
        {
            Debug.LogError("CoopSession was not found in Lobby scene.");
            return;
        }

        var flowSync = coopSession.GetComponent<DialogueFlowSync>();
        if (flowSync == null)
        {
            flowSync = coopSession.AddComponent<DialogueFlowSync>();
        }

        var serializedFlow = new SerializedObject(flowSync);
        serializedFlow.FindProperty("flowConfig").objectReferenceValue = config;
        serializedFlow.FindProperty("sessionCoordinator").objectReferenceValue = coopSession.GetComponent<CoopSessionCoordinator>();
        serializedFlow.FindProperty("gpsStateSync").objectReferenceValue = coopSession.GetComponent<CoopGpsStateSync>();
        serializedFlow.FindProperty("worldMapSceneName").stringValue = "UJI";
        serializedFlow.FindProperty("persistAcrossScenes").boolValue = true;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        var coordinator = coopSession.GetComponent<CoopSessionCoordinator>();
        if (coordinator != null)
        {
            var serializedCoordinator = new SerializedObject(coordinator);
            serializedCoordinator.FindProperty("dialogueFlowSync").objectReferenceValue = flowSync;
            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(coopSession);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureWorldMapScene()
    {
        var scene = EditorSceneManager.OpenScene(WorldMapScenePath, OpenSceneMode.Single);
        var boundary = GameObject.Find(GardenBoundaryName);
        if (boundary == null)
        {
            boundary = new GameObject(GardenBoundaryName, typeof(BoxCollider), typeof(DialogueGardenBoundary));
        }
        else if (boundary.GetComponent<DialogueGardenBoundary>() == null)
        {
            boundary.AddComponent<DialogueGardenBoundary>();
        }

        var boxCollider = boundary.GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        ConfigureBoundaryFromMinigameZones(boxCollider);

        EditorUtility.SetDirty(boundary);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureBoundaryFromMinigameZones(BoxCollider boundaryCollider)
    {
        var zones = Object.FindObjectsByType<CoopMinigameZoneDefinition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (zones == null || zones.Length == 0)
        {
            boundaryCollider.center = Vector3.zero;
            boundaryCollider.size = new Vector3(250f, 80f, 250f);
            return;
        }

        var hasBounds = false;
        var bounds = new Bounds();
        for (var index = 0; index < zones.Length; index++)
        {
            var collider = zones[index] == null ? null : zones[index].ZoneCollider;
            if (collider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
        {
            boundaryCollider.center = Vector3.zero;
            boundaryCollider.size = new Vector3(250f, 80f, 250f);
            return;
        }

        boundaryCollider.transform.position = bounds.center;
        boundaryCollider.center = Vector3.zero;
        boundaryCollider.size = new Vector3(
            Mathf.Max(30f, bounds.size.x + 40f),
            Mathf.Max(40f, bounds.size.y + 40f),
            Mathf.Max(30f, bounds.size.z + 40f));
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        var parentFolder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(parentFolder))
        {
            EnsureFolder(parentFolder);
        }

        AssetDatabase.CreateFolder(parentFolder, System.IO.Path.GetFileName(assetPath));
    }
}
