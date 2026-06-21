using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyTeamNameFieldSceneSetup
{
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string SessionPanelPath = "Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel";
    private const string JoinCodeInputPath = "Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/JoinPanel/JoinCodeInput";
    private const string SessionPanelPrefabPath = "Assets/Prefabs/Lobby/SessionPanel.prefab";
    private const string JoinPanelPrefabPath = "Assets/Prefabs/Lobby/JoinPanel.prefab";

    [MenuItem("Tools/Coop/Ensure Lobby Team Name Field")]
    public static void EnsureFromMenu()
    {
        Debug.Log(Run());
    }

    public static void RunFromCommandLine()
    {
        Debug.Log(Run());
    }

    public static string Run()
    {
        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
        var sessionPanel = GameObject.Find(SessionPanelPath)?.transform;
        var joinCodeInput = GameObject.Find(JoinCodeInputPath)?.GetComponent<TMP_InputField>();

        if (controller == null || sessionPanel == null || joinCodeInput == null)
        {
            return "LobbyTeamNameFieldSceneSetup could not find controller, SessionPanel or JoinCodeInput.";
        }

        var sceneInput = EnsureField(sessionPanel, joinCodeInput);
        AssignControllerReference(controller, sceneInput);
        EnsureSessionPanelPrefab();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "TeamNameInput ensured in Lobby scene and SessionPanel prefab.";
    }

    private static TMP_InputField EnsureField(Transform sessionPanel, TMP_InputField inputTemplate)
    {
        var existingInput = sessionPanel.Find("TeamNameInput")?.GetComponent<TMP_InputField>();
        if (existingInput != null)
        {
            ConfigureInput(existingInput);
            return existingInput;
        }

        var joinCodeLabel = sessionPanel.Find("JoinCodeLabel");
        var insertIndex = joinCodeLabel == null ? sessionPanel.childCount : joinCodeLabel.GetSiblingIndex();

        var label = sessionPanel.Find("TeamNameLabel")?.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = CreateLabel(sessionPanel);
            label.transform.SetSiblingIndex(insertIndex);
            insertIndex += 1;
        }

        var inputObject = Object.Instantiate(inputTemplate.gameObject, sessionPanel, false);
        inputObject.name = "TeamNameInput";
        inputObject.transform.SetSiblingIndex(insertIndex);

        var input = inputObject.GetComponent<TMP_InputField>();
        ConfigureInput(input);

        var layoutElement = inputObject.GetComponent<LayoutElement>() ?? inputObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 60f;
        return input;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent)
    {
        var labelObject = new GameObject("TeamNameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Nombre del equipo";
        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 22f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = new Color(0.96f, 0.88f, 0.74f, 1f);

        var layoutElement = labelObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 36f;
        return label;
    }

    private static void ConfigureInput(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        input.characterLimit = 24;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.SetTextWithoutNotify("Equipo Micorriza");

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "Nombre del equipo";
        }

        if (input.textComponent != null)
        {
            input.textComponent.text = "Equipo Micorriza";
        }
    }

    private static void AssignControllerReference(MultiplayerMenuController controller, TMP_InputField teamNameInput)
    {
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("teamNameInput").objectReferenceValue = teamNameInput;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureSessionPanelPrefab()
    {
        var sessionPrefab = PrefabUtility.LoadPrefabContents(SessionPanelPrefabPath);
        var joinPrefab = PrefabUtility.LoadPrefabContents(JoinPanelPrefabPath);
        try
        {
            var joinCodeInput = joinPrefab.transform.Find("JoinCodeInput")?.GetComponent<TMP_InputField>();
            if (joinCodeInput == null)
            {
                return;
            }

            EnsureField(sessionPrefab.transform, joinCodeInput);
            PrefabUtility.SaveAsPrefabAsset(sessionPrefab, SessionPanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(joinPrefab);
            PrefabUtility.UnloadPrefabContents(sessionPrefab);
        }
    }
}
