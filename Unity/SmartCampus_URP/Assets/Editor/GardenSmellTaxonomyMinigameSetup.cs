using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;

public static class GardenSmellTaxonomyMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string SceneFolder = "Assets/Scenes";
    private const string StreamingAssetsFolder = "Assets/StreamingAssets/CoopMinigames";
    private const string MinigameFolder = StreamingAssetsFolder + "/05-GardenSmellTaxonomy";
    private const string TutorialConfigPath = ConfigFolder + "/GardenSmellTaxonomyTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/GardenSmellTaxonomyMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string CsvTemplatePath = MinigameFolder + "/GardenSmellTaxonomyPlants.csv";
    private const string MinigameScenePath = SceneFolder + "/GardenSmellTaxonomyMinigame.unity";
    private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
    private const string MainMapScenePath = SceneFolder + "/UJI.unity";
    private const string MinigameSceneName = "GardenSmellTaxonomyMinigame";
    private const int MinigameIndex = 5;

    [MenuItem("Tools/Coop/Setup Garden Smell Taxonomy Minigame")]
    public static void SetupGardenSmellTaxonomyMinigame()
    {
        EnsureFolders();

        var tutorialContent = CreateOrUpdateTutorialContent();
        var minigameConfig = CreateOrUpdateMinigameConfig(tutorialContent);
        var catalogConfig = CreateOrUpdateCatalogConfig();
        CreateCsvTemplateIfMissing();

        SetupMinigameScene(minigameConfig);
        SetupLobbyScene();
        SetupMainMapScene(catalogConfig);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "CoopMinigames");
        EnsureFolder(RootFolder, "Configs");
        EnsureFolder("Assets", "StreamingAssets");
        EnsureFolder("Assets/StreamingAssets", "CoopMinigames");
        EnsureFolder(StreamingAssetsFolder, "05-GardenSmellTaxonomy");
        EnsureFolder(MinigameFolder, "Plants");
        EnsureFolder("Assets", "Scenes");
    }

    private static void EnsureFolder(string parent, string name)
    {
        var fullPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static MinigameTutorialContentConfig CreateOrUpdateTutorialContent()
    {
        var asset = AssetDatabase.LoadAssetAtPath<MinigameTutorialContentConfig>(TutorialConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MinigameTutorialContentConfig>();
            AssetDatabase.CreateAsset(asset, TutorialConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("title").stringValue = "Taxonomia del jardin del olfato";
        serializedObject.FindProperty("subtitle").stringValue = "Clasifica cada planta por su uso principal";
        serializedObject.FindProperty("bodyText").stringValue =
            "La planta activa aparece abajo con su imagen y nombre cientifico.\n\n" +
            "Arrastra esa planta hacia una de las tres categorias centrales: decoracion, alimentacion o curacion.\n\n" +
            "Si aciertas, su nombre cientifico quedara en verde dentro de la categoria correcta. Si fallas, aparecera en rojo en la categoria real para que todo el grupo vea donde debia ir.\n\n" +
            "El estado es compartido entre todos los dispositivos y la nota final es grupal sobre 10.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GardenSmellTaxonomyMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GardenSmellTaxonomyMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<GardenSmellTaxonomyMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Minijuego 5 - Taxonomia del jardin del olfato";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Taxonomia completada";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("csvRelativePath").stringValue = "CoopMinigames/05-GardenSmellTaxonomy/GardenSmellTaxonomyPlants.csv";
        serializedObject.FindProperty("minimumSupportedPlayers").intValue = 2;
        serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
        serializedObject.FindProperty("minimumRequiredPlants").intValue = 6;
        serializedObject.FindProperty("maxPlantsPerMatch").intValue = 9;
        serializedObject.FindProperty("shufflePlants").boolValue = true;
        serializedObject.FindProperty("timeLimitSeconds").floatValue = 210f;
        serializedObject.FindProperty("transitionDuration").floatValue = 0.2f;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        return CoopMinigameSetupEditorUtility.UpsertCatalogEntry(
            CatalogConfigPath,
            MinigameIndex,
            "Minijuego 5 - Taxonomia del jardin del olfato",
            "Clasifica en grupo cada planta entre decoracion, alimentacion y curacion con feedback comun y nota compartida.",
            MinigameSceneName);
    }

    private static void CreateCsvTemplateIfMissing()
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), CsvTemplatePath);
        var folderPath = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        if (!File.Exists(absolutePath))
        {
            File.WriteAllText(absolutePath, BuildCsvTemplate());
        }

        AssetDatabase.ImportAsset(CsvTemplatePath);
    }

    private static void SetupMinigameScene(GardenSmellTaxonomyMinigameConfig config)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureInputSystemUiEventSystem();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, config.VisualSettings.BackgroundColor);

        var sessionObject = new GameObject("GardenSmellTaxonomySession", typeof(Unity.Netcode.NetworkObject), typeof(GardenSmellTaxonomyMinigameSession));
        var session = sessionObject.GetComponent<GardenSmellTaxonomyMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("gardenSmellTaxonomyMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("GardenSmellTaxonomyCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = config.VisualSettings.BackgroundColor;

        var uiRoot = CreateUiObject("GardenSmellTaxonomyUI", safeAreaRoot.transform, typeof(GardenSmellTaxonomyMinigameUIController));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        waitingPanel.AddComponent<ResponsivePanelLayoutController>().Configure(canvas.GetComponent<RectTransform>(), 0.8f, 0.2f, new Vector2(280f, 180f), new Vector2(720f, 260f), new Vector2(32f, 32f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, font, "Esperando al resto del grupo.", 28, TextAnchor.MiddleCenter);
        Stretch(waitingStatus.rectTransform, new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(32f, 32f), new Vector2(-32f, -32f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(24, 24, 20, 20);
        gameplayLayout.spacing = 12f;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childForceExpandHeight = false;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, font, config.DisplayName, 42, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

        var statusPanel = CreatePanel("StatusPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 176f;
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(20, 20, 16, 16);
        statusLayout.spacing = 8f;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;
        statusLayout.childForceExpandHeight = false;

        var timerLabel = CreateText("TimerLabel", statusPanel.transform, font, "Tiempo restante: 03:30", 24, TextAnchor.MiddleLeft);
        timerLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        var progressLabel = CreateText("ProgressLabel", statusPanel.transform, font, "Plantas clasificadas: 0/0", 24, TextAnchor.MiddleLeft);
        progressLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        var sharedScoreLabel = CreateText("SharedScoreLabel", statusPanel.transform, font, "Aciertos compartidos: 0   Fallos: 0", 24, TextAnchor.MiddleLeft);
        sharedScoreLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        var statusLabel = CreateText("StatusLabel", statusPanel.transform, font, "Arrastra la planta activa hacia la categoria correcta.", 20, TextAnchor.UpperLeft);
        statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var centerArea = CreateUiObject("CenterArea", gameplayPanel.transform, typeof(HorizontalLayoutGroup));
        var centerAreaLayout = centerArea.GetComponent<HorizontalLayoutGroup>();
        centerAreaLayout.spacing = 10f;
        centerAreaLayout.childControlWidth = true;
        centerAreaLayout.childControlHeight = true;
        centerAreaLayout.childForceExpandWidth = true;
        centerAreaLayout.childForceExpandHeight = true;
        centerArea.AddComponent<LayoutElement>().flexibleHeight = 1f;
        centerArea.GetComponent<LayoutElement>().minHeight = 320f;

        var decorationZone = CreateDropZone("DecorationZone", centerArea.transform, font, GardenSmellTaxonomyCategory.Decoration, config.VisualSettings);
        var foodZone = CreateDropZone("FoodZone", centerArea.transform, font, GardenSmellTaxonomyCategory.Food, config.VisualSettings);
        var healingZone = CreateDropZone("HealingZone", centerArea.transform, font, GardenSmellTaxonomyCategory.Healing, config.VisualSettings);

        var bottomArea = CreatePanel("BottomArea", gameplayPanel.transform, new Color(1f, 1f, 1f, 0.06f));
        bottomArea.AddComponent<LayoutElement>().preferredHeight = 320f;

        var cardRoot = CreateUiObject(
            "PlantCard",
            bottomArea.transform,
            typeof(Image),
            typeof(CanvasGroup),
            typeof(GardenSmellTaxonomyPlantCardView),
            typeof(ResponsiveAspectRatioLayoutController));
        var cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(480f, 400f);
        cardRoot.GetComponent<Image>().color = config.VisualSettings.CardColor;
        cardRoot.GetComponent<ResponsiveAspectRatioLayoutController>().Configure(
            bottomArea.GetComponent<RectTransform>(),
            480f / 400f,
            new Vector2(220f, 190f),
            new Vector2(540f, 420f),
            new Vector2(24f, 24f));

        var frame = CreateUiObject("Frame", cardRoot.transform, typeof(Image));
        Stretch(frame.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -6f));
        frame.GetComponent<Image>().color = config.VisualSettings.CardFrameColor;
        frame.transform.SetAsFirstSibling();

        var illustration = CreateUiObject("Illustration", cardRoot.transform, typeof(Image));
        var illustrationRect = illustration.GetComponent<RectTransform>();
        illustrationRect.anchorMin = new Vector2(0.08f, 0.38f);
        illustrationRect.anchorMax = new Vector2(0.92f, 0.9f);
        illustrationRect.offsetMin = Vector2.zero;
        illustrationRect.offsetMax = Vector2.zero;
        illustration.GetComponent<Image>().preserveAspect = true;

        var illustrationPlaceholder = CreatePanel("IllustrationPlaceholder", illustration.transform, new Color(0.79f, 0.82f, 0.77f, 1f));
        Stretch(illustrationPlaceholder.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        var illustrationPlaceholderLabel = CreateText("IllustrationPlaceholderLabel", illustrationPlaceholder.transform, font, "Imagen de la planta", 28, TextAnchor.MiddleCenter);
        Stretch(illustrationPlaceholderLabel.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -16f));

        var scientificNameLabel = CreateText("ScientificNameLabel", cardRoot.transform, font, "Lavandula dentata", 32, TextAnchor.MiddleCenter);
        var scientificNameRect = scientificNameLabel.rectTransform;
        scientificNameRect.anchorMin = new Vector2(0.08f, 0.2f);
        scientificNameRect.anchorMax = new Vector2(0.92f, 0.32f);
        scientificNameRect.offsetMin = Vector2.zero;
        scientificNameRect.offsetMax = Vector2.zero;

        var helperLabel = CreateText("HelperLabel", cardRoot.transform, font, "Arrastra la planta hasta su uso principal.", 20, TextAnchor.UpperCenter);
        var helperRect = helperLabel.rectTransform;
        helperRect.anchorMin = new Vector2(0.08f, 0.06f);
        helperRect.anchorMax = new Vector2(0.92f, 0.16f);
        helperRect.offsetMin = Vector2.zero;
        helperRect.offsetMax = Vector2.zero;

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);

        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);

        var cardView = cardRoot.GetComponent<GardenSmellTaxonomyPlantCardView>();
        var serializedCardView = new SerializedObject(cardView);
        serializedCardView.FindProperty("cardTransform").objectReferenceValue = cardRect;
        serializedCardView.FindProperty("canvasGroup").objectReferenceValue = cardRoot.GetComponent<CanvasGroup>();
        serializedCardView.FindProperty("frameImage").objectReferenceValue = frame.GetComponent<Image>();
        serializedCardView.FindProperty("illustrationImage").objectReferenceValue = illustration.GetComponent<Image>();
        serializedCardView.FindProperty("illustrationPlaceholderRoot").objectReferenceValue = illustrationPlaceholder;
        serializedCardView.FindProperty("scientificNameLabel").objectReferenceValue = scientificNameLabel;
        serializedCardView.FindProperty("helperLabel").objectReferenceValue = helperLabel;
        serializedCardView.ApplyModifiedPropertiesWithoutUndo();

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<GardenSmellTaxonomyMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("gardenSmellTaxonomyMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("plantCardView").objectReferenceValue = cardView;
        serializedUiController.FindProperty("decorationDropZone").objectReferenceValue = decorationZone.DropZoneView;
        serializedUiController.FindProperty("foodDropZone").objectReferenceValue = foodZone.DropZoneView;
        serializedUiController.FindProperty("healingDropZone").objectReferenceValue = healingZone.DropZoneView;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("progressLabel").objectReferenceValue = progressLabel;
        serializedUiController.FindProperty("sharedScoreLabel").objectReferenceValue = sharedScoreLabel;
        serializedUiController.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedUiController.ApplyModifiedPropertiesWithoutUndo();

        FantasyWoodenThemeUtility.ApplyThemeToOpenScene(scene);
        EditorSceneManager.SaveScene(scene, MinigameScenePath);
    }

    private static DropZoneReferences CreateDropZone(string name, Transform parent, Font font, GardenSmellTaxonomyCategory category, GardenSmellTaxonomyVisualSettings visuals)
    {
        var zoneRoot = CreateUiObject(name, parent, typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(GardenSmellTaxonomyDropZoneView));
        zoneRoot.GetComponent<Image>().color = visuals.PanelColor;
        zoneRoot.GetComponent<LayoutElement>().flexibleWidth = 1f;
        zoneRoot.GetComponent<LayoutElement>().flexibleHeight = 1f;
        zoneRoot.GetComponent<LayoutElement>().minWidth = 190f;

        var layout = zoneRoot.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var accent = CreateUiObject("Accent", zoneRoot.transform, typeof(Image));
        accent.AddComponent<LayoutElement>().preferredHeight = 8f;
        accent.GetComponent<Image>().color = visuals.GetCategoryColor(category);

        var titleLabel = CreateText("TitleLabel", zoneRoot.transform, font, GardenSmellTaxonomyCategoryLabels.GetDisplayName(category), 28, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        var badge = CreateUiObject("Badge", zoneRoot.transform, typeof(Image), typeof(LayoutElement));
        badge.GetComponent<Image>().color = visuals.GetCategoryColor(category);
        badge.GetComponent<LayoutElement>().preferredWidth = 96f;
        badge.GetComponent<LayoutElement>().preferredHeight = 96f;

        var badgeLabel = CreateText("BadgeLabel", badge.transform, font, GardenSmellTaxonomyCategoryLabels.GetBadgeLabel(category), 32, TextAnchor.MiddleCenter);
        Stretch(badgeLabel.rectTransform, Vector2.zero, Vector2.zero);
        badgeLabel.color = Color.white;

        var subtitleLabel = CreateText("SubtitleLabel", zoneRoot.transform, font, GardenSmellTaxonomyCategoryLabels.GetSupportText(category), 18, TextAnchor.MiddleCenter);
        subtitleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 0f;
        subtitleLabel.gameObject.SetActive(false);

        var historyPanel = CreatePanel("HistoryPanel", zoneRoot.transform, new Color(1f, 1f, 1f, 0.08f));
        historyPanel.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var emptyStateLabel = CreateText("EmptyStateLabel", historyPanel.transform, font, "Todavia no hay plantas clasificadas.", 18, TextAnchor.MiddleCenter);
        Stretch(emptyStateLabel.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));

        var scrollView = CreateScrollView("HistoryScrollView", historyPanel.transform);
        Stretch(scrollView.Root.GetComponent<RectTransform>(), new Vector2(8f, 8f), new Vector2(-8f, -8f));
        scrollView.ContentVerticalLayout.spacing = 8f;
        var historyTemplate = CreateText("HistoryEntryTemplate", scrollView.ContentRoot.transform, font, "Lavandula dentata", 18, TextAnchor.MiddleLeft);
        historyTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
        historyTemplate.gameObject.SetActive(false);

        var dropZoneView = zoneRoot.GetComponent<GardenSmellTaxonomyDropZoneView>();
        var serializedDropZone = new SerializedObject(dropZoneView);
        serializedDropZone.FindProperty("category").enumValueIndex = (int)category;
        serializedDropZone.FindProperty("zoneTransform").objectReferenceValue = zoneRoot.GetComponent<RectTransform>();
        serializedDropZone.FindProperty("panelImage").objectReferenceValue = zoneRoot.GetComponent<Image>();
        serializedDropZone.FindProperty("accentImage").objectReferenceValue = accent.GetComponent<Image>();
        serializedDropZone.FindProperty("badgeImage").objectReferenceValue = badge.GetComponent<Image>();
        serializedDropZone.FindProperty("badgeLabel").objectReferenceValue = badgeLabel;
        serializedDropZone.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedDropZone.FindProperty("subtitleLabel").objectReferenceValue = subtitleLabel;
        serializedDropZone.FindProperty("emptyStateLabel").objectReferenceValue = emptyStateLabel;
        serializedDropZone.FindProperty("historyRoot").objectReferenceValue = scrollView.ContentRoot.transform;
        serializedDropZone.FindProperty("historyEntryTemplate").objectReferenceValue = historyTemplate;
        serializedDropZone.ApplyModifiedPropertiesWithoutUndo();

        return new DropZoneReferences(zoneRoot, dropZoneView);
    }

    private static void SetupLobbyScene()
    {
        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        var coordinator = UnityEngine.Object.FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        if (coordinator == null)
        {
            throw new InvalidOperationException("La escena de lobby necesita un CoopSessionCoordinator.");
        }

        var serializedCoordinator = new SerializedObject(coordinator);
        var miniGameSceneNames = serializedCoordinator.FindProperty("miniGameSceneNames");
        miniGameSceneNames.arraySize = Math.Max(MinigameIndex + 1, miniGameSceneNames.arraySize);
        miniGameSceneNames.GetArrayElementAtIndex(MinigameIndex).stringValue = MinigameSceneName;
        serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(coordinator);
        FantasyWoodenThemeUtility.ApplyThemeToOpenScene(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupMainMapScene(CoopMinigameCatalogConfig catalogConfig)
    {
        CoopMinigameSetupEditorUtility.ConfigureMainMapLauncher(catalogConfig);
    }

    private static void UpdateBuildSettings()
    {
        var scenePaths = new[]
        {
            MainMapScenePath,
            LobbyScenePath,
            MinigameScenePath
        };

        var scenes = EditorBuildSettings.scenes;
        foreach (var scenePath in scenePaths)
        {
            var exists = false;
            for (var index = 0; index < scenes.Length; index++)
            {
                if (string.Equals(scenes[index].path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scenes[index].enabled = true;
                    exists = true;
                    break;
                }
            }

            if (!exists && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
            {
                ArrayUtility.Add(ref scenes, new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes;
    }

    private static TutorialPopupController CreateTutorialPopup(Transform parent, Font font)
    {
        return TutorialPopupPrefabUtility.InstantiateTutorialPopup(parent);
    }

    private static MinigameResultView CreateResultPopup(Transform parent, Font font)
    {
        var popupRoot = CreateUiObject("ResultPopup", parent, typeof(MinigameResultView));
        Stretch(popupRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", popupRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

        var contentPanel = CreatePanel("ContentPanel", popupRoot.transform, new Color(0.95f, 0.97f, 0.94f, 1f));
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.86f, 0.42f, new Vector2(320f, 320f), new Vector2(720f, 560f), new Vector2(32f, 32f));
        var contentRect = contentPanel.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText("TitleLabel", contentPanel.transform, font, "Partida terminada", 36, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        var score = CreateText("ScoreLabel", contentPanel.transform, font, "0/10", 52, TextAnchor.MiddleCenter);
        score.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;
        var summary = CreateText("SummaryLabel", contentPanel.transform, font, "Aciertos: 0", 24, TextAnchor.MiddleCenter);
        summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
        var returnButton = CreateButton("ReturnButton", contentPanel.transform, font, "Volver al mapa", 22, new Color(0.21f, 0.42f, 0.46f, 1f));
        returnButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var waitingLabel = CreateText("WaitingHostLabel", contentPanel.transform, font, "Esperando a que el host vuelva al mapa.", 22, TextAnchor.MiddleCenter);
        waitingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        var resultView = popupRoot.GetComponent<MinigameResultView>();
        var serializedResultView = new SerializedObject(resultView);
        serializedResultView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedResultView.FindProperty("scoreLabel").objectReferenceValue = score;
        serializedResultView.FindProperty("summaryLabel").objectReferenceValue = summary;
        serializedResultView.FindProperty("returnButton").objectReferenceValue = returnButton.GetComponent<Button>();
        serializedResultView.FindProperty("returnButtonLabel").objectReferenceValue = returnButton.GetComponentInChildren<TMP_Text>();
        serializedResultView.FindProperty("waitingHostLabel").objectReferenceValue = waitingLabel;
        serializedResultView.FindProperty("successfulActionsLabel").stringValue = "Aciertos";
        serializedResultView.FindProperty("failedActionsLabel").stringValue = "Fallos";
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();
        return resultView;
    }

    private static string BuildCsvTemplate()
    {
        return
            "plantId,scientificName,imagePath,correctCategory\n" +
            "lavandula_dentata,Lavandula dentata,Plants/lavandula-dentata.png,Decoracion\n" +
            "jasminum_officinale,Jasminum officinale,Plants/jasminum-officinale.png,Decoracion\n" +
            "rosa_damascena,Rosa damascena,Plants/rosa-damascena.png,Decoracion\n" +
            "pelargonium_graveolens,Pelargonium graveolens,Plants/pelargonium-graveolens.png,Decoracion\n" +
            "mentha_spicata,Mentha spicata,Plants/mentha-spicata.png,Alimentacion\n" +
            "ocimum_basilicum,Ocimum basilicum,Plants/ocimum-basilicum.png,Alimentacion\n" +
            "foeniculum_vulgare,Foeniculum vulgare,Plants/foeniculum-vulgare.png,Alimentacion\n" +
            "laurus_nobilis,Laurus nobilis,Plants/laurus-nobilis.png,Alimentacion\n" +
            "aloe_vera,Aloe vera,Plants/aloe-vera.png,Curacion\n" +
            "matricaria_chamomilla,Matricaria chamomilla,Plants/matricaria-chamomilla.png,Curacion\n" +
            "salvia_officinalis,Salvia officinalis,Plants/salvia-officinalis.png,Curacion\n" +
            "thymus_vulgaris,Thymus vulgaris,Plants/thymus-vulgaris.png,Curacion\n";
    }

    private static Canvas CreateCanvas(string name)
    {
        var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void EnsureInputSystemUiEventSystem()
    {
        var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            UnityEngine.Object.DestroyImmediate(standaloneInputModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static ScrollViewReferences CreateScrollView(string name, Transform parent)
    {
        var root = CreateUiObject(name, parent, typeof(Image), typeof(ScrollRect));
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

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

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = CreateUiObject(name, parent, typeof(Image));
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        var objectComponents = new Type[components.Length + 1];
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

    private static TMP_Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment)
    {
        var textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = ConvertAlignment(alignment);
        text.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.TopLeft
        };
    }

    private static GameObject CreateButton(string name, Transform parent, Font font, string label, int fontSize, Color backgroundColor)
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

        var labelText = CreateText("Label", buttonObject.transform, font, label, fontSize, TextAnchor.MiddleCenter);
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.zero);
        labelText.color = Color.white;
        return buttonObject;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private readonly struct ScrollViewReferences
    {
        public ScrollViewReferences(GameObject root, GameObject contentRoot, VerticalLayoutGroup contentVerticalLayout)
        {
            Root = root;
            ContentRoot = contentRoot;
            ContentVerticalLayout = contentVerticalLayout;
        }

        public GameObject Root { get; }
        public GameObject ContentRoot { get; }
        public VerticalLayoutGroup ContentVerticalLayout { get; }
    }

    private readonly struct DropZoneReferences
    {
        public DropZoneReferences(GameObject root, GardenSmellTaxonomyDropZoneView dropZoneView)
        {
            Root = root;
            DropZoneView = dropZoneView;
        }

        public GameObject Root { get; }
        public GardenSmellTaxonomyDropZoneView DropZoneView { get; }
    }
}
