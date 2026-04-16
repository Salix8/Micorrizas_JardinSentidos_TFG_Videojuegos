using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop;
using SmartCampus.Coop.Minigames;

public static class LobbySceneUiSetup
{
    private const string ScenePath = "Assets/Scenes/Lobby.unity";
    private static readonly string ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "lobby-ui-setup-report.txt");

    [MenuItem("Tools/Coop/Setup Lobby UI")]
    public static void SetupLobbyUi()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var controller = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
        var relayConnectionService = Object.FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);
        var coopSessionCoordinator = Object.FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);

        if (canvas == null || controller == null || relayConnectionService == null || coopSessionCoordinator == null)
        {
            throw new System.InvalidOperationException("Lobby scene requires Canvas, MultiplayerMenuController, RelayConnectionService and CoopSessionCoordinator references.");
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            throw new System.InvalidOperationException("Could not load builtin Arial font.");
        }

        AssignSessionDefaults(relayConnectionService, coopSessionCoordinator);
        ConfigureRoot(canvas, controller);
        ClearExistingUi(controller.transform);

        var safeAreaRoot = CreateUiObject("SafeAreaRoot", controller.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreatePanel("Background", safeAreaRoot.transform, new Color(0.08f, 0.11f, 0.16f, 1f));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var surfacePanel = CreatePanel("SurfacePanel", safeAreaRoot.transform, new Color(0.12f, 0.16f, 0.22f, 0.94f));
        var surfaceRect = surfacePanel.GetComponent<RectTransform>();
        surfaceRect.anchorMin = new Vector2(0.5f, 0.5f);
        surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
        surfaceRect.pivot = new Vector2(0.5f, 0.5f);
        surfaceRect.anchoredPosition = Vector2.zero;
        surfacePanel.AddComponent<ResponsivePanelLayoutController>().Configure(
            safeAreaRoot.GetComponent<RectTransform>(),
            0.86f,
            0.82f,
            new Vector2(320f, 500f),
            new Vector2(820f, 1480f),
            new Vector2(32f, 32f));

        var surfaceLayout = surfacePanel.AddComponent<VerticalLayoutGroup>();
        surfaceLayout.padding = new RectOffset(36, 36, 48, 32);
        surfaceLayout.spacing = 24f;
        surfaceLayout.childAlignment = TextAnchor.UpperCenter;
        surfaceLayout.childControlWidth = true;
        surfaceLayout.childControlHeight = true;
        surfaceLayout.childForceExpandWidth = true;
        surfaceLayout.childForceExpandHeight = false;

        var headerTopSpacer = CreateUiObject("HeaderTopSpacer", surfacePanel.transform, typeof(LayoutElement));
        var spacerLayout = headerTopSpacer.GetComponent<LayoutElement>();
        spacerLayout.minHeight = 24f;
        spacerLayout.flexibleHeight = 0.2f;

        var headerStack = CreateUiObject("HeaderStack", surfacePanel.transform, typeof(VerticalLayoutGroup));
        headerStack.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var headerLayout = headerStack.GetComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandHeight = false;
        var headerElement = headerStack.AddComponent<LayoutElement>();
        headerElement.minHeight = 112f;

        var appTitle = CreateText("AppTitle", headerStack.transform, font, "Co-op Lobby", 40, TextAnchor.MiddleCenter, Color.white);
        ConfigureAutoSizedText(appTitle, 22, 40);
        appTitle.gameObject.AddComponent<LayoutElement>().minHeight = 52f;

        var appSubtitle = CreateText("AppSubtitle", headerStack.transform, font, "Multijugador editable y responsive para Android.", 20, TextAnchor.MiddleCenter, new Color(0.86f, 0.9f, 0.95f, 1f));
        ConfigureAutoSizedText(appSubtitle, 14, 20);
        appSubtitle.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

        var scrollFrame = CreateUiObject("ScrollFrame", surfacePanel.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var scrollFrameLayout = scrollFrame.GetComponent<HorizontalLayoutGroup>();
        scrollFrameLayout.padding = new RectOffset(12, 12, 0, 0);
        scrollFrameLayout.childAlignment = TextAnchor.UpperCenter;
        scrollFrameLayout.spacing = 0f;
        scrollFrameLayout.childControlWidth = true;
        scrollFrameLayout.childControlHeight = true;
        scrollFrameLayout.childForceExpandWidth = false;
        scrollFrameLayout.childForceExpandHeight = true;
        var scrollFrameElement = scrollFrame.GetComponent<LayoutElement>();
        scrollFrameElement.flexibleHeight = 1f;
        scrollFrameElement.minHeight = 260f;

        var scrollView = CreateScrollView("PanelScrollView", scrollFrame.transform);
        var scrollLayout = scrollView.Root.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.flexibleWidth = 1f;
        scrollLayout.minHeight = 220f;
        scrollView.ContentLayout.spacing = 18f;

        var homePanel = CreateContentPanel("HomePanel", scrollView.ContentRoot.transform);
        var hostPanel = CreateContentPanel("HostPanel", scrollView.ContentRoot.transform);
        var joinPanel = CreateContentPanel("JoinPanel", scrollView.ContentRoot.transform);
        var sessionPanel = CreateContentPanel("SessionPanel", scrollView.ContentRoot.transform);

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
        EditorUtility.SetDirty(relayConnectionService);
        EditorUtility.SetDirty(coopSessionCoordinator);
        EditorUtility.SetDirty(canvas.gameObject);
        FantasyWoodenThemeUtility.ApplyThemeToOpenScene(scene);
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

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var controllerRect = controller.GetComponent<RectTransform>();
        controllerRect.anchorMin = Vector2.zero;
        controllerRect.anchorMax = Vector2.one;
        controllerRect.offsetMin = Vector2.zero;
        controllerRect.offsetMax = Vector2.zero;
        controllerRect.localScale = Vector3.one;
    }

    private static void AssignSessionDefaults(RelayConnectionService relayConnectionService, CoopSessionCoordinator coopSessionCoordinator)
    {
        var relaySerializedObject = new SerializedObject(relayConnectionService);
        relaySerializedObject.FindProperty("minPlayersToStart").intValue = CoopSessionRules.DefaultMinimumPlayers;
        relaySerializedObject.FindProperty("maxPlayers").intValue = CoopSessionRules.DefaultMaximumPlayers;
        relaySerializedObject.FindProperty("mainMapSceneName").stringValue = "UJI";
        relaySerializedObject.ApplyModifiedPropertiesWithoutUndo();

        var coordinatorSerializedObject = new SerializedObject(coopSessionCoordinator);
        coordinatorSerializedObject.FindProperty("minPlayersToStart").intValue = CoopSessionRules.DefaultMinimumPlayers;
        coordinatorSerializedObject.FindProperty("maxPlayers").intValue = CoopSessionRules.DefaultMaximumPlayers;
        coordinatorSerializedObject.FindProperty("lobbySceneName").stringValue = "Lobby";
        coordinatorSerializedObject.FindProperty("mainMapSceneName").stringValue = "UJI";
        coordinatorSerializedObject.ApplyModifiedPropertiesWithoutUndo();
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
        CreateSectionTitle("HomeTitle", parent, font, "Escoge modo de entrada");
        var homeInfoFrame = CreatePaddedContentFrame("HomeInfoFrame", parent, 32, 32);
        CreateBodyText("HomeInfo", homeInfoFrame.transform, font, "Aloja una sala si vas a iniciar la partida o unete con el codigo compartido por otra persona.");

        var hostNavigationButton = CreateActionButton("CreateSessionButton", parent, font, "Crear sala");
        var joinNavigationButton = CreateActionButton("OpenJoinPanelButton", parent, font, "Unirse con codigo");

        UnityEventTools.AddPersistentListener(hostNavigationButton.onClick, controller.HostSession);
        UnityEventTools.AddPersistentListener(joinNavigationButton.onClick, controller.ShowJoinPanel);
    }

    private static Button BuildHostPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateSectionTitle("HostTitle", parent, font, "Crear sesion");
        CreateBodyText("HostInfo", parent, font, "Genera la sala Relay, comparte el codigo y vuelve al mapa cuando todo el grupo este listo.");

        var hostButton = CreateActionButton("HostSessionButton", parent, font, "Crear lobby");
        var backButton = CreateSecondaryButton("BackButton", parent, font, "Volver");

        UnityEventTools.AddPersistentListener(hostButton.onClick, controller.HostSession);
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowHomePanel);
        return hostButton;
    }

    private static (InputField inputField, Button joinButton) BuildJoinPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateSectionTitle("JoinTitle", parent, font, "Unirse a sesion");
        CreateBodyText("JoinInfo", parent, font, "Introduce el codigo del host. El campo queda visible en jerarquia para que puedas retocar placeholder, margenes y estilo.");

        var inputField = CreateInputField("JoinCodeInput", parent, font, "Codigo de acceso");
        inputField.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

        var joinButton = CreateActionButton("JoinSessionButton", parent, font, "Entrar al lobby");
        var backButton = CreateSecondaryButton("BackButton", parent, font, "Volver");

        UnityEventTools.AddPersistentListener(joinButton.onClick, controller.JoinSession);
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowHomePanel);
        return (inputField, joinButton);
    }

    private static (Text statusLabel, Text joinCodeLabel, Text playerCountLabel, Text requirementsLabel, Button startMatchButton, Button leaveSessionButton, Button copyJoinCodeButton) BuildSessionPanel(Transform parent, MultiplayerMenuController controller, Font font)
    {
        CreateSectionTitle("SessionTitle", parent, font, "Estado del lobby");
        var statusLabel = CreateBodyText("StatusLabel", parent, font, "Esperando a que el host cree o abra la sala.");
        statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;

        var joinCodeLabel = CreateValueLabel("JoinCodeLabel", parent, font, "Join code: -");
        var playerCountLabel = CreateValueLabel("PlayerCountLabel", parent, font, "Players: 0/6 (minimum 2)");
        var requirementsLabel = CreateValueLabel("SessionRequirementsLabel", parent, font, "Lobby rule: 2-6 players");

        var actionsRoot = CreateUiObject("ActionsRoot", parent, typeof(VerticalLayoutGroup));
        var actionsLayout = actionsRoot.GetComponent<VerticalLayoutGroup>();
        actionsLayout.spacing = 10f;
        actionsLayout.childControlWidth = true;
        actionsLayout.childControlHeight = true;
        actionsLayout.childForceExpandHeight = false;

        var copyJoinCodeButton = CreateActionButton("CopyJoinCodeButton", actionsRoot.transform, font, "Copiar codigo");
        var startMatchButton = CreateActionButton("StartMatchButton", actionsRoot.transform, font, "Ir al mapa");
        var leaveSessionButton = CreateSecondaryButton("LeaveSessionButton", actionsRoot.transform, font, "Salir de la sesion");

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

    private static GameObject CreateContentPanel(string name, Transform parent)
    {
        var panel = CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.06f));
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panel.AddComponent<LayoutElement>().preferredHeight = -1f;
        return panel;
    }

    private static GameObject CreateCenteredWidthFrame(string name, Transform parent, float preferredWidth, float maxWidth)
    {
        var frame = CreateUiObject(name, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var frameLayout = frame.GetComponent<HorizontalLayoutGroup>();
        frameLayout.padding = new RectOffset(0, 0, 0, 0);
        frameLayout.spacing = 0f;
        frameLayout.childAlignment = TextAnchor.MiddleCenter;
        frameLayout.childControlWidth = false;
        frameLayout.childControlHeight = true;
        frameLayout.childForceExpandWidth = false;
        frameLayout.childForceExpandHeight = false;

        var layoutElement = frame.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 88f;

        var constrainedWidth = CreateUiObject("ConstrainedWidth", frame.transform, typeof(LayoutElement));
        var constrainedLayout = constrainedWidth.GetComponent<LayoutElement>();
        constrainedLayout.preferredWidth = preferredWidth;
        constrainedLayout.minWidth = preferredWidth;
        constrainedLayout.flexibleWidth = 1f;
        constrainedLayout.layoutPriority = 1;
        constrainedWidth.GetComponent<RectTransform>().sizeDelta = new Vector2(maxWidth, 0f);

        return constrainedWidth;
    }

    private static GameObject CreatePaddedContentFrame(string name, Transform parent, int leftPadding, int rightPadding)
    {
        var frame = CreateUiObject(name, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var frameLayout = frame.GetComponent<HorizontalLayoutGroup>();
        frameLayout.padding = new RectOffset(leftPadding, rightPadding, 0, 0);
        frameLayout.spacing = 0f;
        frameLayout.childAlignment = TextAnchor.MiddleCenter;
        frameLayout.childControlWidth = true;
        frameLayout.childControlHeight = true;
        frameLayout.childForceExpandWidth = true;
        frameLayout.childForceExpandHeight = false;

        var layoutElement = frame.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 88f;

        var contentRoot = CreateUiObject("PaddedContent", frame.transform, typeof(LayoutElement));
        var contentLayout = contentRoot.GetComponent<LayoutElement>();
        contentLayout.flexibleWidth = 1f;
        contentLayout.layoutPriority = 1;

        return contentRoot;
    }

    private static Text CreateSectionTitle(string name, Transform parent, Font font, string value)
    {
        var title = CreateText(name, parent, font, value, 28, TextAnchor.MiddleCenter, Color.white);
        ConfigureAutoSizedText(title, 18, 28);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
        return title;
    }

    private static Text CreateBodyText(string name, Transform parent, Font font, string value)
    {
        var text = CreateText(name, parent, font, value, 20, TextAnchor.UpperLeft, new Color(0.9f, 0.93f, 0.97f, 1f));
        ConfigureAutoSizedText(text, 14, 20);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;
        return text;
    }

    private static Text CreateValueLabel(string name, Transform parent, Font font, string value)
    {
        var text = CreateText(name, parent, font, value, 20, TextAnchor.MiddleLeft, Color.white);
        ConfigureAutoSizedText(text, 14, 20);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        return text;
    }

    private static Button CreateActionButton(string name, Transform parent, Font font, string label)
    {
        var buttonFrame = CreateCenteredWidthFrame($"{name}Frame", parent, 320f, 400f);
        var button = CreateButton(name, buttonFrame.transform, font, label, new Color(0.24f, 0.41f, 0.69f, 1f));
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        return button;
    }

    private static Button CreateSecondaryButton(string name, Transform parent, Font font, string label)
    {
        var buttonFrame = CreateCenteredWidthFrame($"{name}Frame", parent, 320f, 400f);
        var button = CreateButton(name, buttonFrame.transform, font, label, new Color(0.22f, 0.29f, 0.36f, 1f));
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        return button;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, Color backgroundColor)
    {
        var buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = image.color * 1.08f;
        colors.pressedColor = image.color * 0.9f;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var labelText = CreateText("Label", buttonObject.transform, font, label, 20, TextAnchor.MiddleCenter, Color.white);
        ConfigureAutoSizedText(labelText, 14, 20);
        Stretch(labelText.rectTransform, new Vector2(16f, 10f), new Vector2(-16f, -10f));
        return button;
    }

    private static InputField CreateInputField(string name, Transform parent, Font font, string placeholderValue)
    {
        var inputObject = CreateUiObject(name, parent, typeof(Image), typeof(InputField));
        var image = inputObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.96f, 0.98f, 1f);

        var textComponent = CreateText("Text", inputObject.transform, font, string.Empty, 20, TextAnchor.MiddleLeft, new Color(0.12f, 0.12f, 0.14f, 1f));
        var placeholderComponent = CreateText("Placeholder", inputObject.transform, font, placeholderValue, 20, TextAnchor.MiddleLeft, new Color(0.35f, 0.39f, 0.42f, 0.7f));
        ConfigureAutoSizedText(textComponent, 14, 20);
        ConfigureAutoSizedText(placeholderComponent, 14, 20);
        Stretch(textComponent.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        Stretch(placeholderComponent.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));

        var inputField = inputObject.GetComponent<InputField>();
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderComponent;
        inputField.characterLimit = 12;
        inputField.lineType = InputField.LineType.SingleLine;
        return inputField;
    }

    private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Color color)
    {
        var textObject = CreateUiObject(name, parent, typeof(Text));
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void ConfigureAutoSizedText(Text text, int minSize, int maxSize)
    {
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = CreateUiObject(name, parent, typeof(Image));
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        var objectComponents = new System.Type[components.Length + 1];
        objectComponents[0] = typeof(RectTransform);
        for (var index = 0; index < components.Length; index++)
        {
            objectComponents[index + 1] = components[index];
        }

        var gameObject = new GameObject(name, objectComponents);
        gameObject.layer = 5;
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static ScrollViewReferences CreateScrollView(string name, Transform parent)
    {
        var root = CreateUiObject(name, parent, typeof(Image), typeof(ScrollRect));
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        var viewport = CreateUiObject("Viewport", root.transform, typeof(Image), typeof(Mask));
        Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var contentRoot = CreateUiObject("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 12f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return new ScrollViewReferences(root, contentRoot, contentLayout);
    }

    private readonly struct ScrollViewReferences
    {
        public ScrollViewReferences(GameObject root, GameObject contentRoot, VerticalLayoutGroup contentLayout)
        {
            Root = root;
            ContentRoot = contentRoot;
            ContentLayout = contentLayout;
        }

        public GameObject Root { get; }
        public GameObject ContentRoot { get; }
        public VerticalLayoutGroup ContentLayout { get; }
    }
}
