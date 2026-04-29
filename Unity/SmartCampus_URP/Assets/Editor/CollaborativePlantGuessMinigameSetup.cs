using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;

public static class CollaborativePlantGuessMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string SceneFolder = "Assets/Scenes";
    private const string StreamingAssetsFolder = "Assets/StreamingAssets/CoopMinigames";
    private const string TutorialConfigPath = ConfigFolder + "/CollaborativePlantGuessTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/CollaborativePlantGuessMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string CsvTemplatePath = StreamingAssetsFolder + "/CollaborativePlantGuessPlants.csv";
    private const string MinigameScenePath = SceneFolder + "/CollaborativePlantGuessMinigame.unity";
    private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
    private const string MainMapScenePath = SceneFolder + "/UJI.unity";
    private const string MinigameSceneName = "CollaborativePlantGuessMinigame";
    private const int MinigameIndex = 3;

    [MenuItem("Tools/Coop/Setup Collaborative Plant Guess Minigame")]
    public static void SetupCollaborativePlantGuessMinigame()
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

    public static void RepairCollaborativePlantGuessInput()
    {
        var scene = EditorSceneManager.OpenScene(MinigameScenePath, OpenSceneMode.Single);
        EnsureInputSystemUiEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "CoopMinigames");
        EnsureFolder(RootFolder, "Configs");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "StreamingAssets");
        EnsureFolder("Assets/StreamingAssets", "CoopMinigames");
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
        serializedObject.FindProperty("title").stringValue = "Adivina la planta";
        serializedObject.FindProperty("subtitle").stringValue = "Deduce en grupo con pistas visuales";
        serializedObject.FindProperty("bodyText").stringValue =
            "Todo el grupo intenta descubrir la planta objetivo.\n\n" +
            "Escribe una planta del CSV con ayuda del autocompletado. Puedes buscar por nombre comun, cientifico o sinonimos y cada intento aparece en todos los dispositivos con pistas de color por atributo.\n\n" +
            "Verde significa acierto, naranja significa casi y rojo significa fallo. El mismo dispositivo no puede enviar dos intentos seguidos.\n\n" +
            "Las pistas aparecen en este orden: rugosidad, tipo de hoja, categoria del fruto, tipo de fruto, hoja perenne o caduca y tipo de planta.\n\n" +
            "El tipo de fruto se desbloquea en el intento 2, la persistencia de la hoja en el 4 y el tipo de planta en el 6.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CollaborativePlantGuessMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<CollaborativePlantGuessMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Minijuego 3 - Adivina la planta";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Planta encontrada";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("csvRelativePath").stringValue = "CoopMinigames/CollaborativePlantGuessPlants.csv";
        serializedObject.FindProperty("minimumSupportedPlayers").intValue = 2;
        serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
        serializedObject.FindProperty("timeLimitSeconds").floatValue = 180f;
        serializedObject.FindProperty("maxAttempts").intValue = 8;
        serializedObject.FindProperty("leafTypeRevealAttempt").intValue = 1;
        serializedObject.FindProperty("fruitDetailRevealAttempt").intValue = 2;
        serializedObject.FindProperty("leafPersistenceRevealAttempt").intValue = 4;
        serializedObject.FindProperty("plantTypeRevealAttempt").intValue = 6;
        serializedObject.FindProperty("victoryRevealDelaySeconds").floatValue = 1.2f;
        serializedObject.FindProperty("autocompleteSuggestionCount").intValue = 6;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.FindProperty("attemptsExhaustedMessage").stringValue = "Intentos agotados";
        serializedObject.FindProperty("invalidGuessMessage").stringValue = "Selecciona una planta valida del autocompletado";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        return CoopMinigameSetupEditorUtility.UpsertCatalogEntry(
            CatalogConfigPath,
            MinigameIndex,
            "Minijuego 3 - Adivina la planta",
            "Adivina una planta entre todos con historial compartido, autocompletado y pistas de atributos tipo Loldle.",
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

        var shouldWriteTemplate = !File.Exists(absolutePath);
        if (!shouldWriteTemplate)
        {
            var existingHeader = File.ReadLines(absolutePath).FirstOrDefault();
            var expectedHeader = BuildCsvTemplate().Split('\n')[0].TrimEnd('\r');
            shouldWriteTemplate = !string.Equals(existingHeader, expectedHeader, StringComparison.Ordinal);
        }

        if (shouldWriteTemplate)
        {
            File.WriteAllText(absolutePath, BuildCsvTemplate());
        }

        AssetDatabase.ImportAsset(CsvTemplatePath);
    }

    private static void SetupMinigameScene(CollaborativePlantGuessMinigameConfig config)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureInputSystemUiEventSystem();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, config.VisualSettings.BackgroundColor);

        var sessionObject = new GameObject("CollaborativePlantGuessSession", typeof(Unity.Netcode.NetworkObject), typeof(CollaborativePlantGuessMinigameSession));
        var session = sessionObject.GetComponent<CollaborativePlantGuessMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("collaborativePlantGuessMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("CollaborativePlantGuessCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = config.VisualSettings.BackgroundColor;

        var uiRoot = CreateUiObject("CollaborativePlantGuessUI", safeAreaRoot.transform, typeof(CollaborativePlantGuessMinigameUIController));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        waitingPanel.AddComponent<ResponsivePanelLayoutController>().Configure(canvas.GetComponent<RectTransform>(), 0.8f, 0.18f, new Vector2(280f, 180f), new Vector2(700f, 260f), new Vector2(32f, 32f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, font, "Esperando al resto del grupo.", 28, TextAnchor.MiddleCenter);
        Stretch(waitingStatus.GetComponent<RectTransform>(), new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(36f, 36f), new Vector2(-36f, -36f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(24, 24, 24, 24);
        gameplayLayout.spacing = 18f;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childForceExpandHeight = false;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, font, config.DisplayName, 40, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var statusPanel = CreatePanel("StatusPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 240f;
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(24, 24, 18, 18);
        statusLayout.spacing = 10f;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;

        var timerLabel = CreateText("TimerLabel", statusPanel.transform, font, "Tiempo restante: 03:00", 24, TextAnchor.MiddleLeft);
        timerLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        var attemptsLabel = CreateText("AttemptsLabel", statusPanel.transform, font, "Intentos: 0/8", 24, TextAnchor.MiddleLeft);
        attemptsLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        var statusLabel = CreateText("StatusLabel", statusPanel.transform, font, "Preparando la partida.", 20, TextAnchor.UpperLeft);
        statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var helperLabel = CreateText("HelperLabel", statusPanel.transform, font, "Escribe una planta del listado.", 18, TextAnchor.UpperLeft);
        helperLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
        var hintLabel = CreateText("HintLabel", statusPanel.transform, font, "Pista", 20, TextAnchor.UpperLeft);
        hintLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        hintLabel.gameObject.SetActive(false);

        var inputPanel = CreatePanel("InputPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        inputPanel.AddComponent<LayoutElement>().preferredHeight = 240f;
        var inputLayout = inputPanel.AddComponent<VerticalLayoutGroup>();
        inputLayout.padding = new RectOffset(20, 20, 18, 18);
        inputLayout.spacing = 10f;
        inputLayout.childControlWidth = true;
        inputLayout.childControlHeight = true;

        var inputField = CreateInputField("GuessInputField", inputPanel.transform, font, "Escribe una planta...");
        inputField.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        var submitButton = CreateButton("SubmitButton", inputPanel.transform, font, "Enviar planta", 22, config.VisualSettings.PrimaryButtonColor);
        submitButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var suggestionPanel = CreatePanel("SuggestionPanel", inputPanel.transform, new Color(1f, 1f, 1f, 0.55f));
        suggestionPanel.AddComponent<LayoutElement>().preferredHeight = 96f;
        var suggestionLayout = suggestionPanel.AddComponent<VerticalLayoutGroup>();
        suggestionLayout.padding = new RectOffset(12, 12, 12, 12);
        suggestionLayout.spacing = 8f;
        suggestionLayout.childControlWidth = true;
        suggestionLayout.childControlHeight = true;
        suggestionLayout.childForceExpandHeight = false;

        var suggestionTemplate = CreateSuggestionTemplate(suggestionPanel.transform, font, config.VisualSettings.SecondaryButtonColor);
        suggestionTemplate.gameObject.SetActive(false);

        var historyPanel = CreatePanel("HistoryPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        historyPanel.AddComponent<LayoutElement>().flexibleHeight = 1f;
        historyPanel.GetComponent<LayoutElement>().minHeight = 360f;

        var emptyHistoryLabel = CreateText("EmptyHistoryLabel", historyPanel.transform, font, "Todavia no hay intentos compartidos.", 22, TextAnchor.MiddleCenter);
        Stretch(emptyHistoryLabel.GetComponent<RectTransform>(), new Vector2(24f, 24f), new Vector2(-24f, -24f));

        var scrollView = CreateScrollView("HistoryScrollView", historyPanel.transform);
        Stretch(scrollView.Root.GetComponent<RectTransform>(), new Vector2(12f, 12f), new Vector2(-12f, -12f));
        scrollView.ContentVerticalLayout.spacing = 12f;
        var historyTemplate = CreateHistoryTemplate(scrollView.ContentRoot.transform, font, config);
        historyTemplate.gameObject.SetActive(false);

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);
        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<CollaborativePlantGuessMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("collaborativePlantGuessMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("attemptsLabel").objectReferenceValue = attemptsLabel;
        serializedUiController.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedUiController.FindProperty("helperLabel").objectReferenceValue = helperLabel;
        serializedUiController.FindProperty("hintLabel").objectReferenceValue = hintLabel;
        serializedUiController.FindProperty("guessInputField").objectReferenceValue = inputField;
        serializedUiController.FindProperty("submitGuessButton").objectReferenceValue = submitButton.GetComponent<Button>();
        serializedUiController.FindProperty("submitGuessButtonLabel").objectReferenceValue = submitButton.GetComponentInChildren<TMP_Text>();
        serializedUiController.FindProperty("suggestionRoot").objectReferenceValue = suggestionPanel.transform;
        serializedUiController.FindProperty("suggestionTemplate").objectReferenceValue = suggestionTemplate;
        serializedUiController.FindProperty("historyRoot").objectReferenceValue = scrollView.ContentRoot.transform;
        serializedUiController.FindProperty("historyRowTemplate").objectReferenceValue = historyTemplate;
        serializedUiController.FindProperty("emptyHistoryLabel").objectReferenceValue = emptyHistoryLabel;
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
        var scenePaths = new[] { MainMapScenePath, LobbyScenePath, MinigameScenePath };
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

    private static CollaborativePlantGuessSuggestionEntryView CreateSuggestionTemplate(Transform parent, Font font, Color buttonColor)
    {
        var buttonObject = CreateButton("SuggestionTemplate", parent, font, "Planta", 20, buttonColor);
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        var suggestionView = buttonObject.AddComponent<CollaborativePlantGuessSuggestionEntryView>();
        var serializedSuggestionView = new SerializedObject(suggestionView);
        serializedSuggestionView.FindProperty("selectionButton").objectReferenceValue = buttonObject.GetComponent<Button>();
        serializedSuggestionView.FindProperty("titleLabel").objectReferenceValue = buttonObject.GetComponentInChildren<TMP_Text>();
        serializedSuggestionView.ApplyModifiedPropertiesWithoutUndo();
        return suggestionView;
    }

    private static CollaborativePlantGuessHistoryRowView CreateHistoryTemplate(Transform parent, Font font, CollaborativePlantGuessMinigameConfig config)
    {
        var root = CreatePanel("HistoryRowTemplate", parent, new Color(1f, 1f, 1f, 0.58f));
        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var rowLayoutElement = root.AddComponent<LayoutElement>();
        rowLayoutElement.minHeight = 136f;
        rowLayoutElement.preferredHeight = -1f;
        var rowFitter = root.AddComponent<ContentSizeFitter>();
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var infoColumn = CreateUiObject("InfoColumn", root.transform, typeof(VerticalLayoutGroup));
        var infoLayoutElement = infoColumn.AddComponent<LayoutElement>();
        infoLayoutElement.minWidth = 196f;
        infoLayoutElement.preferredWidth = 228f;
        infoLayoutElement.flexibleWidth = 0f;
        var infoLayout = infoColumn.GetComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 6f;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandHeight = false;

        var guessedByLabel = CreateText("GuessedByLabel", infoColumn.transform, font, "Intento", 18, TextAnchor.MiddleLeft);
        guessedByLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        guessedByLabel.gameObject.SetActive(false);
        var plantImageRoot = CreatePanel("PlantImageRoot", infoColumn.transform, config.VisualSettings.NeutralCellColor);
        plantImageRoot.AddComponent<LayoutElement>().preferredHeight = 78f;
        var plantImage = CreateUiObject("PlantImage", plantImageRoot.transform, typeof(Image)).GetComponent<Image>();
        Stretch(plantImage.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -6f));
        plantImage.preserveAspect = true;
        var placeholder = CreateText("ImagePlaceholder", plantImageRoot.transform, font, "Sin imagen", 16, TextAnchor.MiddleCenter);
        Stretch(placeholder.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        var plantNameLabel = CreateText("PlantNameLabel", infoColumn.transform, font, "Planta", 22, TextAnchor.MiddleLeft);
        plantNameLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = -1f;

        var comparisonsRow = CreateUiObject("ComparisonsRow", root.transform, typeof(HorizontalLayoutGroup));
        var comparisonsRowLayoutElement = comparisonsRow.AddComponent<LayoutElement>();
        comparisonsRowLayoutElement.flexibleWidth = 1f;
        var comparisonsLayout = comparisonsRow.GetComponent<HorizontalLayoutGroup>();
        comparisonsLayout.padding = new RectOffset(0, 0, 0, 0);
        comparisonsLayout.spacing = 8f;
        comparisonsLayout.childControlWidth = true;
        comparisonsLayout.childControlHeight = true;
        comparisonsLayout.childForceExpandWidth = true;
        comparisonsLayout.childForceExpandHeight = false;
        comparisonsLayout.childAlignment = TextAnchor.UpperLeft;

        var surfaceRoughnessCell = CreateComparisonCell("Rugosidad", comparisonsRow.transform, font, "Media", config.VisualSettings.NeutralCellColor);
        var leafTypeCell = CreateComparisonCell("Tipo de hoja", comparisonsRow.transform, font, "Lanceolada", config.VisualSettings.NeutralCellColor);
        var fruitCategoryCell = CreateComparisonCell("Categoria del fruto", comparisonsRow.transform, font, "Carnoso", config.VisualSettings.NeutralCellColor);
        var fruitTypeCell = CreateComparisonCell("Tipo de fruto", comparisonsRow.transform, font, "?", config.VisualSettings.NeutralCellColor);
        var leafPersistenceCell = CreateComparisonCell("Hoja perenne/caduca", comparisonsRow.transform, font, "?", config.VisualSettings.NeutralCellColor);
        var plantTypeCell = CreateComparisonCell("Tipo de planta", comparisonsRow.transform, font, "?", config.VisualSettings.NeutralCellColor);

        var rowView = root.AddComponent<CollaborativePlantGuessHistoryRowView>();
        var serializedRowView = new SerializedObject(rowView);
        serializedRowView.FindProperty("guessedByLabel").objectReferenceValue = guessedByLabel;
        serializedRowView.FindProperty("plantNameLabel").objectReferenceValue = plantNameLabel;
        serializedRowView.FindProperty("plantImage").objectReferenceValue = plantImage;
        serializedRowView.FindProperty("plantImagePlaceholder").objectReferenceValue = placeholder.gameObject;
        serializedRowView.FindProperty("plantTypeCell").objectReferenceValue = plantTypeCell.RootImage;
        serializedRowView.FindProperty("plantTypeLabel").objectReferenceValue = plantTypeCell.ValueLabel;
        serializedRowView.FindProperty("surfaceRoughnessCell").objectReferenceValue = surfaceRoughnessCell.RootImage;
        serializedRowView.FindProperty("surfaceRoughnessLabel").objectReferenceValue = surfaceRoughnessCell.ValueLabel;
        serializedRowView.FindProperty("leafPersistenceCell").objectReferenceValue = leafPersistenceCell.RootImage;
        serializedRowView.FindProperty("leafPersistenceLabel").objectReferenceValue = leafPersistenceCell.ValueLabel;
        serializedRowView.FindProperty("leafTypeCell").objectReferenceValue = leafTypeCell.RootImage;
        serializedRowView.FindProperty("leafTypeLabel").objectReferenceValue = leafTypeCell.ValueLabel;
        serializedRowView.FindProperty("fruitCategoryCell").objectReferenceValue = fruitCategoryCell.RootImage;
        serializedRowView.FindProperty("fruitCategoryLabel").objectReferenceValue = fruitCategoryCell.ValueLabel;
        serializedRowView.FindProperty("fruitTypeCell").objectReferenceValue = fruitTypeCell.RootImage;
        serializedRowView.FindProperty("fruitTypeLabel").objectReferenceValue = fruitTypeCell.ValueLabel;
        serializedRowView.ApplyModifiedPropertiesWithoutUndo();
        return rowView;
    }

    private static ComparisonCellReferences CreateComparisonCell(string headerText, Transform parent, Font font, string value, Color backgroundColor)
    {
        var root = CreatePanel($"{headerText}Cell", parent, backgroundColor);
        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.minWidth = 84f;
        layoutElement.preferredWidth = 104f;
        layoutElement.flexibleWidth = 1f;
        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 8);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        var fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var header = CreateText("HeaderLabel", root.transform, font, headerText, 16, TextAnchor.UpperCenter);
        header.enableWordWrapping = true;
        header.overflowMode = TextOverflowModes.Overflow;
        header.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        var valueLabel = CreateText("ValueLabel", root.transform, font, value, 20, TextAnchor.UpperCenter);
        valueLabel.enableWordWrapping = true;
        valueLabel.overflowMode = TextOverflowModes.Overflow;
        valueLabel.gameObject.AddComponent<LayoutElement>().minHeight = 36f;

        return new ComparisonCellReferences(root.GetComponent<Image>(), valueLabel);
    }

    private static ScrollViewReferences CreateScrollView(string name, Transform parent)
    {
        var root = CreateUiObject(name, parent, typeof(Image), typeof(ScrollRect));
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);

        var viewport = CreateUiObject("Viewport", root.transform, typeof(Image), typeof(Mask));
        Stretch(viewport.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -6f));
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var contentRoot = CreateUiObject("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var verticalLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(8, 8, 8, 8);
        verticalLayout.spacing = 8f;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandHeight = false;
        contentRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return new ScrollViewReferences(root, contentRoot, verticalLayout);
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, Font font, string placeholder)
    {
        var root = CreatePanel(name, parent, Color.white);
        var inputField = root.AddComponent<TMP_InputField>();
        var textLabel = CreateText("Text", root.transform, font, string.Empty, 22, TextAnchor.MiddleLeft);
        var placeholderLabel = CreateText("Placeholder", root.transform, font, placeholder, 22, TextAnchor.MiddleLeft);
        placeholderLabel.color = new Color(0.35f, 0.39f, 0.42f, 0.7f);

        var textRect = textLabel.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);
        var placeholderRect = placeholderLabel.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(18f, 12f);
        placeholderRect.offsetMax = new Vector2(-18f, -12f);

        inputField.targetGraphic = root.GetComponent<Image>();
        inputField.textComponent = textLabel;
        inputField.placeholder = placeholderLabel;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        return inputField;
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
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.9f, 0.86f, new Vector2(320f, 460f), new Vector2(840f, 1160f), new Vector2(32f, 32f));

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        var closeButton = CreateButton("CloseButton", contentPanel.transform, font, "X", 20, new Color(0.21f, 0.42f, 0.46f, 1f));
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
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.86f, 0.42f, new Vector2(320f, 320f), new Vector2(720f, 560f), new Vector2(32f, 32f));

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

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
        serializedResultView.FindProperty("successfulActionsLabel").stringValue = "Plantas acertadas";
        serializedResultView.FindProperty("failedActionsLabel").stringValue = "Intentos fallidos";
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();
        return resultView;
    }

    private static string BuildCsvTemplate()
    {
        return
            "plantId,commonName,scientificName,synonyms,imagePath,surfaceRoughness,leafType,fruitCategory,fruitType,leafPersistence,plantType\n" +
            "adelfa_baladre,Adelfa / baladre,Nerium oleander,Adelfa|Baladre|Nerium oleander,,Media,Lanceolada,Seco,Foliculo,Perenne,Arbusto\n" +
            "acebo,Acebo,Ilex aquifolium,Acebo|Ilex aquifolium,,Rugosa,Coriacea,Carnoso,Baya,Perenne,Arbusto\n" +
            "palmito_margallo,Palmito / margallo,Chamaerops humilis,Palmito|Margallo|Chamaerops humilis,,Aspera,Palmada,Carnoso,Drupa,Perenne,Palmera\n" +
            "olivo,Olivo,Olea europaea,Olivo|Olea europaea,,Media,Lanceolada,Carnoso,Drupa,Perenne,Arbol\n" +
            "encina,Encina,Quercus ilex,Encina|Carrasca|Quercus ilex,,Rugosa,Coriacea,Seco,Bellota,Perenne,Arbol\n" +
            "hiedra_enredadera,Hiedra / enredadera,Hedera helix,Hiedra|Enredadera|Enredaderas|Hedera helix,,Media,Lobulada,Carnoso,Baya,Perenne,Trepadora\n" +
            "garrofera_algarrobo,Garrofera / algarrobo,Ceratonia siliqua,Garrofera|Algarrobo|Ceratonia siliqua,,Media,Compuesta,Seco,Legumbre,Perenne,Arbol\n" +
            "madrono,Madrono,Arbutus unedo,Madrono|Arbutus unedo,,Media,Aserrada,Carnoso,Baya,Perenne,Arbusto\n" +
            "alcornoque_surera,Alcornoque / surera,Quercus suber,Alcornoque|Surera|Quercus suber,,Muy rugosa,Coriacea,Seco,Bellota,Perenne,Arbol\n" +
            "pino_carrasco,Pino carrasco,Pinus halepensis,Pino carrasco|Pinus halepensis,,Rugosa,Acicular,Seco,Pina,Perenne,Arbol\n" +
            "chumbera_figa_palera,Chumbera / figa palera,Opuntia ficus-indica,Chumbera|Figa palera|Nopal|Opuntia ficus-indica,,Media,Carnosa,Carnoso,Baya,Perenne,Suculenta\n";
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

        var gameObject = eventSystem.gameObject;
        var standaloneInputModule = gameObject.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            UnityEngine.Object.DestroyImmediate(standaloneInputModule);
        }

        if (gameObject.GetComponent<InputSystemUIInputModule>() == null)
        {
            gameObject.AddComponent<InputSystemUIInputModule>();
        }
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
        Stretch(labelText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        labelText.color = Color.white;
        return buttonObject;
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private readonly struct ComparisonCellReferences
    {
        public ComparisonCellReferences(Image rootImage, TMP_Text valueLabel)
        {
            RootImage = rootImage;
            ValueLabel = valueLabel;
        }

        public Image RootImage { get; }
        public TMP_Text ValueLabel { get; }
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
}

internal static class CoopMinigameSetupEditorUtility
{
    private const string MainMapScenePath = "Assets/Scenes/UJI.unity";
    private const string CatalogConfigPath = "Assets/CoopMinigames/Configs/CoopMinigameCatalog.asset";

    [MenuItem("Tools/Coop/Refresh Main Map Minigame Launcher")]
    public static void RefreshMainMapMinigameLauncher()
    {
        var catalogConfig = AssetDatabase.LoadAssetAtPath<CoopMinigameCatalogConfig>(CatalogConfigPath);
        if (catalogConfig == null)
        {
            throw new InvalidOperationException("No existe CoopMinigameCatalog.asset. Ejecuta primero el setup de al menos un minijuego.");
        }

        ConfigureMainMapLauncher(catalogConfig);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static CoopMinigameCatalogConfig UpsertCatalogEntry(string catalogConfigPath, int minigameIndex, string displayName, string description, string sceneName)
    {
        var asset = AssetDatabase.LoadAssetAtPath<CoopMinigameCatalogConfig>(catalogConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CoopMinigameCatalogConfig>();
            AssetDatabase.CreateAsset(asset, catalogConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        var entries = serializedObject.FindProperty("entries");
        var targetIndex = FindEntryIndex(entries, minigameIndex);
        if (targetIndex < 0)
        {
            targetIndex = entries.arraySize;
            entries.InsertArrayElementAtIndex(targetIndex);
        }

        var entry = entries.GetArrayElementAtIndex(targetIndex);
        entry.FindPropertyRelative("minigameIndex").intValue = minigameIndex;
        entry.FindPropertyRelative("displayName").stringValue = displayName;
        entry.FindPropertyRelative("description").stringValue = description;
        entry.FindPropertyRelative("sceneName").stringValue = sceneName;

        SortEntries(entries);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    public static void ConfigureMainMapLauncher(CoopMinigameCatalogConfig catalogConfig)
    {
        var scene = EditorSceneManager.OpenScene(MainMapScenePath, OpenSceneMode.Single);
        var launcherController = UnityEngine.Object.FindFirstObjectByType<CoopMinigameLauncherUIController>(FindObjectsInactive.Include);
        if (launcherController == null)
        {
            throw new InvalidOperationException("La escena UJI necesita un CoopMinigameLauncherUIController.");
        }

        ApplyLauncherCatalogReference(launcherController, catalogConfig);
        EnsureLauncherSceneBindings(launcherController, catalogConfig);

        var safeAreaRoot = ConfigureResponsiveMainMapCanvas(launcherController);
        var launcherPanel = launcherController.transform as RectTransform;
        if (launcherPanel == null)
        {
            throw new InvalidOperationException("El launcher del mapa principal necesita un RectTransform.");
        }

        if (safeAreaRoot != null && launcherPanel.parent != safeAreaRoot)
        {
            launcherPanel.SetParent(safeAreaRoot, false);
        }

        var serializedLauncherController = new SerializedObject(launcherController);
        serializedLauncherController.Update();
        var entryRoot = serializedLauncherController.FindProperty("entryRoot").objectReferenceValue as Transform;
        var entryTemplate = serializedLauncherController.FindProperty("entryTemplate").objectReferenceValue as CoopMinigameLauncherEntryView;
        if (entryRoot == null || entryTemplate == null)
        {
            throw new InvalidOperationException("La escena UJI necesita entryRoot y entryTemplate configurados en el launcher.");
        }

        ConfigureLauncherPanel(launcherPanel, launcherController, safeAreaRoot);
        EnsureScrollableEntryRoot(launcherPanel, entryRoot);
        EnsureSceneVisibleEntries(entryRoot, entryTemplate, catalogConfig);
        EnsureLauncherSceneBindings(launcherController, catalogConfig);
        EnsureToggleLauncherBindings();
        SanitizeRectTransformTree(canvasRoot: safeAreaRoot != null ? safeAreaRoot : launcherPanel.root as RectTransform);
        ApplyLauncherCatalogReference(launcherController, catalogConfig);
        EnsureLauncherSceneBindings(launcherController, catalogConfig);
        EnsureToggleLauncherBindings();

        EditorUtility.SetDirty(launcherController);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyLauncherCatalogReference(CoopMinigameLauncherUIController launcherController, CoopMinigameCatalogConfig catalogConfig)
    {
        var serializedLauncherController = new SerializedObject(launcherController);
        serializedLauncherController.Update();
        serializedLauncherController.FindProperty("minigameCatalogConfig").objectReferenceValue = catalogConfig;
        serializedLauncherController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform ConfigureResponsiveMainMapCanvas(CoopMinigameLauncherUIController launcherController)
    {
        var canvas = launcherController.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRect = canvas.GetComponent<RectTransform>();
        var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") as RectTransform;
        if (safeAreaRoot == null)
        {
            var safeAreaObject = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaObject.layer = canvas.gameObject.layer;
            safeAreaObject.transform.SetParent(canvas.transform, false);
            safeAreaRoot = safeAreaObject.GetComponent<RectTransform>();
        }

        Stretch(safeAreaRoot, Vector2.zero, Vector2.zero);
        return safeAreaRoot;
    }

    private static void ConfigureLauncherPanel(RectTransform launcherPanel, CoopMinigameLauncherUIController launcherController, RectTransform safeAreaRoot)
    {
        launcherPanel.anchorMin = new Vector2(0.5f, 1f);
        launcherPanel.anchorMax = new Vector2(0.5f, 1f);
        launcherPanel.pivot = new Vector2(0.5f, 1f);
        launcherPanel.anchoredPosition = new Vector2(0f, -32f);

        var panelLayout = launcherPanel.GetComponent<VerticalLayoutGroup>();
        if (panelLayout != null)
        {
            panelLayout.padding = new RectOffset(24, 24, 24, 24);
            panelLayout.spacing = 14f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
        }

        var responsivePanel = launcherPanel.GetComponent<ResponsivePanelLayoutController>();
        if (responsivePanel == null)
        {
            responsivePanel = launcherPanel.gameObject.AddComponent<ResponsivePanelLayoutController>();
        }

        responsivePanel.Configure(
            safeAreaRoot != null ? safeAreaRoot : launcherPanel.parent as RectTransform,
            0.88f,
            0.82f,
            new Vector2(320f, 360f),
            new Vector2(720f, 1280f),
            new Vector2(32f, 32f));

        var helperLabelField = new SerializedObject(launcherController).FindProperty("helperLabel").objectReferenceValue as TMP_Text;
        if (helperLabelField != null)
        {
            helperLabelField.enableAutoSizing = false;
            helperLabelField.enableWordWrapping = true;
            helperLabelField.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static void EnsureLauncherSceneBindings(CoopMinigameLauncherUIController launcherController, CoopMinigameCatalogConfig catalogConfig)
    {
        if (launcherController == null)
        {
            return;
        }

        var launcherPanel = launcherController.transform as RectTransform;
        if (launcherPanel == null)
        {
            return;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var headerLabel = EnsureTextLabel(
            launcherPanel,
            "Header",
            "Hub de minijuegos cooperativos",
            34,
            TextAnchor.MiddleCenter,
            40f,
            new Color(0.33f, 0.18f, 0.07f, 1f),
            new Color(0.98f, 0.92f, 0.78f, 0.9f));
        var helperLabel = EnsureTextLabel(
            launcherPanel,
            "HelperLabel",
            "Elige el siguiente minijuego para todo el grupo.",
            18,
            TextAnchor.UpperLeft,
            54f,
            new Color(0.25f, 0.15f, 0.08f, 1f),
            new Color(0.96f, 0.9f, 0.8f, 0.35f));

        var entryRoot = launcherController.transform.Find("EntriesScrollView/Viewport/EntriesContent");
        if (entryRoot == null)
        {
            var rootObject = new GameObject("EntriesContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rootObject.layer = launcherPanel.gameObject.layer;
            rootObject.transform.SetParent(launcherPanel, false);
            entryRoot = rootObject.transform;
        }

        var entryTemplate = entryRoot.GetComponentInChildren<CoopMinigameLauncherEntryView>(true);
        if (entryTemplate == null)
        {
            entryTemplate = CreateLauncherEntryTemplate(entryRoot, font);
        }

        EnsureLauncherEntryTemplateBindings(entryTemplate, font);

        var serializedLauncherController = new SerializedObject(launcherController);
        serializedLauncherController.Update();
        if (catalogConfig != null)
        {
            serializedLauncherController.FindProperty("minigameCatalogConfig").objectReferenceValue = catalogConfig;
        }
        serializedLauncherController.FindProperty("entryRoot").objectReferenceValue = entryRoot;
        serializedLauncherController.FindProperty("entryTemplate").objectReferenceValue = entryTemplate;
        serializedLauncherController.FindProperty("helperLabel").objectReferenceValue = helperLabel;
        serializedLauncherController.ApplyModifiedPropertiesWithoutUndo();

        if (headerLabel != null)
        {
            EditorUtility.SetDirty(headerLabel);
        }

        EditorUtility.SetDirty(launcherController);
    }

    private static void EnsureLauncherEntryTemplateBindings(CoopMinigameLauncherEntryView entryTemplate, Font font)
    {
        if (entryTemplate == null)
        {
            return;
        }

        var titleLabel = EnsureTextLabel(
            entryTemplate.transform,
            "TitleLabel",
            "Minijuego",
            24,
            TextAnchor.MiddleLeft,
            34f,
            new Color(0.33f, 0.18f, 0.07f, 1f),
            new Color(0.98f, 0.92f, 0.78f, 0.9f));
        var descriptionLabel = EnsureTextLabel(
            entryTemplate.transform,
            "DescriptionLabel",
            "Descripcion",
            18,
            TextAnchor.UpperLeft,
            64f,
            new Color(0.25f, 0.15f, 0.08f, 1f),
            new Color(0.96f, 0.9f, 0.8f, 0.35f));

        var buttonTransform = entryTemplate.transform.Find("LaunchButton");
        Button launchButton;
        if (buttonTransform == null)
        {
            var buttonObject = new GameObject("LaunchButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.layer = entryTemplate.gameObject.layer;
            buttonObject.transform.SetParent(entryTemplate.transform, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            launchButton = buttonObject.GetComponent<Button>();
            launchButton.targetGraphic = image;
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 52f;
        }
        else
        {
            launchButton = buttonTransform.GetComponent<Button>();
        }

        var buttonLabel = EnsureButtonLabel(launchButton, "Lanzar", 20, font);
        var serializedEntryView = new SerializedObject(entryTemplate);
        serializedEntryView.Update();
        serializedEntryView.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedEntryView.FindProperty("descriptionLabel").objectReferenceValue = descriptionLabel;
        serializedEntryView.FindProperty("launchButton").objectReferenceValue = launchButton;
        serializedEntryView.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
        serializedEntryView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(entryTemplate);
    }

    private static CoopMinigameLauncherEntryView CreateLauncherEntryTemplate(Transform parent, Font font)
    {
        var entryRoot = new GameObject("EntryTemplate", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(CoopMinigameLauncherEntryView));
        entryRoot.layer = parent.gameObject.layer;
        entryRoot.transform.SetParent(parent, false);

        var image = entryRoot.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);

        var layout = entryRoot.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var entryView = entryRoot.GetComponent<CoopMinigameLauncherEntryView>();
        EnsureLauncherEntryTemplateBindings(entryView, font);
        return entryView;
    }

    private static TMP_Text EnsureTextLabel(
        Transform parent,
        string objectName,
        string defaultText,
        int fontSize,
        TextAnchor alignment,
        float preferredHeight,
        Color color,
        Color shadowColor)
    {
        var labelTransform = parent.Find(objectName);
        if (labelTransform == null)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            labelTransform = labelObject.transform;
        }

        var text = labelTransform.GetComponent<TMP_Text>() ?? labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrWhiteSpace(text.text) ? defaultText : text.text;
        text.fontSize = fontSize;
        text.alignment = ConvertAlignment(alignment);
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        var layoutElement = labelTransform.GetComponent<LayoutElement>() ?? labelTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;

        var shadow = labelTransform.GetComponent<Shadow>() ?? labelTransform.gameObject.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1f, -1f);
        shadow.useGraphicAlpha = true;

        return text;
    }

    private static TMP_Text EnsureButtonLabel(Button button, string defaultText, int fontSize, Font font)
    {
        if (button == null)
        {
            return null;
        }

        var labelTransform = button.transform.Find("Label");
        if (labelTransform == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = button.gameObject.layer;
            labelObject.transform.SetParent(button.transform, false);
            labelTransform = labelObject.transform;
        }

        var text = labelTransform.GetComponent<TMP_Text>() ?? labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrWhiteSpace(text.text) ? defaultText : text.text;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.27f, 0.13f, 0.03f, 1f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        if (text is TextMeshProUGUI textMeshPro)
        {
            textMeshPro.enableAutoSizing = true;
            textMeshPro.fontSizeMin = 14f;
            textMeshPro.fontSizeMax = fontSize;
        }

        Stretch(text.rectTransform, new Vector2(16f, 10f), new Vector2(-16f, -10f));
        var shadow = labelTransform.GetComponent<Shadow>() ?? labelTransform.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.95f, 0.84f, 0.62f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);
        shadow.useGraphicAlpha = true;
        return text;
    }

    private static void EnsureToggleLauncherBindings()
    {
        var toggleController = UnityEngine.Object.FindFirstObjectByType<MinigameLauncherToggleUIController>(FindObjectsInactive.Include);
        if (toggleController == null)
        {
            return;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var serializedToggle = new SerializedObject(toggleController);
        serializedToggle.Update();

        var toggleButton = serializedToggle.FindProperty("toggleButton").objectReferenceValue as Button;
        if (toggleButton == null)
        {
            toggleButton = toggleController.GetComponentInChildren<Button>(true);
        }

        var visibleLabel = serializedToggle.FindProperty("visibleStateLabel").stringValue;
        var buttonLabel = EnsureButtonLabel(toggleButton, string.IsNullOrWhiteSpace(visibleLabel) ? "Ocultar\nminijuegos" : visibleLabel, 20, font);

        serializedToggle.FindProperty("toggleButton").objectReferenceValue = toggleButton;
        serializedToggle.FindProperty("toggleButtonLabel").objectReferenceValue = buttonLabel;
        serializedToggle.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(toggleController);
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

    private static void EnsureScrollableEntryRoot(RectTransform launcherPanel, Transform entryRoot)
    {
        if (launcherPanel == null || entryRoot == null)
        {
            return;
        }

        var scrollRoot = launcherPanel.Find("EntriesScrollView") as RectTransform;
        RectTransform viewport;
        if (scrollRoot == null)
        {
            var scrollObject = new GameObject("EntriesScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.layer = launcherPanel.gameObject.layer;
            scrollObject.transform.SetParent(launcherPanel, false);
            scrollRoot = scrollObject.GetComponent<RectTransform>();
            scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            var scrollLayoutElement = scrollObject.GetComponent<LayoutElement>();
            scrollLayoutElement.flexibleHeight = 1f;
            scrollLayoutElement.minHeight = 220f;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.layer = launcherPanel.gameObject.layer;
            viewportObject.transform.SetParent(scrollRoot, false);
            viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
        else
        {
            viewport = scrollRoot.Find("Viewport") as RectTransform;
        }

        if (viewport == null)
        {
            throw new InvalidOperationException("EntriesScrollView necesita un Viewport.");
        }

        if (entryRoot.parent != viewport)
        {
            entryRoot.SetParent(viewport, false);
        }

        entryRoot.name = "EntriesContent";
        if (entryRoot is RectTransform entryRect)
        {
            entryRect.anchorMin = new Vector2(0f, 1f);
            entryRect.anchorMax = new Vector2(1f, 1f);
            entryRect.pivot = new Vector2(0.5f, 1f);
            entryRect.offsetMin = Vector2.zero;
            entryRect.offsetMax = Vector2.zero;
        }

        var contentLayout = entryRoot.GetComponent<VerticalLayoutGroup>();
        if (contentLayout != null)
        {
            contentLayout.spacing = 12f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
        }

        var fitter = entryRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = entryRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scrollRectComponent = scrollRoot.GetComponent<ScrollRect>();
        scrollRectComponent.content = entryRoot as RectTransform;
    }

    private static void EnsureSceneVisibleEntries(Transform entryRoot, CoopMinigameLauncherEntryView entryTemplate, CoopMinigameCatalogConfig catalogConfig)
    {
        var existingEntries = new List<CoopMinigameLauncherEntryView>();
        for (var childIndex = 0; childIndex < entryRoot.childCount; childIndex++)
        {
            var child = entryRoot.GetChild(childIndex);
            if (child == entryTemplate.transform)
            {
                continue;
            }

            var childEntry = child.GetComponent<CoopMinigameLauncherEntryView>();
            if (childEntry != null)
            {
                existingEntries.Add(childEntry);
            }
        }

        for (var index = existingEntries.Count; index < catalogConfig.Entries.Count; index++)
        {
            var instance = UnityEngine.Object.Instantiate(entryTemplate, entryRoot, false);
            existingEntries.Add(instance);
        }

        entryTemplate.gameObject.name = "EntryTemplate";
        entryTemplate.gameObject.SetActive(false);
        entryTemplate.transform.SetAsLastSibling();

        for (var index = 0; index < existingEntries.Count; index++)
        {
            var entryView = existingEntries[index];
            var shouldBeVisible = index < catalogConfig.Entries.Count;
            entryView.gameObject.SetActive(shouldBeVisible);
            if (!shouldBeVisible)
            {
                continue;
            }

            var catalogEntry = catalogConfig.Entries[index];
            entryView.gameObject.name = BuildEntryObjectName(catalogEntry);
            entryView.transform.SetSiblingIndex(index);
            entryView.ConfigureForSceneAuthoredPresentation(catalogEntry.DisplayName, catalogEntry.Description, "Abrir");

            var layoutElement = entryView.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = entryView.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = 168f;
            layoutElement.flexibleHeight = 0f;
        }
    }

    private static void SanitizeRectTransformTree(RectTransform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return;
        }

        foreach (var rectTransform in canvasRoot.GetComponentsInChildren<RectTransform>(true))
        {
            rectTransform.anchorMin = SanitizeVector2(rectTransform.anchorMin, Vector2.zero);
            rectTransform.anchorMax = SanitizeVector2(rectTransform.anchorMax, Vector2.one);
            rectTransform.anchoredPosition = SanitizeVector2(rectTransform.anchoredPosition, Vector2.zero);
            rectTransform.sizeDelta = SanitizeVector2(rectTransform.sizeDelta, Vector2.zero);
            rectTransform.offsetMin = SanitizeVector2(rectTransform.offsetMin, Vector2.zero);
            rectTransform.offsetMax = SanitizeVector2(rectTransform.offsetMax, Vector2.zero);
            rectTransform.pivot = SanitizeVector2(rectTransform.pivot, new Vector2(0.5f, 0.5f));
            rectTransform.localScale = SanitizeVector3(rectTransform.localScale, Vector3.one);
            rectTransform.localPosition = SanitizeVector3(rectTransform.localPosition, Vector3.zero);
        }
    }

    private static Vector2 SanitizeVector2(Vector2 value, Vector2 fallback)
    {
        return new Vector2(
            float.IsFinite(value.x) ? value.x : fallback.x,
            float.IsFinite(value.y) ? value.y : fallback.y);
    }

    private static Vector3 SanitizeVector3(Vector3 value, Vector3 fallback)
    {
        return new Vector3(
            float.IsFinite(value.x) ? value.x : fallback.x,
            float.IsFinite(value.y) ? value.y : fallback.y,
            float.IsFinite(value.z) ? value.z : fallback.z);
    }

    private static int FindEntryIndex(SerializedProperty entries, int minigameIndex)
    {
        for (var index = 0; index < entries.arraySize; index++)
        {
            if (entries.GetArrayElementAtIndex(index).FindPropertyRelative("minigameIndex").intValue == minigameIndex)
            {
                return index;
            }
        }

        return -1;
    }

    private static void SortEntries(SerializedProperty entries)
    {
        var values = new List<(int MinigameIndex, string DisplayName, string Description, string SceneName)>();
        for (var index = 0; index < entries.arraySize; index++)
        {
            var entry = entries.GetArrayElementAtIndex(index);
            values.Add((
                entry.FindPropertyRelative("minigameIndex").intValue,
                entry.FindPropertyRelative("displayName").stringValue,
                entry.FindPropertyRelative("description").stringValue,
                entry.FindPropertyRelative("sceneName").stringValue));
        }

        values.Sort((left, right) => left.MinigameIndex.CompareTo(right.MinigameIndex));
        entries.arraySize = values.Count;
        for (var index = 0; index < values.Count; index++)
        {
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("minigameIndex").intValue = values[index].MinigameIndex;
            entry.FindPropertyRelative("displayName").stringValue = values[index].DisplayName;
            entry.FindPropertyRelative("description").stringValue = values[index].Description;
            entry.FindPropertyRelative("sceneName").stringValue = values[index].SceneName;
        }
    }

    private static string BuildEntryObjectName(CoopMinigameCatalogEntry entry)
    {
        var sanitizedName = new string(entry.DisplayName.Where(character => char.IsLetterOrDigit(character)).ToArray());
        return $"MinigameShortcut_{entry.MinigameIndex}_{sanitizedName}";
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }
}
