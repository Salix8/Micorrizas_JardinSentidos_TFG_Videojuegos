using System;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.DistributedPairs;

public static class DistributedPairsMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string SceneFolder = "Assets/Scenes";
    private const string TutorialConfigPath = ConfigFolder + "/DistributedPairsTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/DistributedPairsMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string CardPrefabPath = PrefabFolder + "/DistributedPairsCardView.prefab";
    private const string MinigameScenePath = SceneFolder + "/DistributedPairsMinigame.unity";
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string MainMapScenePath = "Assets/Scenes/UJI.unity";
    private const string MinigameSceneName = "DistributedPairsMinigame";

    [MenuItem("Tools/Coop/Setup Distributed Pairs Minigame")]
    public static void SetupDistributedPairsMinigame()
    {
        EnsureFolders();

        var tutorialContent = CreateOrUpdateTutorialContent();
        var minigameConfig = CreateOrUpdateMinigameConfig(tutorialContent);
        var catalogConfig = CreateOrUpdateCatalogConfig();
        var cardPrefab = CreateOrUpdateCardPrefab();

        SetupDistributedPairsScene(minigameConfig, cardPrefab);
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
        EnsureFolder(RootFolder, "Prefabs");
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
        serializedObject.FindProperty("title").stringValue = "Parejas distribuidas";
        serializedObject.FindProperty("subtitle").stringValue = "Coordina tu informacion con el resto";
        serializedObject.FindProperty("bodyText").stringValue =
            "Cada dispositivo solo puede revelar una carta a la vez.\n\n" +
            "Cuando dos jugadores revelan la pareja correcta en dispositivos distintos, ambas cartas se descartan y se repone la mano si el mazo lo permite.\n\n" +
            "Puedes cerrar este popup con la cruz o tocando fuera. Edita este asset para anadir texto, imagenes, video o contenido personalizado.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static DistributedPairsMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<DistributedPairsMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<DistributedPairsMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Parejas distribuidas";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Lo habeis conseguido";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("cardsPerDevice").intValue = 4;
        serializedObject.FindProperty("pairsToUse").intValue = 10;
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("maxColumns").intValue = 2;

        var pairDefinitions = serializedObject.FindProperty("pairDefinitions");
        pairDefinitions.arraySize = 10;
        for (var index = 0; index < pairDefinitions.arraySize; index++)
        {
            var element = pairDefinitions.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("title").stringValue = $"Pareja {index + 1}";
            element.FindPropertyRelative("description").stringValue = "Placeholder editable desde el asset del minijuego.";
            element.FindPropertyRelative("faceColor").colorValue = Color.HSVToRGB(index / 10f, 0.35f, 0.92f);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        return CoopMinigameSetupEditorUtility.UpsertCatalogEntry(
            CatalogConfigPath,
            4,
            "Minijuego 4 - Parejas distribuidas",
            "Minijuego cooperativo con informacion parcial repartida entre dispositivos.",
            MinigameSceneName);
    }

    private static DistributedPairsCardView CreateOrUpdateCardPrefab()
    {
        var font = GetBuiltinFont();
        var root = CreateUiObject("DistributedPairsCard", null, typeof(Image), typeof(Button), typeof(LayoutElement), typeof(DistributedPairsCardView));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(200f, 280f);

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.84f, 0.9f, 0.91f, 1f);
        var layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 200f;
        layoutElement.preferredHeight = 280f;

        var button = root.GetComponent<Button>();
        button.targetGraphic = rootImage;

        var frontFace = CreateUiObject("FrontFace", root.transform, typeof(Image));
        Stretch(frontFace.GetComponent<RectTransform>(), new Vector2(10f, 10f), new Vector2(-10f, -10f));
        var frontBackground = frontFace.GetComponent<Image>();
        frontBackground.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        var frontLayout = frontFace.AddComponent<VerticalLayoutGroup>();
        frontLayout.padding = new RectOffset(16, 16, 16, 16);
        frontLayout.spacing = 12f;
        frontLayout.childAlignment = TextAnchor.UpperCenter;
        frontLayout.childControlWidth = true;
        frontLayout.childControlHeight = false;
        frontLayout.childForceExpandHeight = false;
        frontLayout.childForceExpandWidth = true;

        var illustration = CreateUiObject("Illustration", frontFace.transform, typeof(Image), typeof(LayoutElement));
        var illustrationLayout = illustration.GetComponent<LayoutElement>();
        illustrationLayout.preferredHeight = 110f;
        var title = CreateText("Title", frontFace.transform, font, "Carta", 28, TextAnchor.MiddleCenter);
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 42f;
        var description = CreateText("Description", frontFace.transform, font, "Descripcion", 18, TextAnchor.UpperCenter);
        var descriptionLayout = description.gameObject.AddComponent<LayoutElement>();
        descriptionLayout.flexibleHeight = 1f;

        var backFace = CreateUiObject("BackFace", root.transform, typeof(Image));
        Stretch(backFace.GetComponent<RectTransform>(), new Vector2(10f, 10f), new Vector2(-10f, -10f));
        var backBackground = backFace.GetComponent<Image>();
        backBackground.color = new Color(0.16f, 0.29f, 0.35f, 1f);
        var backLabel = CreateText("BackLabel", backFace.transform, font, "Carta 1", 28, TextAnchor.MiddleCenter);
        Stretch(backLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var cardView = root.GetComponent<DistributedPairsCardView>();
        var serializedCardView = new SerializedObject(cardView);
        serializedCardView.FindProperty("selectionButton").objectReferenceValue = button;
        serializedCardView.FindProperty("frameImage").objectReferenceValue = rootImage;
        serializedCardView.FindProperty("frontFaceRoot").objectReferenceValue = frontFace;
        serializedCardView.FindProperty("frontFaceBackground").objectReferenceValue = frontBackground;
        serializedCardView.FindProperty("illustrationImage").objectReferenceValue = illustration.GetComponent<Image>();
        serializedCardView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedCardView.FindProperty("descriptionLabel").objectReferenceValue = description;
        serializedCardView.FindProperty("backFaceRoot").objectReferenceValue = backFace;
        serializedCardView.FindProperty("backFaceBackground").objectReferenceValue = backBackground;
        serializedCardView.FindProperty("backFaceLabel").objectReferenceValue = backLabel;
        serializedCardView.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<DistributedPairsCardView>(CardPrefabPath);
    }

    private static void SetupDistributedPairsScene(DistributedPairsMinigameConfig config, DistributedPairsCardView cardPrefab)
    {
        var font = GetBuiltinFont();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateEventSystemIfMissing();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, new Color(0.94f, 0.97f, 0.93f, 1f));

        var sessionObject = new GameObject("DistributedPairsSession", typeof(NetworkObject), typeof(DistributedPairsMinigameSession));
        var session = sessionObject.GetComponent<DistributedPairsMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("distributedPairsMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("DistributedPairsCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = new Color(0.94f, 0.97f, 0.93f, 1f);

        var uiRoot = CreateUiObject("DistributedPairsUI", safeAreaRoot.transform, typeof(DistributedPairsMinigameUIController));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        var waitingRect = waitingPanel.GetComponent<RectTransform>();
        waitingRect.anchorMin = new Vector2(0.5f, 0.5f);
        waitingRect.anchorMax = new Vector2(0.5f, 0.5f);
        waitingRect.anchoredPosition = Vector2.zero;
        var waitingResponsiveLayout = waitingPanel.AddComponent<ResponsivePanelLayoutController>();
        waitingResponsiveLayout.Configure(
            canvas.GetComponent<RectTransform>(),
            0.82f,
            0.18f,
            new Vector2(280f, 180f),
            new Vector2(720f, 260f),
            new Vector2(32f, 32f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, font, "Esperando al resto del grupo.", 28, TextAnchor.MiddleCenter);
        Stretch(waitingStatus.GetComponent<RectTransform>(), new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(36f, 36f), new Vector2(-36f, -36f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(24, 24, 24, 24);
        gameplayLayout.spacing = 16f;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childForceExpandHeight = false;
        gameplayLayout.childForceExpandWidth = true;
        gameplayPanel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, font, config.DisplayName, 42, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var statusPanel = CreatePanel("StatusPanel", gameplayPanel.transform, new Color(1f, 1f, 1f, 0.72f));
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(24, 24, 20, 20);
        statusLayout.spacing = 10f;
        statusLayout.childControlHeight = true;
        statusLayout.childControlWidth = true;
        statusLayout.childForceExpandHeight = false;
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 320f;

        var progressLabel = CreateText("ProgressLabel", statusPanel.transform, font, "Parejas: 0/0   Errores: 0", 24, TextAnchor.MiddleLeft);
        progressLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var sharedStatusLabel = CreateText("SharedStatusLabel", statusPanel.transform, font, "Selecciona una carta y comunicate con el resto del grupo.", 22, TextAnchor.UpperLeft);
        sharedStatusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        var localSelectionLabel = CreateText("LocalSelectionLabel", statusPanel.transform, font, "Solo puedes tener una carta activa.", 20, TextAnchor.UpperLeft);
        localSelectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var pileHudRow = CreateUiObject("PileHudRow", statusPanel.transform, typeof(HorizontalLayoutGroup));
        pileHudRow.AddComponent<LayoutElement>().preferredHeight = 116f;
        var pileHudLayout = pileHudRow.GetComponent<HorizontalLayoutGroup>();
        pileHudLayout.spacing = 12f;
        pileHudLayout.childControlWidth = true;
        pileHudLayout.childControlHeight = true;
        pileHudLayout.childForceExpandWidth = true;
        pileHudLayout.childForceExpandHeight = false;
        var deckPanel = CreatePileHudPanel("DeckPanel", pileHudRow.transform, font, "Mazo");
        var discardPanel = CreatePileHudPanel("DiscardPanel", pileHudRow.transform, font, "Descartes");
        var drawPileAnchor = deckPanel.transform.Find("CardAnchor") as RectTransform;
        var drawPileCountLabel = deckPanel.transform.Find("CountLabel")?.GetComponent<Text>();
        var discardPileCountLabel = discardPanel.transform.Find("CountLabel")?.GetComponent<Text>();

        var handPanel = CreatePanel("HandPanel", gameplayPanel.transform, new Color(1f, 1f, 1f, 0.72f));
        var handLayoutElement = handPanel.AddComponent<LayoutElement>();
        handLayoutElement.flexibleHeight = 1f;
        handLayoutElement.minHeight = 360f;

        var handGrid = CreateUiObject("HandGrid", handPanel.transform, typeof(GridLayoutGroup), typeof(ResponsiveGridLayoutController), typeof(DistributedPairsHandView));
        Stretch(handGrid.GetComponent<RectTransform>(), new Vector2(16f, 16f), new Vector2(-16f, -16f));
        var gridLayout = handGrid.GetComponent<GridLayoutGroup>();
        gridLayout.spacing = new Vector2(14f, 14f);
        gridLayout.padding = new RectOffset(8, 8, 8, 8);
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        CreateSceneVisibleHandSlots(handGrid.transform, config.CardsPerDevice);

        var handView = handGrid.GetComponent<DistributedPairsHandView>();
        var serializedHandView = new SerializedObject(handView);
        serializedHandView.FindProperty("cardRoot").objectReferenceValue = handGrid.transform;
        serializedHandView.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
        serializedHandView.FindProperty("responsiveGridLayoutController").objectReferenceValue = handGrid.GetComponent<ResponsiveGridLayoutController>();
        serializedHandView.ApplyModifiedPropertiesWithoutUndo();

        var mismatchOverlay = CreateUiObject("MismatchResetOverlay", uiRoot.transform, typeof(Image), typeof(Button));
        Stretch(mismatchOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        mismatchOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);
        var mismatchResetButton = mismatchOverlay.GetComponent<Button>();
        mismatchResetButton.targetGraphic = mismatchOverlay.GetComponent<Image>();
        mismatchOverlay.SetActive(false);
        var mismatchResetLabel = CreateText("Label", mismatchOverlay.transform, font, "No coinciden. Toca para girarlas de nuevo.", 26, TextAnchor.MiddleCenter);
        mismatchResetLabel.color = Color.white;
        Stretch(mismatchResetLabel.GetComponent<RectTransform>(), new Vector2(32f, 48f), new Vector2(-32f, -48f));

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);

        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<DistributedPairsMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("distributedPairsMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("localHandView").objectReferenceValue = handView;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("progressLabel").objectReferenceValue = progressLabel;
        serializedUiController.FindProperty("sharedStatusLabel").objectReferenceValue = sharedStatusLabel;
        serializedUiController.FindProperty("localSelectionLabel").objectReferenceValue = localSelectionLabel;
        serializedUiController.FindProperty("drawPileAnchor").objectReferenceValue = drawPileAnchor;
        serializedUiController.FindProperty("drawPileCountLabel").objectReferenceValue = drawPileCountLabel;
        serializedUiController.FindProperty("discardPileCountLabel").objectReferenceValue = discardPileCountLabel;
        serializedUiController.FindProperty("mismatchResetButton").objectReferenceValue = mismatchResetButton;
        serializedUiController.FindProperty("mismatchResetLabel").objectReferenceValue = mismatchResetLabel;
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
            throw new InvalidOperationException("Lobby scene requires a CoopSessionCoordinator.");
        }

        var serializedCoordinator = new SerializedObject(coordinator);
        serializedCoordinator.FindProperty("persistAcrossScenes").boolValue = true;

        var miniGameSceneNames = serializedCoordinator.FindProperty("miniGameSceneNames");
        miniGameSceneNames.arraySize = Math.Max(5, miniGameSceneNames.arraySize);
        miniGameSceneNames.GetArrayElementAtIndex(4).stringValue = MinigameSceneName;
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

            if (!exists)
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
        var backgroundButton = dismissButton.GetComponent<Button>();

        var contentPanel = CreatePanel("ContentPanel", popupRoot.transform, new Color(0.95f, 0.97f, 0.94f, 1f));
        var contentRect = contentPanel.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        var responsiveLayout = contentPanel.AddComponent<ResponsivePanelLayoutController>();
        responsiveLayout.Configure(
            popupRoot.GetComponent<RectTransform>(),
            0.9f,
            0.86f,
            new Vector2(320f, 460f),
            new Vector2(840f, 1160f),
            new Vector2(32f, 32f));

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
        serializedController.FindProperty("backgroundDismissButton").objectReferenceValue = backgroundButton;
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
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        var responsiveLayout = contentPanel.AddComponent<ResponsivePanelLayoutController>();
        responsiveLayout.Configure(
            popupRoot.GetComponent<RectTransform>(),
            0.86f,
            0.42f,
            new Vector2(320f, 320f),
            new Vector2(720f, 560f),
            new Vector2(32f, 32f));

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText("TitleLabel", contentPanel.transform, font, "Lo habeis conseguido", 36, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        var score = CreateText("ScoreLabel", contentPanel.transform, font, "10/10", 52, TextAnchor.MiddleCenter);
        score.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;
        var summary = CreateText("SummaryLabel", contentPanel.transform, font, "Parejas acertadas: 0", 24, TextAnchor.MiddleCenter);
        summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
        var returnButton = CreateButton("ReturnButton", contentPanel.transform, font, "Volver al mapa", 22);
        returnButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var waitingLabel = CreateText("WaitingHostLabel", contentPanel.transform, font, "Esperando a que el host vuelva al mapa.", 22, TextAnchor.MiddleCenter);
        waitingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        var returnButtonLabel = returnButton.GetComponentInChildren<Text>();

        var resultView = popupRoot.GetComponent<MinigameResultView>();
        var serializedResultView = new SerializedObject(resultView);
        serializedResultView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedResultView.FindProperty("scoreLabel").objectReferenceValue = score;
        serializedResultView.FindProperty("summaryLabel").objectReferenceValue = summary;
        serializedResultView.FindProperty("returnButton").objectReferenceValue = returnButton.GetComponent<Button>();
        serializedResultView.FindProperty("returnButtonLabel").objectReferenceValue = returnButtonLabel;
        serializedResultView.FindProperty("waitingHostLabel").objectReferenceValue = waitingLabel;
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();

        return resultView;
    }

    private static CoopMinigameLauncherEntryView CreateLauncherEntryTemplate(Transform parent, Font font)
    {
        var entryRoot = CreatePanel("EntryTemplate", parent, new Color(1f, 1f, 1f, 0.12f));
        var layout = entryRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText("TitleLabel", entryRoot.transform, font, "Minijuego", 24, TextAnchor.MiddleLeft);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var description = CreateText("DescriptionLabel", entryRoot.transform, font, "Descripcion", 18, TextAnchor.UpperLeft);
        description.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var button = CreateButton("LaunchButton", entryRoot.transform, font, "Lanzar", 20);
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        var entryView = entryRoot.AddComponent<CoopMinigameLauncherEntryView>();
        var serializedEntryView = new SerializedObject(entryView);
        serializedEntryView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedEntryView.FindProperty("descriptionLabel").objectReferenceValue = description;
        serializedEntryView.FindProperty("launchButton").objectReferenceValue = button.GetComponent<Button>();
        serializedEntryView.ApplyModifiedPropertiesWithoutUndo();

        return entryView;
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

    private static Font GetBuiltinFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void CreateSceneVisibleHandSlots(Transform cardRoot, int slotCount)
    {
        var cardPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        if (cardPrefabRoot == null)
        {
            throw new InvalidOperationException($"Card prefab not found at path '{CardPrefabPath}'.");
        }

        for (var index = 0; index < slotCount; index++)
        {
            var slotObject = new GameObject($"HandSlot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObject.transform.SetParent(cardRoot, false);
            slotObject.layer = cardRoot.gameObject.layer;

            var slotBackground = slotObject.GetComponent<Image>();
            slotBackground.color = new Color(1f, 1f, 1f, 0.08f);
            slotBackground.raycastTarget = false;

            var layoutElement = slotObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;

            var cardInstance = PrefabUtility.InstantiatePrefab(cardPrefabRoot) as GameObject;
            if (cardInstance == null)
            {
                cardInstance = UnityEngine.Object.Instantiate(cardPrefabRoot);
            }

            cardInstance.transform.SetParent(slotObject.transform, false);
            cardInstance.name = "CardView";
            var cardRectTransform = cardInstance.GetComponent<RectTransform>();
            Stretch(cardRectTransform, Vector2.zero, Vector2.zero);
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

    private static GameObject CreatePileHudPanel(string name, Transform parent, Font font, string title)
    {
        var panel = CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.78f));
        panel.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var cardAnchor = CreateUiObject("CardAnchor", panel.transform, typeof(Image));
        cardAnchor.AddComponent<LayoutElement>().preferredHeight = 64f;
        var cardAnchorImage = cardAnchor.GetComponent<Image>();
        cardAnchorImage.color = new Color(0.16f, 0.29f, 0.35f, 1f);
        cardAnchorImage.raycastTarget = false;

        var titleLabel = CreateText("TitleLabel", panel.transform, font, title, 18, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var countLabel = CreateText("CountLabel", panel.transform, font, $"{title}\n0", 20, TextAnchor.MiddleCenter);
        countLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
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

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.52f, 0.56f, 1f);
        colors.pressedColor = new Color(0.16f, 0.32f, 0.35f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

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
