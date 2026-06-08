using System;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
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
    private const int MinigameIndex = 3;

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
        serializedObject.FindProperty("cardsPerDevice").intValue = 6;
        serializedObject.FindProperty("pairsToUse").intValue = 12;
        serializedObject.FindProperty("guaranteedVisiblePairsOffset").intValue = 1;
        serializedObject.FindProperty("timeLimitSeconds").floatValue = 180f;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("minColumns").intValue = 2;
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("maxColumns").intValue = 2;
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("minCardSize").vector2Value = new Vector2(140f, 180f);
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("maxCardSize").vector2Value = new Vector2(280f, 340f);
        serializedObject.FindProperty("cardVisualSettings").FindPropertyRelative("cardAspectRatio").floatValue = 0.82f;

        var pairDefinitions = serializedObject.FindProperty("pairDefinitions");
        pairDefinitions.arraySize = 12;
        for (var index = 0; index < pairDefinitions.arraySize; index++)
        {
            var element = pairDefinitions.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("title").stringValue = $"Pareja {index + 1}";
            element.FindPropertyRelative("flavorHint").stringValue = $"Sabor {index + 1}";
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
            MinigameIndex,
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
        var themeConfig = CoopMinigameThemeSetupUtility.GetOrCreateDefaultTheme();
        CoopMinigameSharedPanelPrefabUtility.CreateOrUpdateSharedPanelPrefabs();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateEventSystemIfMissing();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, themeConfig.Palette.ScreenBackground);

        var sessionObject = new GameObject("DistributedPairsSession", typeof(NetworkObject), typeof(DistributedPairsMinigameSession));
        var session = sessionObject.GetComponent<DistributedPairsMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("distributedPairsMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(session);

        var canvas = CreateCanvas("DistributedPairsCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = themeConfig.Palette.ScreenBackground;

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

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        var screenMargin = themeConfig.ScreenLayout.ScreenMargin;
        Stretch(gameplayPanel.GetComponent<RectTransform>(), screenMargin, -screenMargin);
        gameplayPanel.GetComponent<Image>().color = Color.clear;
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(0, 0, 0, 0);
        gameplayLayout.spacing = themeConfig.ScreenLayout.VerticalSpacing;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childForceExpandHeight = false;
        gameplayLayout.childForceExpandWidth = true;

        var topPanelView = CoopMinigameSharedPanelSetupUtility.InstantiateSharedPanel<CoopMinigameTopPanelView>(
            CoopMinigameSharedPanelPrefabUtility.TopPanelPrefabPath,
            gameplayPanel.transform,
            themeConfig.ScreenLayout.TopPanelHeight);
        topPanelView.SetTheme(themeConfig);
        topPanelView.Bind(config.DisplayName, 0f, string.Empty, string.Empty);

        TMP_Text titleLabel = null;

        var interactivePanel = CreatePanel("InteractivePanel", gameplayPanel.transform, themeConfig.Palette.PanelBackground);
        interactivePanel.AddComponent<LayoutElement>().flexibleHeight = 1f;
        interactivePanel.GetComponent<LayoutElement>().minHeight = 360f;
        var interactiveLayout = interactivePanel.AddComponent<VerticalLayoutGroup>();
        interactiveLayout.padding = new RectOffset(24, 24, 24, 24);
        interactiveLayout.spacing = 16f;
        interactiveLayout.childControlWidth = true;
        interactiveLayout.childControlHeight = true;
        interactiveLayout.childForceExpandHeight = false;

        var statusPanel = CreatePanel("StatusPanel", interactivePanel.transform, themeConfig.Palette.PanelBackground);
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(24, 24, 20, 20);
        statusLayout.spacing = 10f;
        statusLayout.childControlHeight = true;
        statusLayout.childControlWidth = true;
        statusLayout.childForceExpandHeight = false;
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 320f;

        var statusHeaderRow = CreateUiObject("StatusHeaderRow", statusPanel.transform, typeof(HorizontalLayoutGroup));
        statusHeaderRow.AddComponent<LayoutElement>().preferredHeight = 40f;
        var statusHeaderLayout = statusHeaderRow.GetComponent<HorizontalLayoutGroup>();
        statusHeaderLayout.spacing = 16f;
        statusHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
        statusHeaderLayout.childControlWidth = true;
        statusHeaderLayout.childControlHeight = true;
        statusHeaderLayout.childForceExpandWidth = false;
        statusHeaderLayout.childForceExpandHeight = false;

        var timerLabel = CreateText("TimerLabel", statusHeaderRow.transform, font, "Tiempo 03:00", 24, TextAnchor.MiddleLeft);
        var timerLayout = timerLabel.gameObject.AddComponent<LayoutElement>();
        timerLayout.preferredWidth = 180f;
        timerLayout.preferredHeight = 34f;

        var progressLabel = CreateText("ProgressLabel", statusHeaderRow.transform, font, "Parejas: 0/0   Errores: 0", 24, TextAnchor.MiddleLeft);
        var progressLayout = progressLabel.gameObject.AddComponent<LayoutElement>();
        progressLayout.flexibleWidth = 1f;
        progressLayout.preferredHeight = 34f;
        var sharedStatusLabel = CreateText("SharedStatusLabel", statusPanel.transform, font, "Seleccionad 2 cartas entre todos", 22, TextAnchor.UpperLeft);
        sharedStatusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        var localSelectionLabel = CreateText("LocalSelectionLabel", statusPanel.transform, font, string.Empty, 20, TextAnchor.UpperLeft);
        localSelectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        localSelectionLabel.gameObject.SetActive(false);

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
        var drawPileCountLabel = deckPanel.transform.Find("CountLabel")?.GetComponent<TMP_Text>();
        var discardPileCountLabel = discardPanel.transform.Find("CountLabel")?.GetComponent<TMP_Text>();

        var handPanel = CreatePanel("HandPanel", interactivePanel.transform, themeConfig.Palette.PanelBackground);
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
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = config.CardVisualSettings.MaxColumns;
        gridLayout.cellSize = config.CardVisualSettings.MaxCardSize;

        var responsiveGridLayoutController = handGrid.GetComponent<ResponsiveGridLayoutController>();
        var serializedResponsiveLayout = new SerializedObject(responsiveGridLayoutController);
        serializedResponsiveLayout.FindProperty("minColumns").intValue = config.CardVisualSettings.MinColumns;
        serializedResponsiveLayout.FindProperty("maxColumns").intValue = config.CardVisualSettings.MaxColumns;
        serializedResponsiveLayout.FindProperty("minCellSize").vector2Value = config.CardVisualSettings.MinCardSize;
        serializedResponsiveLayout.FindProperty("maxCellSize").vector2Value = config.CardVisualSettings.MaxCardSize;
        serializedResponsiveLayout.FindProperty("cardAspectRatio").floatValue = config.CardVisualSettings.CardAspectRatio;
        serializedResponsiveLayout.ApplyModifiedPropertiesWithoutUndo();

        CreateSceneVisibleHandSlots(handGrid.transform, config.CardsPerDevice);

        var handView = handGrid.GetComponent<DistributedPairsHandView>();
        var serializedHandView = new SerializedObject(handView);
        serializedHandView.FindProperty("cardRoot").objectReferenceValue = handGrid.transform;
        serializedHandView.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
        serializedHandView.FindProperty("responsiveGridLayoutController").objectReferenceValue = responsiveGridLayoutController;
        serializedHandView.ApplyModifiedPropertiesWithoutUndo();

        var bottomPanelView = CoopMinigameSharedPanelSetupUtility.InstantiateSharedPanel<CoopMinigameBottomPanelView>(
            CoopMinigameSharedPanelPrefabUtility.BottomPanelPrefabPath,
            gameplayPanel.transform,
            themeConfig.ScreenLayout.BottomPanelHeight);
        bottomPanelView.SetTheme(themeConfig);
        bottomPanelView.Bind(
            "ENCONTRAD LOS PARES",
            "En cada dispositivo hay varias cartas. Volved dos a la vez para encontrar los pares.",
            config.TimeLimitSeconds,
            config.TimeLimitSeconds,
            0f);

        var mismatchOverlay = CreateUiObject("MismatchResetOverlay", uiRoot.transform, typeof(Image), typeof(Button));
        Stretch(mismatchOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        mismatchOverlay.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.14f, 0.08f);
        var mismatchResetButton = mismatchOverlay.GetComponent<Button>();
        mismatchResetButton.targetGraphic = mismatchOverlay.GetComponent<Image>();
        mismatchResetButton.transition = Selectable.Transition.None;
        mismatchOverlay.SetActive(false);
        var mismatchResetLabel = CreateText("Label", mismatchOverlay.transform, font, "No coinciden. Toca cualquier parte para girarlas.", 22, TextAnchor.MiddleCenter);
        mismatchResetLabel.color = Color.white;
        var mismatchLabelRect = mismatchResetLabel.GetComponent<RectTransform>();
        mismatchLabelRect.anchorMin = new Vector2(0.5f, 0f);
        mismatchLabelRect.anchorMax = new Vector2(0.5f, 0f);
        mismatchLabelRect.pivot = new Vector2(0.5f, 0.5f);
        mismatchLabelRect.anchoredPosition = new Vector2(0f, 92f);
        mismatchLabelRect.sizeDelta = new Vector2(760f, 72f);

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);

        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);
        var failureFeedback = MinigameFailureFeedbackSetupUtility.CreateOrUpdateFailureFeedback(uiRoot, gameplayPanel);

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<DistributedPairsMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        MinigameFailureFeedbackSetupUtility.AssignToUiController(serializedUiController, failureFeedback);
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("distributedPairsMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("topPanelView").objectReferenceValue = topPanelView;
        serializedUiController.FindProperty("bottomPanelView").objectReferenceValue = bottomPanelView;
        serializedUiController.FindProperty("localHandView").objectReferenceValue = handView;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("progressLabel").objectReferenceValue = progressLabel;
        serializedUiController.FindProperty("sharedStatusLabel").objectReferenceValue = sharedStatusLabel;
        serializedUiController.FindProperty("localSelectionLabel").objectReferenceValue = localSelectionLabel;
        serializedUiController.FindProperty("defaultStatusMessage").stringValue = "Seleccionad 2 cartas entre todos";
        serializedUiController.FindProperty("selectedCardPrefix").stringValue = "Sabor a ";
        var successMessagesProperty = serializedUiController.FindProperty("successStatusMessages");
        successMessagesProperty.arraySize = 3;
        successMessagesProperty.GetArrayElementAtIndex(0).stringValue = "Sabor correcto";
        successMessagesProperty.GetArrayElementAtIndex(1).stringValue = "Eso es";
        successMessagesProperty.GetArrayElementAtIndex(2).stringValue = "Si esas dos van juntas";
        var failureMessagesProperty = serializedUiController.FindProperty("failureStatusMessages");
        failureMessagesProperty.arraySize = 3;
        failureMessagesProperty.GetArrayElementAtIndex(0).stringValue = "No, esas no son iguales";
        failureMessagesProperty.GetArrayElementAtIndex(1).stringValue = "Buen intento pero diferentes";
        failureMessagesProperty.GetArrayElementAtIndex(2).stringValue = "Casi, pero no";
        serializedUiController.FindProperty("drawPileAnchor").objectReferenceValue = drawPileAnchor;
        serializedUiController.FindProperty("drawPileCountLabel").objectReferenceValue = drawPileCountLabel;
        serializedUiController.FindProperty("discardPileCountLabel").objectReferenceValue = discardPileCountLabel;
        serializedUiController.FindProperty("mismatchResetButton").objectReferenceValue = mismatchResetButton;
        serializedUiController.FindProperty("mismatchResetLabel").objectReferenceValue = mismatchResetLabel;
        serializedUiController.ApplyModifiedPropertiesWithoutUndo();

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

            if (!exists)
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

        var returnButtonLabel = returnButton.GetComponentInChildren<TMP_Text>();

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

    private static TMP_Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment)
    {
        var textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = ConvertAlignment(alignment);
        text.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
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
