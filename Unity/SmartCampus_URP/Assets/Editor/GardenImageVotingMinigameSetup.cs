using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.GardenImageVoting;

public static class GardenImageVotingMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string SceneFolder = "Assets/Scenes";
    private const string StreamingAssetsFolder = "Assets/StreamingAssets/CoopMinigames";
    private const string TutorialConfigPath = ConfigFolder + "/GardenImageVotingTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/GardenImageVotingMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string CsvTemplatePath = StreamingAssetsFolder + "/01-GardenImagenVotingCards/GardenImageVotingCards.csv";
    private const string MinigameScenePath = SceneFolder + "/GardenImageVotingMinigame.unity";
    private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
    private const string MainMapScenePath = SceneFolder + "/UJI.unity";
    private const string MinigameSceneName = "GardenImageVotingMinigame";
    private const string DistributedPairsSceneName = "DistributedPairsMinigame";

    [MenuItem("Tools/Coop/Setup Garden Image Voting Minigame")]
    public static void SetupGardenImageVotingMinigame()
    {
        EnsureFolders();

        var tutorialContent = CreateOrUpdateTutorialContent();
        var minigameConfig = CreateOrUpdateMinigameConfig(tutorialContent);
        var catalogConfig = CreateOrUpdateCatalogConfig();
        CreateCsvTemplateIfMissing();

        SetupGardenImageVotingScene(minigameConfig);
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
        EnsureFolder("Assets/StreamingAssets/CoopMinigames", "01-GardenImagenVotingCards");
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
        serializedObject.FindProperty("title").stringValue = "Imagenes del jardin";
        serializedObject.FindProperty("subtitle").stringValue = "Decide con un gesto rapido";
        serializedObject.FindProperty("bodyText").stringValue =
            "Cada dispositivo recibe una secuencia propia de imagenes relacionadas entre si.\n\n" +
            "Desliza la tarjeta a la derecha si crees que esa imagen si aparece en el jardin, y a la izquierda si crees que no.\n\n" +
            "Los aciertos suman puntos compartidos para todo el lobby. La partida termina cuando se acaba el tiempo o cuando todos completan sus imagenes.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GardenImageVotingMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GardenImageVotingMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<GardenImageVotingMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Minijuego 1 - Imagenes del jardin";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Secuencia completada";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("csvRelativePath").stringValue = "CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv";
        serializedObject.FindProperty("cardsPerDevice").intValue = 5;
        serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
        serializedObject.FindProperty("allowRepeatedImagesAcrossDevices").boolValue = true;
        serializedObject.FindProperty("timeLimitSeconds").floatValue = 300f;
        serializedObject.FindProperty("swipeThreshold").floatValue = 120f;
        serializedObject.FindProperty("transitionDuration").floatValue = 0.22f;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        return CoopMinigameSetupEditorUtility.UpsertCatalogEntry(
            CatalogConfigPath,
            1,
            "Minijuego 1 - Imagenes del jardin",
            "Cada dispositivo decide con un gesto si una imagen pertenece o no al jardin. La puntuacion se comparte entre todo el grupo.",
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

    private static void SetupGardenImageVotingScene(GardenImageVotingMinigameConfig config)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateEventSystemIfMissing();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, new Color(0.92f, 0.96f, 0.9f, 1f));

        var sessionObject = new GameObject("GardenImageVotingSession", typeof(Unity.Netcode.NetworkObject), typeof(GardenImageVotingMinigameSession));
        var session = sessionObject.GetComponent<GardenImageVotingMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("gardenImageVotingMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("GardenImageVotingCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = new Color(0.92f, 0.96f, 0.9f, 1f);

        var uiRoot = CreateUiObject("GardenImageVotingUI", safeAreaRoot.transform, typeof(GardenImageVotingMinigameUIController));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        var waitingResponsiveLayout = waitingPanel.AddComponent<ResponsivePanelLayoutController>();
        waitingResponsiveLayout.Configure(canvas.GetComponent<RectTransform>(), 0.8f, 0.18f, new Vector2(280f, 180f), new Vector2(700f, 260f), new Vector2(32f, 32f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, font, "Esperando al resto del grupo.", 28, TextAnchor.MiddleCenter);
        Stretch(waitingStatus.GetComponent<RectTransform>(), new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(40f, 40f), new Vector2(-40f, -40f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(28, 28, 28, 28);
        gameplayLayout.spacing = 16f;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childForceExpandHeight = false;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, font, config.DisplayName, 42, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var statusPanel = CreatePanel("StatusPanel", gameplayPanel.transform, new Color(1f, 1f, 1f, 0.75f));
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 220f;
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(24, 24, 20, 20);
        statusLayout.spacing = 10f;
        statusLayout.childControlHeight = true;
        statusLayout.childControlWidth = true;
        statusLayout.childForceExpandHeight = false;

        var timerLabel = CreateText("TimerLabel", statusPanel.transform, font, "Tiempo restante: 05:00", 24, TextAnchor.MiddleLeft);
        timerLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var scoreLabel = CreateText("ScoreLabel", statusPanel.transform, font, "Puntos compartidos: 0", 24, TextAnchor.MiddleLeft);
        scoreLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var progressLabel = CreateText("ProgressLabel", statusPanel.transform, font, "Respondidas: 0/0", 24, TextAnchor.MiddleLeft);
        progressLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var statusLabel = CreateText("StatusLabel", statusPanel.transform, font, "Desliza a la derecha si la has visto y a la izquierda si no.", 20, TextAnchor.UpperLeft);
        statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var cardPanel = CreatePanel("CardPanel", gameplayPanel.transform, new Color(1f, 1f, 1f, 0.88f));
        var cardPanelLayout = cardPanel.AddComponent<LayoutElement>();
        cardPanelLayout.flexibleHeight = 1f;
        cardPanelLayout.minHeight = 360f;

        var cardRoot = CreateUiObject("CardRoot", cardPanel.transform, typeof(Image), typeof(CanvasGroup), typeof(GardenImageVotingCardView), typeof(ResponsiveAspectRatioLayoutController));
        var cardRootRect = cardRoot.GetComponent<RectTransform>();
        cardRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRootRect.pivot = new Vector2(0.5f, 0.5f);
        cardRootRect.sizeDelta = new Vector2(640f, 920f);
        cardRoot.GetComponent<Image>().color = config.CardVisualSettings.BackgroundColor;
        cardRoot.GetComponent<ResponsiveAspectRatioLayoutController>().Configure(
            cardPanel.GetComponent<RectTransform>(),
            640f / 920f,
            new Vector2(220f, 340f),
            new Vector2(680f, 980f),
            new Vector2(24f, 24f));

        var illustration = CreateUiObject("Illustration", cardRoot.transform, typeof(Image));
        var illustrationRect = illustration.GetComponent<RectTransform>();
        illustrationRect.anchorMin = new Vector2(0.08f, 0.46f);
        illustrationRect.anchorMax = new Vector2(0.92f, 0.9f);
        illustrationRect.offsetMin = Vector2.zero;
        illustrationRect.offsetMax = Vector2.zero;
        illustration.GetComponent<Image>().preserveAspect = true;

        var illustrationPlaceholder = CreatePanel("IllustrationPlaceholder", illustration.transform, config.CardVisualSettings.PlaceholderColor);
        Stretch(illustrationPlaceholder.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        var illustrationPlaceholderLabel = CreateText("IllustrationPlaceholderLabel", illustrationPlaceholder.transform, font, "Imagen pendiente", 30, TextAnchor.MiddleCenter);
        Stretch(illustrationPlaceholderLabel.GetComponent<RectTransform>(), new Vector2(16f, 16f), new Vector2(-16f, -16f));

        var topicLabel = CreateText("TopicLabel", cardRoot.transform, font, "Tema", 30, TextAnchor.MiddleCenter);
        var topicRect = topicLabel.GetComponent<RectTransform>();
        topicRect.anchorMin = new Vector2(0.08f, 0.33f);
        topicRect.anchorMax = new Vector2(0.92f, 0.39f);
        topicRect.offsetMin = Vector2.zero;
        topicRect.offsetMax = Vector2.zero;

        var cardTitleLabel = CreateText("CardTitleLabel", cardRoot.transform, font, "Detalle", 42, TextAnchor.MiddleCenter);
        var cardTitleRect = cardTitleLabel.GetComponent<RectTransform>();
        cardTitleRect.anchorMin = new Vector2(0.08f, 0.24f);
        cardTitleRect.anchorMax = new Vector2(0.92f, 0.32f);
        cardTitleRect.offsetMin = Vector2.zero;
        cardTitleRect.offsetMax = Vector2.zero;

        var bodyLabel = CreateText("BodyLabel", cardRoot.transform, font, "Desliza a la derecha si la has visto en el jardin o a la izquierda si no la has visto.", 24, TextAnchor.UpperCenter);
        var bodyRect = bodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.08f, 0.1f);
        bodyRect.anchorMax = new Vector2(0.92f, 0.21f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var decisionHintLabel = CreateText("DecisionHintLabel", cardRoot.transform, font, "Arrastra para responder", 26, TextAnchor.MiddleCenter);
        var decisionHintRect = decisionHintLabel.GetComponent<RectTransform>();
        decisionHintRect.anchorMin = new Vector2(0.08f, 0.03f);
        decisionHintRect.anchorMax = new Vector2(0.92f, 0.08f);
        decisionHintRect.offsetMin = Vector2.zero;
        decisionHintRect.offsetMax = Vector2.zero;

        var completionLabel = CreateText("CompletionLabel", gameplayPanel.transform, font, "Has terminado tus imagenes.", 20, TextAnchor.MiddleCenter);
        completionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        completionLabel.gameObject.SetActive(false);

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);

        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);

        var cardView = cardRoot.GetComponent<GardenImageVotingCardView>();
        var serializedCardView = new SerializedObject(cardView);
        serializedCardView.FindProperty("cardTransform").objectReferenceValue = cardRootRect;
        serializedCardView.FindProperty("canvasGroup").objectReferenceValue = cardRoot.GetComponent<CanvasGroup>();
        serializedCardView.FindProperty("frameImage").objectReferenceValue = cardRoot.GetComponent<Image>();
        serializedCardView.FindProperty("illustrationImage").objectReferenceValue = illustration.GetComponent<Image>();
        serializedCardView.FindProperty("illustrationPlaceholderRoot").objectReferenceValue = illustrationPlaceholder;
        serializedCardView.FindProperty("topicLabel").objectReferenceValue = topicLabel;
        serializedCardView.FindProperty("titleLabel").objectReferenceValue = cardTitleLabel;
        serializedCardView.FindProperty("bodyLabel").objectReferenceValue = bodyLabel;
        serializedCardView.FindProperty("decisionHintLabel").objectReferenceValue = decisionHintLabel;
        serializedCardView.ApplyModifiedPropertiesWithoutUndo();

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<GardenImageVotingMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("gardenImageVotingMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("cardView").objectReferenceValue = cardView;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("scoreLabel").objectReferenceValue = scoreLabel;
        serializedUiController.FindProperty("progressLabel").objectReferenceValue = progressLabel;
        serializedUiController.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedUiController.FindProperty("completionLabel").objectReferenceValue = completionLabel;
        serializedUiController.ApplyModifiedPropertiesWithoutUndo();

        FantasyWoodenThemeUtility.ApplyThemeToOpenScene(scene);
        EditorSceneManager.SaveScene(scene, MinigameScenePath);
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
        miniGameSceneNames.arraySize = Math.Max(5, miniGameSceneNames.arraySize);
        miniGameSceneNames.GetArrayElementAtIndex(1).stringValue = MinigameSceneName;
        miniGameSceneNames.GetArrayElementAtIndex(4).stringValue = DistributedPairsSceneName;
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
            MinigameScenePath,
            SceneFolder + "/" + DistributedPairsSceneName + ".unity"
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
        var popupRoot = CreateUiObject("TutorialPopup", parent, typeof(TutorialPopupController));
        Stretch(popupRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var dismissButton = CreateUiObject("DismissBackground", popupRoot.transform, typeof(Image), typeof(Button));
        Stretch(dismissButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        dismissButton.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

        var contentPanel = CreatePanel("ContentPanel", popupRoot.transform, new Color(0.95f, 0.97f, 0.94f, 1f));
        var contentRect = contentPanel.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.9f, 0.86f, new Vector2(320f, 460f), new Vector2(840f, 1160f), new Vector2(32f, 32f));

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var closeButton = CreateButton("CloseButton", contentPanel.transform, font, "X", 20);
        closeButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        var scrollView = CreateScrollView("ContentScrollView", contentPanel.transform);
        var scrollLayout = scrollView.Root.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 220f;

        var title = CreateText("TitleLabel", scrollView.ContentRoot.transform, font, "Tutorial", 36, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        var subtitle = CreateText("SubtitleLabel", scrollView.ContentRoot.transform, font, "Subtitulo", 24, TextAnchor.MiddleCenter);
        subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        var body = CreateText("BodyLabel", scrollView.ContentRoot.transform, font, "Contenido del tutorial", 22, TextAnchor.UpperLeft);
        body.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
        var illustration = CreateUiObject("Illustration", scrollView.ContentRoot.transform, typeof(Image));
        illustration.AddComponent<LayoutElement>().preferredHeight = 220f;
        var videoSurface = CreateUiObject("VideoSurface", scrollView.ContentRoot.transform, typeof(RawImage));
        videoSurface.AddComponent<LayoutElement>().preferredHeight = 220f;
        var customContentRoot = CreateUiObject("CustomContentRoot", scrollView.ContentRoot.transform, typeof(ContentSizeFitter));
        customContentRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var videoPlayer = popupRoot.AddComponent<UnityEngine.Video.VideoPlayer>();

        var controller = popupRoot.GetComponent<TutorialPopupController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("backgroundDismissButton").objectReferenceValue = dismissButton.GetComponent<Button>();
        serializedController.FindProperty("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
        serializedController.FindProperty("titleLabel").objectReferenceValue = title;
        serializedController.FindProperty("subtitleLabel").objectReferenceValue = subtitle;
        serializedController.FindProperty("bodyLabel").objectReferenceValue = body;
        serializedController.FindProperty("illustrationImage").objectReferenceValue = illustration.GetComponent<Image>();
        serializedController.FindProperty("videoSurface").objectReferenceValue = videoSurface.GetComponent<RawImage>();
        serializedController.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        serializedController.FindProperty("customContentRoot").objectReferenceValue = customContentRoot.transform;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }

    private static MinigameResultView CreateResultPopup(Transform parent, Font font)
    {
        var popupRoot = CreateUiObject("ResultPopup", parent, typeof(MinigameResultView));
        Stretch(popupRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", popupRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

        var contentPanel = CreatePanel("ContentPanel", popupRoot.transform, new Color(0.95f, 0.97f, 0.94f, 1f));
        var contentRect = contentPanel.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.86f, 0.42f, new Vector2(320f, 320f), new Vector2(720f, 560f), new Vector2(32f, 32f));

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
        var returnButton = CreateButton("ReturnButton", contentPanel.transform, font, "Volver al mapa", 22);
        returnButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var waitingLabel = CreateText("WaitingHostLabel", contentPanel.transform, font, "Esperando a que el host vuelva al mapa.", 22, TextAnchor.MiddleCenter);
        waitingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        var resultView = popupRoot.GetComponent<MinigameResultView>();
        var serializedResultView = new SerializedObject(resultView);
        serializedResultView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedResultView.FindProperty("scoreLabel").objectReferenceValue = score;
        serializedResultView.FindProperty("summaryLabel").objectReferenceValue = summary;
        serializedResultView.FindProperty("returnButton").objectReferenceValue = returnButton.GetComponent<Button>();
        serializedResultView.FindProperty("returnButtonLabel").objectReferenceValue = returnButton.GetComponentInChildren<Text>();
        serializedResultView.FindProperty("waitingHostLabel").objectReferenceValue = waitingLabel;
        serializedResultView.FindProperty("successfulActionsLabel").stringValue = "Aciertos";
        serializedResultView.FindProperty("failedActionsLabel").stringValue = "Fallos";
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();

        return resultView;
    }

    private static string BuildCsvTemplate()
    {
        var csv = "roundIndex,deviceSlot,topic,title,imagePath,isSeenInGarden\n";
        for (var roundIndex = 1; roundIndex <= 5; roundIndex++)
        {
            for (var deviceSlot = 1; deviceSlot <= 6; deviceSlot++)
            {
                csv += $"{roundIndex},{deviceSlot},Tema {roundIndex},Imagen {roundIndex}-{deviceSlot},,false\n";
            }
        }

        return csv;
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

    private static void CreateEventSystemIfMissing()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
        contentLayout.spacing = 16f;
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

        return new ScrollViewReferences(root, contentRoot);
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

    private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment)
    {
        var textObject = CreateUiObject(name, parent, typeof(Text));
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static GameObject CreateButton(string name, Transform parent, Font font, string label, int fontSize)
    {
        var buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.21f, 0.42f, 0.46f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var labelText = CreateText("Label", buttonObject.transform, font, label, fontSize, TextAnchor.MiddleCenter);
        Stretch(labelText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
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
        public ScrollViewReferences(GameObject root, GameObject contentRoot)
        {
            Root = root;
            ContentRoot = contentRoot;
        }

        public GameObject Root { get; }
        public GameObject ContentRoot { get; }
    }
}
