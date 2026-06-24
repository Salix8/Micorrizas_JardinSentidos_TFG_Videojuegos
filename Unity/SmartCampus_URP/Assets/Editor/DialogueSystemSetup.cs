using System.IO;
using SmartCampus.Coop.Minigames;
using SmartCampus.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DialogueSystemSetup
{
    private const string DialogueFolder = "Assets/Dialogue";
    private const string ConfigFolder = "Assets/Dialogue/Config";
    private const string GeneratedFolder = "Assets/Dialogue/Generated";
    private const string PrefabFolder = "Assets/Prefabs/Dialogue";
    private const string PlaceholderSpritePath = GeneratedFolder + "/DeeprootPortraitPlaceholder.png";
    private const string PortraitDatabasePath = ConfigFolder + "/CharacterPortraitDatabase.asset";
    private const string AudioDatabasePath = ConfigFolder + "/DialogueAudioDatabase.asset";
    private const string DialogueSystemConfigPath = ConfigFolder + "/DialogueSystemConfig.asset";
    private const string DialoguePrefabPath = PrefabFolder + "/DialoguePanel.prefab";
    private const string DeeprootPrefabPath = "Assets/Prefabs/Deeproot.prefab";
    private const string DialogueScenePath = "Assets/Scenes/Dialogue.unity";
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string DialogueCsvPath = "Assets/Art/Narrativa.xlsx - Localizacion_Deeproot.csv";

    [MenuItem("SmartCampus/Dialogue/Setup/All")]
    public static void SetupAll()
    {
        EnsureDialogueAssetsAndPrefab();
        ConfigureDialogueDemoScene();
        ConfigureLobbyLanguageSelector();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("SmartCampus/Dialogue/Setup/Assets And Prefab")]
    public static void EnsureDialogueAssetsAndPrefab()
    {
        EnsureFolders();

        var placeholderSprite = EnsurePlaceholderSprite();
        var portraitDatabase = EnsurePortraitDatabase(placeholderSprite);
        var audioDatabase = EnsureAudioDatabase();
        var systemConfig = EnsureDialogueSystemConfig(portraitDatabase, audioDatabase);

        EnsureDialoguePrefab(systemConfig, placeholderSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("SmartCampus/Dialogue/Setup/Demo Scene")]
    public static void ConfigureDialogueDemoScene()
    {
        EnsureDialogueAssetsAndPrefab();

        var scene = EditorSceneManager.OpenScene(DialogueScenePath, OpenSceneMode.Single);
        var systemConfig = AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(DialogueSystemConfigPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabPath);
        if (systemConfig == null || prefab == null)
        {
            Debug.LogError("Dialogue assets are missing. Run the dialogue asset setup first.");
            return;
        }

        var existingDialoguePanel = GameObject.Find("DialoguePanel");
        var existingControlsPanel = GameObject.Find("DialogueDemoControls");
        if (existingDialoguePanel != null && existingControlsPanel != null)
        {
            BindExistingDialogueDemoScene(existingDialoguePanel, existingControlsPanel, systemConfig);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
        }

        var existingEventSystem = GameObject.Find("EventSystem");
        if (existingEventSystem != null)
        {
            Object.DestroyImmediate(existingEventSystem);
        }

        var canvas = CreateCanvasRoot();
        var safeAreaRoot = CreateSafeAreaRoot(canvas.transform);
        EnsureEventSystem();

        var controlsPanel = CreateDemoControlsPanel(safeAreaRoot.transform);
        var instantiatedPrefab = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instantiatedPrefab == null)
        {
            Debug.LogError("Dialogue prefab could not be instantiated into the demo scene.");
            return;
        }

        instantiatedPrefab.name = "DialoguePanel";
        instantiatedPrefab.transform.SetParent(safeAreaRoot.transform, false);
        var panelRect = instantiatedPrefab.GetComponent<RectTransform>();
        Stretch(panelRect, Vector2.zero, Vector2.zero);

        var dialogueController = instantiatedPrefab.GetComponent<DialogueUIController>();
        var selectorController = controlsPanel.AddComponent<DialogueLanguageSelectorUIController>();
        var demoController = controlsPanel.AddComponent<DialogueDemoUIController>();

        var sequenceDropdown = controlsPanel.transform.Find("SequenceRow/SequenceDropdown")?.GetComponent<TMP_Dropdown>();
        var languageDropdown = controlsPanel.transform.Find("LanguageRow/LanguageDropdown")?.GetComponent<TMP_Dropdown>();
        var playSequenceButton = controlsPanel.transform.Find("SequenceRow/PlaySequenceButton")?.GetComponent<Button>();
        var playLineButton = controlsPanel.transform.Find("LineRow/PlayLineButton")?.GetComponent<Button>();
        var lineInput = controlsPanel.transform.Find("LineRow/LineIdInput")?.GetComponent<TMP_InputField>();
        var feedbackLabel = controlsPanel.transform.Find("FeedbackLabel")?.GetComponent<TextMeshProUGUI>();

        AssignDialogueLanguageSelector(selectorController, systemConfig, languageDropdown, dialogueController);
        AssignDialogueDemoController(demoController, systemConfig, dialogueController, sequenceDropdown, playSequenceButton, lineInput, playLineButton, feedbackLabel);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("SmartCampus/Dialogue/Setup/Lobby Language Selector")]
    public static void ConfigureLobbyLanguageSelector()
    {
        EnsureDialogueAssetsAndPrefab();

        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        var systemConfig = AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(DialogueSystemConfigPath);
        if (systemConfig == null)
        {
            Debug.LogError("Dialogue system config is missing. Run the dialogue asset setup first.");
            return;
        }

        var headerStack = GameObject.Find("HeaderStack");
        if (headerStack == null)
        {
            Debug.LogError("HeaderStack was not found in Lobby scene.");
            return;
        }

        var existingPanel = headerStack.transform.Find("LanguageSelectorPanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel.gameObject);
        }

        var resources = BuildTmpResources();
        var panel = new GameObject(
            "LanguageSelectorPanel",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(HorizontalLayoutGroup));
        panel.transform.SetParent(headerStack.transform, false);

        var layoutElement = panel.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 110f;
        panel.GetComponent<Image>().color = new Color32(78, 53, 34, 170);

        var horizontalLayout = panel.GetComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(26, 26, 18, 18);
        horizontalLayout.spacing = 18f;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childForceExpandWidth = false;

        var label = CreateText("LanguageLabel", panel.transform, "Idioma del dispositivo", 28, FontStyles.Bold, TextAlignmentOptions.Left);
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 330f;

        var dropdown = CreateDropdown("LanguageDropdown", panel.transform, resources);
        var dropdownLayout = dropdown.GetComponent<LayoutElement>() ?? dropdown.gameObject.AddComponent<LayoutElement>();
        dropdownLayout.preferredWidth = 320f;

        var selectorController = panel.AddComponent<DialogueLanguageSelectorUIController>();
        AssignDialogueLanguageSelector(selectorController, systemConfig, dropdown, null);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureFolders()
    {
        EnsureFolder(DialogueFolder);
        EnsureFolder(ConfigFolder);
        EnsureFolder(GeneratedFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        var parentFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(parentFolder))
        {
            EnsureFolder(parentFolder);
        }

        AssetDatabase.CreateFolder(parentFolder, Path.GetFileName(assetPath));
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        var absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, PlaceholderSpritePath);
        if (!File.Exists(absolutePath))
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var horizontalBlend = x / (texture.width - 1f);
                    var verticalBlend = y / (texture.height - 1f);
                    var baseColor = Color.Lerp(new Color(0.19f, 0.31f, 0.23f), new Color(0.51f, 0.67f, 0.34f), horizontalBlend);
                    texture.SetPixel(x, y, Color.Lerp(baseColor, new Color(0.88f, 0.81f, 0.63f), verticalBlend * 0.35f));
                }
            }

            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(PlaceholderSpritePath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
    }

    private static CharacterPortraitDatabase EnsurePortraitDatabase(Sprite placeholderSprite)
    {
        var portraitDatabase = AssetDatabase.LoadAssetAtPath<CharacterPortraitDatabase>(PortraitDatabasePath);
        if (portraitDatabase == null)
        {
            portraitDatabase = ScriptableObject.CreateInstance<CharacterPortraitDatabase>();
            AssetDatabase.CreateAsset(portraitDatabase, PortraitDatabasePath);
        }

        var serializedObject = new SerializedObject(portraitDatabase);
        var fallbackPortraitProperty = serializedObject.FindProperty("fallbackPortrait");
        if (fallbackPortraitProperty.objectReferenceValue == null)
        {
            fallbackPortraitProperty.objectReferenceValue = placeholderSprite;
        }

        var entriesProperty = serializedObject.FindProperty("entries");
        var deeprootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeeprootPrefabPath);
        var entryProperty = FindCharacterEntry(entriesProperty, "Deeproot");
        if (entryProperty == null)
        {
            var newIndex = entriesProperty.arraySize;
            entriesProperty.arraySize++;
            entryProperty = entriesProperty.GetArrayElementAtIndex(newIndex);
            entryProperty.FindPropertyRelative("characterId").stringValue = "Deeproot";
        }

        var displayNameProperty = entryProperty.FindPropertyRelative("displayName");
        if (string.IsNullOrWhiteSpace(displayNameProperty.stringValue))
        {
            displayNameProperty.stringValue = "Deeproot";
        }

        var portraitProperty = entryProperty.FindPropertyRelative("portrait");
        if (portraitProperty.objectReferenceValue == null)
        {
            portraitProperty.objectReferenceValue = placeholderSprite;
        }

        var portraitVisualPrefabProperty = entryProperty.FindPropertyRelative("portraitVisualPrefab");
        if (portraitVisualPrefabProperty.objectReferenceValue == null)
        {
            portraitVisualPrefabProperty.objectReferenceValue = deeprootPrefab;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(portraitDatabase);
        return portraitDatabase;
    }

    private static DialogueAudioDatabase EnsureAudioDatabase()
    {
        var audioDatabase = AssetDatabase.LoadAssetAtPath<DialogueAudioDatabase>(AudioDatabasePath);
        if (audioDatabase == null)
        {
            audioDatabase = ScriptableObject.CreateInstance<DialogueAudioDatabase>();
            AssetDatabase.CreateAsset(audioDatabase, AudioDatabasePath);
        }

        EditorUtility.SetDirty(audioDatabase);
        return audioDatabase;
    }

    private static DialogueSystemConfig EnsureDialogueSystemConfig(
        CharacterPortraitDatabase portraitDatabase,
        DialogueAudioDatabase audioDatabase)
    {
        var systemConfig = AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(DialogueSystemConfigPath);
        if (systemConfig == null)
        {
            systemConfig = ScriptableObject.CreateInstance<DialogueSystemConfig>();
            AssetDatabase.CreateAsset(systemConfig, DialogueSystemConfigPath);
        }

        var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DialogueCsvPath);
        var serializedObject = new SerializedObject(systemConfig);
        var csvProperty = serializedObject.FindProperty("dialogueCsvAsset");
        if (csvProperty.objectReferenceValue == null)
        {
            csvProperty.objectReferenceValue = csvAsset;
        }

        var portraitDatabaseProperty = serializedObject.FindProperty("characterPortraitDatabase");
        if (portraitDatabaseProperty.objectReferenceValue == null)
        {
            portraitDatabaseProperty.objectReferenceValue = portraitDatabase;
        }

        var audioDatabaseProperty = serializedObject.FindProperty("dialogueAudioDatabase");
        if (audioDatabaseProperty.objectReferenceValue == null)
        {
            audioDatabaseProperty.objectReferenceValue = audioDatabase;
        }

        var typewriterSpeedProperty = serializedObject.FindProperty("typewriterCharactersPerSecond");
        if (typewriterSpeedProperty.floatValue <= 0f)
        {
            typewriterSpeedProperty.floatValue = 54f;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(systemConfig);
        return systemConfig;
    }

    private static void EnsureDialoguePrefab(DialogueSystemConfig systemConfig, Sprite placeholderSprite)
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabPath);
        if (existingPrefab != null)
        {
            RebindExistingDialoguePrefab(systemConfig);
            return;
        }

        var root = new GameObject(
            "DialoguePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(DialogueUIController),
            typeof(DialoguePanelView),
            typeof(AudioSource));
        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, Vector2.zero, Vector2.zero);

        var tapCatcher = root.GetComponent<Image>();
        tapCatcher.color = new Color(0f, 0f, 0f, 0.001f);
        tapCatcher.raycastTarget = true;

        var audioSource = root.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        var portraitStage = new GameObject("PortraitStage", typeof(RectTransform));
        portraitStage.transform.SetParent(root.transform, false);
        var portraitStageRect = portraitStage.GetComponent<RectTransform>();
        portraitStageRect.anchorMin = new Vector2(0f, 0f);
        portraitStageRect.anchorMax = new Vector2(0f, 0f);
        portraitStageRect.pivot = new Vector2(0f, 0f);
        portraitStageRect.anchoredPosition = new Vector2(48f, 246f);
        portraitStageRect.sizeDelta = new Vector2(240f, 320f);

        var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portrait.transform.SetParent(portraitStage.transform, false);
        var portraitRect = portrait.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0f);
        portraitRect.anchorMax = new Vector2(1f, 1f);
        portraitRect.offsetMin = Vector2.zero;
        portraitRect.offsetMax = Vector2.zero;
        var portraitImage = portrait.GetComponent<Image>();
        portraitImage.sprite = placeholderSprite;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        var portraitVisualRoot = new GameObject("PortraitVisualRoot", typeof(RectTransform));
        portraitVisualRoot.transform.SetParent(portraitStage.transform, false);
        var portraitVisualRect = portraitVisualRoot.GetComponent<RectTransform>();
        portraitVisualRect.anchorMin = Vector2.zero;
        portraitVisualRect.anchorMax = Vector2.one;
        portraitVisualRect.offsetMin = Vector2.zero;
        portraitVisualRect.offsetMax = Vector2.zero;

        var frame = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frame.transform.SetParent(root.transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = new Vector2(1f, 0f);
        frameRect.pivot = new Vector2(0.5f, 0f);
        frameRect.anchoredPosition = new Vector2(0f, 32f);
        frameRect.sizeDelta = new Vector2(-80f, 800f);
        var frameImage = frame.GetComponent<Image>();
        frameImage.color = new Color32(34, 24, 18, 238);
        frameImage.raycastTarget = false;

        portraitStage.transform.SetParent(root.transform, false);
        portraitStage.transform.SetAsFirstSibling();
        portraitStageRect.anchorMin = Vector2.zero;
        portraitStageRect.anchorMax = Vector2.zero;
        portraitStageRect.pivot = Vector2.zero;
        portraitStageRect.anchoredPosition = new Vector2(64f, 548f);
        portraitStageRect.sizeDelta = new Vector2(260f, 260f);

        var frameAccent = new GameObject("FrameAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameAccent.transform.SetParent(frame.transform, false);
        var frameAccentRect = frameAccent.GetComponent<RectTransform>();
        frameAccentRect.anchorMin = new Vector2(0f, 1f);
        frameAccentRect.anchorMax = new Vector2(1f, 1f);
        frameAccentRect.pivot = new Vector2(0.5f, 1f);
        frameAccentRect.offsetMin = new Vector2(0f, -8f);
        frameAccentRect.offsetMax = new Vector2(0f, 0f);
        frameAccent.GetComponent<Image>().color = new Color32(155, 113, 70, 255);

        var dialogueLabel = CreateText(
            "DialogueTextLabel",
            frame.transform,
            "Los dialogos apareceran aqui cuando se reproduzca una secuencia.",
            30,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        var dialogueLabelRect = dialogueLabel.GetComponent<RectTransform>();
        dialogueLabelRect.anchorMin = Vector2.zero;
        dialogueLabelRect.anchorMax = Vector2.one;
        dialogueLabelRect.pivot = new Vector2(0.5f, 0.5f);
        dialogueLabelRect.anchoredPosition = new Vector2(0f, -133f);
        dialogueLabelRect.sizeDelta = new Vector2(-88f, -334f);
        dialogueLabel.enableAutoSizing = true;
        dialogueLabel.fontSizeMin = 20f;
        dialogueLabel.fontSizeMax = 34f;
        dialogueLabel.textWrappingMode = TextWrappingModes.Normal;
        dialogueLabel.overflowMode = TextOverflowModes.Ellipsis;
        dialogueLabel.raycastTarget = false;

        var speakerBadge = new GameObject("SpeakerBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        speakerBadge.transform.SetParent(frame.transform, false);
        var speakerBadgeRect = speakerBadge.GetComponent<RectTransform>();
        speakerBadgeRect.anchorMin = new Vector2(0f, 1f);
        speakerBadgeRect.anchorMax = new Vector2(0f, 1f);
        speakerBadgeRect.pivot = new Vector2(0f, 1f);
        speakerBadgeRect.anchoredPosition = new Vector2(300f, -40f);
        speakerBadgeRect.sizeDelta = new Vector2(280f, 64f);
        var speakerBadgeImage = speakerBadge.GetComponent<Image>();
        speakerBadgeImage.color = new Color32(79, 53, 34, 245);
        speakerBadgeImage.raycastTarget = false;

        var speakerLabel = CreateText("SpeakerNameLabel", speakerBadge.transform, "Deeproot", 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        Stretch(speakerLabel.GetComponent<RectTransform>(), new Vector2(24f, 8f), new Vector2(-18f, -8f));
        speakerLabel.raycastTarget = false;

        var panelView = root.GetComponent<DialoguePanelView>();
        var panelViewSerializedObject = new SerializedObject(panelView);
        panelViewSerializedObject.FindProperty("panelRoot").objectReferenceValue = root;
        panelViewSerializedObject.FindProperty("portraitVisualRoot").objectReferenceValue = portraitVisualRoot.transform;
        panelViewSerializedObject.FindProperty("portraitImage").objectReferenceValue = portraitImage;
        panelViewSerializedObject.FindProperty("speakerNameLabel").objectReferenceValue = speakerLabel;
        panelViewSerializedObject.FindProperty("dialogueTextLabel").objectReferenceValue = dialogueLabel;
        panelViewSerializedObject.FindProperty("voiceAudioSource").objectReferenceValue = audioSource;
        panelViewSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        var controller = root.GetComponent<DialogueUIController>();
        var controllerSerializedObject = new SerializedObject(controller);
        controllerSerializedObject.FindProperty("dialogueSystemConfig").objectReferenceValue = systemConfig;
        controllerSerializedObject.FindProperty("dialoguePanelView").objectReferenceValue = panelView;
        controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        EnsureWaitingPanel(root);
        PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void RebindExistingDialoguePrefab(DialogueSystemConfig systemConfig)
    {
        var root = PrefabUtility.LoadPrefabContents(DialoguePrefabPath);
        if (root == null)
        {
            return;
        }

        var panelView = root.GetComponent<DialoguePanelView>();
        var controller = root.GetComponent<DialogueUIController>();
        var portraitVisualRoot = root.transform.Find("PortraitStage/PortraitVisualRoot");
        var portraitImage = root.transform.Find("PortraitStage/Portrait")?.GetComponent<Image>();
        var speakerLabel = root.transform.Find("Frame/SpeakerBadge/SpeakerNameLabel")?.GetComponent<TextMeshProUGUI>();
        var dialogueLabel = root.transform.Find("Frame/DialogueTextLabel")?.GetComponent<TextMeshProUGUI>();
        var audioSource = root.GetComponent<AudioSource>();

        if (panelView != null)
        {
            var panelViewSerializedObject = new SerializedObject(panelView);
            panelViewSerializedObject.FindProperty("panelRoot").objectReferenceValue = root;
            if (portraitVisualRoot != null)
            {
                panelViewSerializedObject.FindProperty("portraitVisualRoot").objectReferenceValue = portraitVisualRoot;
            }

            if (portraitImage != null)
            {
                panelViewSerializedObject.FindProperty("portraitImage").objectReferenceValue = portraitImage;
            }

            if (speakerLabel != null)
            {
                panelViewSerializedObject.FindProperty("speakerNameLabel").objectReferenceValue = speakerLabel;
            }

            if (dialogueLabel != null)
            {
                panelViewSerializedObject.FindProperty("dialogueTextLabel").objectReferenceValue = dialogueLabel;
            }

            if (audioSource != null)
            {
                panelViewSerializedObject.FindProperty("voiceAudioSource").objectReferenceValue = audioSource;
            }

            panelViewSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        if (controller != null)
        {
            var controllerSerializedObject = new SerializedObject(controller);
            controllerSerializedObject.FindProperty("dialogueSystemConfig").objectReferenceValue = systemConfig;
            controllerSerializedObject.FindProperty("dialoguePanelView").objectReferenceValue = panelView;
            controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        EnsurePortraitStageBottomAnchor(root);

        var responsiveLayoutController = root.GetComponent<DialogueResponsiveLayoutController>();
        if (responsiveLayoutController != null)
        {
            Object.DestroyImmediate(responsiveLayoutController);
        }

        EnsureWaitingPanel(root);
        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void EnsurePortraitStageBottomAnchor(GameObject root)
    {
        if (root == null || root.transform.Find("PortraitStage") is not RectTransform portraitStageRect)
        {
            return;
        }

        if (portraitStageRect.anchorMin != Vector2.zero ||
            portraitStageRect.anchorMax != Vector2.zero)
        {
            portraitStageRect.anchorMin = Vector2.zero;
            portraitStageRect.anchorMax = Vector2.zero;
        }

        var currentBottomLeft = portraitStageRect.anchoredPosition -
                                Vector2.Scale(portraitStageRect.sizeDelta, portraitStageRect.pivot);
        portraitStageRect.pivot = Vector2.zero;
        portraitStageRect.anchoredPosition = currentBottomLeft;
    }

    private static DialogueWaitingPanelView EnsureWaitingPanel(GameObject root)
    {
        var existing = root.transform.Find("WaitingPanel");
        var waitingPanel = existing == null
            ? new GameObject(
                "WaitingPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(DialogueWaitingPanelView))
            : existing.gameObject;

        waitingPanel.transform.SetParent(root.transform, false);
        waitingPanel.transform.SetAsLastSibling();
        var rectTransform = waitingPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one;
        rectTransform.anchoredPosition = new Vector2(-40f, -40f);
        rectTransform.sizeDelta = new Vector2(320f, 150f);

        var background = waitingPanel.GetComponent<Image>() ?? waitingPanel.AddComponent<Image>();
        background.color = new Color(0.16f, 0.1f, 0.06f, 0.94f);
        background.raycastTarget = false;

        var canvasGroup = waitingPanel.GetComponent<CanvasGroup>() ?? waitingPanel.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var view = waitingPanel.GetComponent<DialogueWaitingPanelView>() ??
                   waitingPanel.AddComponent<DialogueWaitingPanelView>();
        waitingPanel.SetActive(false);
        return view;
    }

    private static GameObject CreateCanvasRoot()
    {
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static GameObject CreateSafeAreaRoot(Transform parent)
    {
        var safeAreaRoot = new GameObject("SafeAreaRoot", typeof(RectTransform));
        safeAreaRoot.transform.SetParent(parent, false);
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        safeAreaRoot.AddComponent<SafeAreaFitter>();
        return safeAreaRoot;
    }

    private static void EnsureEventSystem()
    {
        if (GameObject.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, SceneManager.GetActiveScene());
    }

    private static GameObject CreateDemoControlsPanel(Transform parent)
    {
        var resources = BuildTmpResources();
        var panel = new GameObject(
            "DialogueDemoControls",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(32f, -260f);
        rect.offsetMax = new Vector2(-32f, -32f);

        panel.GetComponent<Image>().color = new Color32(43, 30, 22, 205);

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var title = CreateText("TitleLabel", panel.transform, "Dialogue Sandbox", 34, FontStyles.Bold, TextAlignmentOptions.Left);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        var languageRow = new GameObject("LanguageRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        languageRow.transform.SetParent(panel.transform, false);
        languageRow.GetComponent<LayoutElement>().preferredHeight = 80f;
        var languageLayout = languageRow.GetComponent<HorizontalLayoutGroup>();
        languageLayout.spacing = 12f;
        languageLayout.childAlignment = TextAnchor.MiddleLeft;
        languageLayout.childControlWidth = false;
        languageLayout.childControlHeight = false;
        languageLayout.childForceExpandWidth = false;

        var languageLabel = CreateText("LanguageLabel", languageRow.transform, "Idioma", 28, FontStyles.Bold, TextAlignmentOptions.Left);
        languageLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        CreateDropdown("LanguageDropdown", languageRow.transform, resources);

        var sequenceRow = new GameObject("SequenceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        sequenceRow.transform.SetParent(panel.transform, false);
        sequenceRow.GetComponent<LayoutElement>().preferredHeight = 80f;
        var sequenceLayout = sequenceRow.GetComponent<HorizontalLayoutGroup>();
        sequenceLayout.spacing = 12f;
        sequenceLayout.childAlignment = TextAnchor.MiddleLeft;
        sequenceLayout.childControlWidth = false;
        sequenceLayout.childControlHeight = false;
        sequenceLayout.childForceExpandWidth = false;

        var sequenceLabel = CreateText("SequenceLabel", sequenceRow.transform, "Secuencia", 28, FontStyles.Bold, TextAlignmentOptions.Left);
        sequenceLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        CreateDropdown("SequenceDropdown", sequenceRow.transform, resources);
        CreateButton("PlaySequenceButton", sequenceRow.transform, "Reproducir", resources);

        var lineRow = new GameObject("LineRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        lineRow.transform.SetParent(panel.transform, false);
        lineRow.GetComponent<LayoutElement>().preferredHeight = 80f;
        var lineLayout = lineRow.GetComponent<HorizontalLayoutGroup>();
        lineLayout.spacing = 12f;
        lineLayout.childAlignment = TextAnchor.MiddleLeft;
        lineLayout.childControlWidth = false;
        lineLayout.childControlHeight = false;
        lineLayout.childForceExpandWidth = false;

        var lineLabel = CreateText("LineLabel", lineRow.transform, "String ID", 28, FontStyles.Bold, TextAlignmentOptions.Left);
        lineLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        CreateInputField("LineIdInput", lineRow.transform, "Ej: DL_DEEPROOT_ACT1_01", resources);
        CreateButton("PlayLineButton", lineRow.transform, "PlayLine", resources);

        var feedbackLabel = CreateText("FeedbackLabel", panel.transform, "Selecciona una secuencia o introduce un String ID.", 24, FontStyles.Italic, TextAlignmentOptions.Left);
        feedbackLabel.color = new Color32(228, 219, 205, 255);
        feedbackLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

        return panel;
    }

    private static void BindExistingDialogueDemoScene(
        GameObject existingDialoguePanel,
        GameObject existingControlsPanel,
        DialogueSystemConfig systemConfig)
    {
        EnsureEventSystem();

        if (PrefabUtility.IsPartOfPrefabInstance(existingDialoguePanel))
        {
            PrefabUtility.RevertPrefabInstance(existingDialoguePanel, InteractionMode.AutomatedAction);
        }

        if (existingDialoguePanel.transform is RectTransform panelRect)
        {
            Stretch(panelRect, Vector2.zero, Vector2.zero);
        }

        var dialogueController = existingDialoguePanel.GetComponent<DialogueUIController>();
        var panelView = existingDialoguePanel.GetComponent<DialoguePanelView>();
        if (dialogueController != null)
        {
            var controllerSerializedObject = new SerializedObject(dialogueController);
            controllerSerializedObject.FindProperty("dialogueSystemConfig").objectReferenceValue = systemConfig;
            controllerSerializedObject.FindProperty("dialoguePanelView").objectReferenceValue = panelView;
            controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        var selectorController = existingControlsPanel.GetComponent<DialogueLanguageSelectorUIController>() ??
                                 existingControlsPanel.AddComponent<DialogueLanguageSelectorUIController>();
        var demoController = existingControlsPanel.GetComponent<DialogueDemoUIController>() ??
                             existingControlsPanel.AddComponent<DialogueDemoUIController>();

        var sequenceDropdown = existingControlsPanel.transform.Find("SequenceRow/SequenceDropdown")?.GetComponent<TMP_Dropdown>();
        var languageDropdown = existingControlsPanel.transform.Find("LanguageRow/LanguageDropdown")?.GetComponent<TMP_Dropdown>();
        var playSequenceButton = existingControlsPanel.transform.Find("SequenceRow/PlaySequenceButton")?.GetComponent<Button>();
        var playLineButton = existingControlsPanel.transform.Find("LineRow/PlayLineButton")?.GetComponent<Button>();
        var lineInput = existingControlsPanel.transform.Find("LineRow/LineIdInput")?.GetComponent<TMP_InputField>();
        var feedbackLabel = existingControlsPanel.transform.Find("FeedbackLabel")?.GetComponent<TextMeshProUGUI>();

        AssignDialogueLanguageSelector(selectorController, systemConfig, languageDropdown, dialogueController);
        AssignDialogueDemoController(demoController, systemConfig, dialogueController, sequenceDropdown, playSequenceButton, lineInput, playLineButton, feedbackLabel);
    }

    private static TMP_DefaultControls.Resources BuildTmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);

        var label = gameObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        label.color = new Color32(250, 244, 232, 255);
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, TMP_DefaultControls.Resources resources)
    {
        var gameObject = TMP_DefaultControls.CreateButton(resources);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 72f);

        var label = gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 30f;
            label.text = text;
            label.color = new Color32(255, 247, 234, 255);
        }

        var image = gameObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color32(82, 54, 35, 220);
        }

        return gameObject.GetComponent<Button>();
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent, TMP_DefaultControls.Resources resources)
    {
        var dropdownBackgroundColor = new Color32(110, 91, 68, 255);
        var dropdownHighlightColor = new Color32(205, 170, 126, 255);
        var dropdownTextColor = new Color32(69, 33, 8, 255);

        var gameObject = TMP_DefaultControls.CreateDropdown(resources);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 72f);

        var image = gameObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = dropdownBackgroundColor;
        }

        var dropdown = gameObject.GetComponent<TMP_Dropdown>();
        var dropdownColors = dropdown.colors;
        dropdownColors.normalColor = dropdownBackgroundColor;
        dropdownColors.highlightedColor = dropdownHighlightColor;
        dropdownColors.selectedColor = dropdownHighlightColor;
        dropdownColors.pressedColor = dropdownHighlightColor;
        dropdown.colors = dropdownColors;
        dropdown.options.Clear();
        if (dropdown.captionText != null)
        {
            dropdown.captionText.font = TMP_Settings.defaultFontAsset;
            dropdown.captionText.fontSize = 28f;
            dropdown.captionText.color = dropdownTextColor;
        }

        if (dropdown.itemText != null)
        {
            dropdown.itemText.font = TMP_Settings.defaultFontAsset;
            dropdown.itemText.fontSize = 26f;
            dropdown.itemText.color = dropdownTextColor;
        }

        var template = gameObject.transform.Find("Template");
        if (template != null)
        {
            if (template.GetComponent<Image>() is Image templateImage)
            {
                templateImage.color = dropdownBackgroundColor;
            }

            var viewport = template.Find("Viewport");
            if (viewport != null && viewport.GetComponent<Image>() is Image viewportImage)
            {
                viewportImage.color = dropdownBackgroundColor;
            }

            var item = template.Find("Viewport/Content/Item");
            if (item != null)
            {
                if (item.GetComponent<Toggle>() is Toggle itemToggle)
                {
                    var colors = itemToggle.colors;
                    colors.normalColor = dropdownBackgroundColor;
                    colors.highlightedColor = dropdownHighlightColor;
                    colors.selectedColor = dropdownHighlightColor;
                    colors.pressedColor = dropdownHighlightColor;
                    itemToggle.colors = colors;
                }

                var itemBackground = item.Find("Item Background");
                if (itemBackground != null && itemBackground.GetComponent<Image>() is Image itemBackgroundImage)
                {
                    // Keep the base graphic neutral so the Toggle ColorBlock is the only source
                    // of item background tint across normal/highlight/pressed/selected states.
                    itemBackgroundImage.color = Color.white;
                }

                var itemCheckmark = item.Find("Item Checkmark");
                if (itemCheckmark != null && itemCheckmark.GetComponent<Image>() is Image itemCheckmarkImage)
                {
                    itemCheckmarkImage.color = new Color32(218, 194, 153, 255);
                }

                var itemLabel = item.Find("Item Label");
                if (itemLabel != null && itemLabel.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI itemLabelText)
                {
                    itemLabelText.color = dropdownTextColor;
                }
            }
        }

        return dropdown;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText, TMP_DefaultControls.Resources resources)
    {
        var gameObject = TMP_DefaultControls.CreateInputField(resources);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 72f);

        var image = gameObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color32(70, 49, 35, 220);
        }

        var inputField = gameObject.GetComponent<TMP_InputField>();
        if (inputField.textViewport != null)
        {
            inputField.textViewport.offsetMin = new Vector2(18f, 10f);
            inputField.textViewport.offsetMax = new Vector2(-18f, -10f);
        }

        if (inputField.textComponent != null)
        {
            inputField.textComponent.font = TMP_Settings.defaultFontAsset;
            inputField.textComponent.fontSize = 28f;
            inputField.textComponent.color = new Color32(250, 244, 232, 255);
        }

        if (inputField.placeholder is TextMeshProUGUI placeholderLabel)
        {
            placeholderLabel.font = TMP_Settings.defaultFontAsset;
            placeholderLabel.fontSize = 28f;
            placeholderLabel.text = placeholderText;
            placeholderLabel.color = new Color32(220, 210, 195, 180);
        }

        return inputField;
    }

    private static void AssignDialogueLanguageSelector(
        DialogueLanguageSelectorUIController selectorController,
        DialogueSystemConfig systemConfig,
        TMP_Dropdown languageDropdown,
        DialogueUIController targetController)
    {
        var serializedObject = new SerializedObject(selectorController);
        serializedObject.FindProperty("dialogueSystemConfig").objectReferenceValue = systemConfig;
        serializedObject.FindProperty("languageDropdown").objectReferenceValue = languageDropdown;
        serializedObject.FindProperty("targetDialogueUIController").objectReferenceValue = targetController;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignDialogueDemoController(
        DialogueDemoUIController demoController,
        DialogueSystemConfig systemConfig,
        DialogueUIController dialogueController,
        TMP_Dropdown sequenceDropdown,
        Button playSequenceButton,
        TMP_InputField lineInput,
        Button playLineButton,
        TextMeshProUGUI feedbackLabel)
    {
        var serializedObject = new SerializedObject(demoController);
        serializedObject.FindProperty("dialogueSystemConfig").objectReferenceValue = systemConfig;
        serializedObject.FindProperty("dialogueUIController").objectReferenceValue = dialogueController;
        serializedObject.FindProperty("sequenceDropdown").objectReferenceValue = sequenceDropdown;
        serializedObject.FindProperty("playSequenceButton").objectReferenceValue = playSequenceButton;
        serializedObject.FindProperty("lineIdInput").objectReferenceValue = lineInput;
        serializedObject.FindProperty("playLineButton").objectReferenceValue = playLineButton;
        serializedObject.FindProperty("feedbackLabel").objectReferenceValue = feedbackLabel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty FindCharacterEntry(SerializedProperty entriesProperty, string characterId)
    {
        for (var index = 0; index < entriesProperty.arraySize; index++)
        {
            var entry = entriesProperty.GetArrayElementAtIndex(index);
            var entryCharacterId = entry.FindPropertyRelative("characterId").stringValue;
            if (string.Equals(entryCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }
}
