using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public static class LobbySceneUiSetup
{
    private const string ScenePath = "Assets/Lobby.unity";
    private const string ReportPath = "C:/Users/saulp/Documents/UJI/Micorrizas_JardinSentidos_TFG_Videojuegos/lobby-ui-setup-report.txt";

    [MenuItem("Tools/Coop/Setup Lobby UI")]
    public static void SetupLobbyUi()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var controller = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);

        if (canvas == null || controller == null)
        {
            throw new System.InvalidOperationException("Lobby scene requires a Canvas and a MultiplayerMenuController.");
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            throw new System.InvalidOperationException("Could not load builtin Arial font.");
        }

        ConfigureRoot(canvas, controller);
        ClearExistingUi(controller.transform);

        var homePanel = CreatePanel("HomePanel", controller.transform, new Vector2(0.5f, 0.5f), new Vector2(640f, 360f), new Color(0.12f, 0.16f, 0.22f, 0.92f));
        var hostPanel = CreatePanel("HostPanel", controller.transform, new Vector2(0.5f, 0.5f), new Vector2(640f, 360f), new Color(0.12f, 0.16f, 0.22f, 0.92f));
        var joinPanel = CreatePanel("JoinPanel", controller.transform, new Vector2(0.5f, 0.5f), new Vector2(640f, 360f), new Color(0.12f, 0.16f, 0.22f, 0.92f));
        var sessionPanel = CreatePanel("SessionPanel", controller.transform, new Vector2(0.5f, 0.5f), new Vector2(760f, 460f), new Color(0.12f, 0.16f, 0.22f, 0.92f));

        BuildHomePanel(homePanel.transform, controller, font);
        var hostButton = BuildHostPanel(hostPanel.transform, controller, font);
        var joinData = BuildJoinPanel(joinPanel.transform, controller, font);
        var sessionData = BuildSessionPanel(sessionPanel.transform, controller, font);

        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        sessionPanel.SetActive(false);

        AssignControllerReferences(
            controller,
            homePanel,
            hostPanel,
            joinPanel,
            sessionPanel,
            joinData.inputField,
            sessionData.statusLabel,
            sessionData.joinCodeLabel,
            sessionData.playerCountLabel,
            sessionData.requirementsLabel,
            hostButton,
            joinData.joinButton,
            sessionData.startMatchButton,
            sessionData.leaveSessionButton,
            sessionData.copyJoinCodeButton);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        var saved = EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        File.WriteAllText(
            ReportPath,
            $"Scene: {scene.path}\n" +
            $"Controller children: {controller.transform.childCount}\n" +
            $"Saved: {saved}\n" +
            $"HomePanel: {homePanel.GetInstanceID()}\n" +
            $"JoinInput: {joinData.inputField.GetInstanceID()}\n" +
            $"StatusLabel: {sessionData.statusLabel.GetInstanceID()}\n");
    }

    private static void ConfigureRoot(Canvas canvas, MultiplayerMenuController controller)
    {
        var canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        var controllerRect = controller.GetComponent<RectTransform>();
        controllerRect.anchorMin = Vector2.zero;
        controllerRect.anchorMax = Vector2.one;
        controllerRect.offsetMin = Vector2.zero;
        controllerRect.offsetMax = Vector2.zero;
        controllerRect.localScale = Vector3.one;
    }

    private static void ClearExistingUi(Transform root)
    {
        for (var index = root.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(root.GetChild(index).gameObject);
        }
    }

    private static void BuildHomePanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateText("Title", parent, font, "Co-op Lobby", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 50f));
        CreateText("Subtitle", parent, font, "Elige si quieres alojar la partida o unirte por codigo.", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(540f, 40f));

        var hostNavigationButton = CreateButton("OpenHostPanelButton", parent, font, "Host", new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(240f, 52f));
        var joinNavigationButton = CreateButton("OpenJoinPanelButton", parent, font, "Join", new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(240f, 52f));

        UnityEventTools.AddPersistentListener(hostNavigationButton.onClick, controller.ShowHostPanel);
        UnityEventTools.AddPersistentListener(joinNavigationButton.onClick, controller.ShowJoinPanel);
    }

    private static Button BuildHostPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateText("Title", parent, font, "Host Session", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(520f, 44f));
        CreateText("Info", parent, font, "Crea la sala Relay y comparte el codigo con el resto del grupo.", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(560f, 40f));

        var hostButton = CreateButton("HostSessionButton", parent, font, "Create Lobby", new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(240f, 52f));
        var backButton = CreateButton("BackButton", parent, font, "Back", new Vector2(0.5f, 0.5f), new Vector2(0f, -72f), new Vector2(240f, 44f));

        UnityEventTools.AddPersistentListener(hostButton.onClick, controller.HostSession);
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowHomePanel);
        return hostButton;
    }

    private static (InputField inputField, Button joinButton) BuildJoinPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateText("Title", parent, font, "Join Session", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(520f, 44f));
        CreateText("Info", parent, font, "Introduce el codigo que te haya compartido el host.", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(560f, 40f));

        var inputField = CreateInputField("JoinCodeInput", parent, font, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(300f, 48f));
        var joinButton = CreateButton("JoinSessionButton", parent, font, "Join Lobby", new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(240f, 52f));
        var backButton = CreateButton("BackButton", parent, font, "Back", new Vector2(0.5f, 0.5f), new Vector2(0f, -128f), new Vector2(240f, 44f));

        UnityEventTools.AddPersistentListener(joinButton.onClick, controller.JoinSession);
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowHomePanel);
        return (inputField, joinButton);
    }

    private static (Text statusLabel, Text joinCodeLabel, Text playerCountLabel, Text requirementsLabel, Button startMatchButton, Button leaveSessionButton, Button copyJoinCodeButton) BuildSessionPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateText("Title", parent, font, "Lobby Status", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(560f, 44f));
        var statusLabel = CreateText("StatusLabel", parent, font, "Esperando a que el host cree o abra la sala.", 18, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(640f, 88f));
        var joinCodeLabel = CreateText("JoinCodeLabel", parent, font, "Join code: -", 20, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(640f, 34f));
        var playerCountLabel = CreateText("PlayerCountLabel", parent, font, "Players: 0/6 (minimum 3)", 20, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(640f, 34f));
        var requirementsLabel = CreateText("SessionRequirementsLabel", parent, font, "Lobby rule: 3-6 players", 18, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -288f), new Vector2(640f, 30f));

        var copyJoinCodeButton = CreateButton("CopyJoinCodeButton", parent, font, "Copy Code", new Vector2(0.5f, 0f), new Vector2(-172f, 54f), new Vector2(180f, 46f));
        var startMatchButton = CreateButton("StartMatchButton", parent, font, "Go To Map", new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(180f, 46f));
        var leaveSessionButton = CreateButton("LeaveSessionButton", parent, font, "Leave", new Vector2(0.5f, 0f), new Vector2(172f, 54f), new Vector2(180f, 46f));

        UnityEventTools.AddPersistentListener(copyJoinCodeButton.onClick, controller.CopyJoinCode);
        UnityEventTools.AddPersistentListener(startMatchButton.onClick, controller.StartMatch);
        UnityEventTools.AddPersistentListener(leaveSessionButton.onClick, controller.LeaveSession);

        return (statusLabel, joinCodeLabel, playerCountLabel, requirementsLabel, startMatchButton, leaveSessionButton, copyJoinCodeButton);
    }

    private static void AssignControllerReferences(
        MultiplayerMenuController controller,
        GameObject homePanel,
        GameObject hostPanel,
        GameObject joinPanel,
        GameObject sessionPanel,
        InputField joinCodeInput,
        Text statusLabel,
        Text joinCodeLabel,
        Text playerCountLabel,
        Text requirementsLabel,
        Button hostButton,
        Button joinButton,
        Button startMatchButton,
        Button leaveSessionButton,
        Button copyJoinCodeButton)
    {
        var serializedObject = new SerializedObject(controller);
        serializedObject.FindProperty("homePanel").objectReferenceValue = homePanel;
        serializedObject.FindProperty("hostPanel").objectReferenceValue = hostPanel;
        serializedObject.FindProperty("joinPanel").objectReferenceValue = joinPanel;
        serializedObject.FindProperty("sessionPanel").objectReferenceValue = sessionPanel;
        serializedObject.FindProperty("joinCodeInput").objectReferenceValue = joinCodeInput;
        serializedObject.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedObject.FindProperty("joinCodeLabel").objectReferenceValue = joinCodeLabel;
        serializedObject.FindProperty("playerCountLabel").objectReferenceValue = playerCountLabel;
        serializedObject.FindProperty("sessionRequirementsLabel").objectReferenceValue = requirementsLabel;
        serializedObject.FindProperty("hostButton").objectReferenceValue = hostButton;
        serializedObject.FindProperty("joinButton").objectReferenceValue = joinButton;
        serializedObject.FindProperty("startMatchButton").objectReferenceValue = startMatchButton;
        serializedObject.FindProperty("leaveSessionButton").objectReferenceValue = leaveSessionButton;
        serializedObject.FindProperty("copyJoinCodeButton").objectReferenceValue = copyJoinCodeButton;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = 5;
        panel.transform.SetParent(parent, false);

        var rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        var image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = 5;
        buttonObject.transform.SetParent(parent, false);

        var rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.41f, 0.69f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.29f, 0.49f, 0.81f, 1f);
        colors.pressedColor = new Color(0.19f, 0.32f, 0.54f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText("Label", buttonObject.transform, font, label, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return button;
    }

    private static InputField CreateInputField(string name, Transform parent, Font font, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        var inputObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        inputObject.layer = 5;
        inputObject.transform.SetParent(parent, false);

        var rectTransform = inputObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        var image = inputObject.GetComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        var textComponent = CreateText("Text", inputObject.transform, font, string.Empty, 20, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 32f, size.y - 12f));
        textComponent.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        var placeholderComponent = CreateText("Placeholder", inputObject.transform, font, "Join code", 20, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 32f, size.y - 12f));
        placeholderComponent.color = new Color(0.45f, 0.45f, 0.45f, 0.75f);

        var inputField = inputObject.GetComponent<InputField>();
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderComponent;
        inputField.characterLimit = 12;
        inputField.lineType = InputField.LineType.SingleLine;

        return inputField;
    }
}
