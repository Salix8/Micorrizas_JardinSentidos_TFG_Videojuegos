using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.PlantPhotoRelay;

public static class PlantPhotoRelayMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string SceneFolder = "Assets/Scenes";
    private const string StreamingAssetsFolder = "Assets/StreamingAssets/CoopMinigames";
    private const string MinigameFolder = StreamingAssetsFolder + "/06-PlantPhotoRelay";
    private const string TutorialConfigPath = ConfigFolder + "/PlantPhotoRelayTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/PlantPhotoRelayMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string CsvTemplatePath = MinigameFolder + "/PlantPhotoRelayPlants.csv";
    private const string CollaborativePlantGuessCsvPath = StreamingAssetsFolder + "/CollaborativePlantGuessPlants.csv";
    private const string MinigameScenePath = SceneFolder + "/PlantPhotoRelayMinigame.unity";
    private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
    private const string MinigameSceneName = "PlantPhotoRelayMinigame";
    private const int MinigameIndex = 5;

    [MenuItem("Tools/Coop/Setup Plant Photo Relay Minigame")]
    public static void SetupPlantPhotoRelayMinigame()
    {
        EnsureFolders();

        var tutorialContent = CreateOrUpdateTutorialContent();
        var minigameConfig = CreateOrUpdateMinigameConfig(tutorialContent);
        var catalogConfig = CreateOrUpdateCatalogConfig();
        SyncCsvCatalog();

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
        EnsureFolder(StreamingAssetsFolder, "06-PlantPhotoRelay");
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
        serializedObject.FindProperty("title").stringValue = "Pista, foto y planta";
        serializedObject.FindProperty("subtitle").stringValue = "Un jugador fotografia y otro adivina";
        serializedObject.FindProperty("bodyText").stringValue =
            "En cada ronda aparece una pista descriptiva de una planta del catalogo UJI.\n\n" +
            "El dispositivo fotografo debe localizar una planta que encaje, abrir la camara, hacer una foto y confirmar su nombre comun desde el autocompletado.\n\n" +
            "Despues, el dispositivo adivinador recibe la foto y debe elegir una planta valida del mismo catalogo.\n\n" +
            "Se acierta cuando ambos dispositivos confirman el mismo nombre comun canónico.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static PlantPhotoRelayMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<PlantPhotoRelayMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<PlantPhotoRelayMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Minijuego 6 - Pista, foto y planta";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Rondas completadas";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("catalogCsvRelativePath").stringValue = "CoopMinigames/06-PlantPhotoRelay/PlantPhotoRelayPlants.csv";
        serializedObject.FindProperty("maxAutocompleteSuggestionCount").intValue = 6;
        serializedObject.FindProperty("minimumSupportedPlayers").intValue = 2;
        serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
        serializedObject.FindProperty("roundCount").intValue = 3;
        serializedObject.FindProperty("cluePhaseDurationSeconds").floatValue = 12f;
        serializedObject.FindProperty("capturePhaseDurationSeconds").floatValue = 45f;
        serializedObject.FindProperty("guessPhaseDurationSeconds").floatValue = 30f;
        serializedObject.FindProperty("resultsRevealDurationSeconds").floatValue = 4f;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.FindProperty("invalidSelectionMessage").stringValue = "Selecciona una planta valida del catalogo";
        serializedObject.FindProperty("cameraUnavailableMessage").stringValue = "La camara no esta disponible o la captura se ha cancelado";
        serializedObject.FindProperty("scoreExactMatch").floatValue = 2.5f;
        serializedObject.FindProperty("scorePromptMatchBonus").floatValue = 0.5f;
        serializedObject.FindProperty("targetPhotoMaxDimension").intValue = 768;
        serializedObject.FindProperty("jpegQuality").intValue = 80;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        return CoopMinigameSetupEditorUtility.UpsertCatalogEntry(
            CatalogConfigPath,
            MinigameIndex,
            "Minijuego 6 - Pista, foto y planta",
            "Un dispositivo fotografia una planta guiado por una pista y otro la adivina por nombre comun.",
            MinigameSceneName);
    }

    private static void SyncCsvCatalog()
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), CsvTemplatePath);
        var directoryPath = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var csvContent = BuildCsvTemplate();
        if (!File.Exists(absolutePath) || !string.Equals(File.ReadAllText(absolutePath), csvContent, StringComparison.Ordinal))
        {
            File.WriteAllText(absolutePath, csvContent);
        }

        AssetDatabase.ImportAsset(CsvTemplatePath);
    }

    private static string BuildCsvTemplate()
    {
        var collaborativeAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), CollaborativePlantGuessCsvPath);
        if (File.Exists(collaborativeAbsolutePath))
        {
            var transformedCatalog = TryBuildCsvTemplateFromCollaborativeCatalog(File.ReadAllText(collaborativeAbsolutePath));
            if (!string.IsNullOrWhiteSpace(transformedCatalog))
            {
                return transformedCatalog;
            }
        }

        return
            "commonNameCanonical,displayCommonName,acceptedCommonNameVariants,plantType,surfaceTexture,hasThorns,hasFruit,leafType,sizeCategory\n" +
            "olivo,Olivo,olivera|aceituno,Arbol,media,false,true,lanceolada,mediano\n" +
            "romero,Romero,romeru,Arbusto,fina,false,false,lineal,pequeno\n" +
            "rosal,Rosal,rosa,Arbusto,media,true,true,compuesta,mediano\n" +
            "encina,Encina,carrasca,Arbol,rugosa,false,true,coriacea,grande\n";
    }

    private static void SetupMinigameScene(PlantPhotoRelayMinigameConfig config)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureInputSystemUiEventSystem();
        MinigameSceneCameraUtility.EnsureFixedCamera(scene, config.VisualSettings.BackgroundColor);

        var sessionObject = new GameObject("PlantPhotoRelaySession", typeof(Unity.Netcode.NetworkObject), typeof(PlantPhotoRelayMinigameSession));
        var session = sessionObject.GetComponent<PlantPhotoRelayMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("plantPhotoRelayMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("PlantPhotoRelayCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = config.VisualSettings.BackgroundColor;

        var uiRoot = CreateUiObject("PlantPhotoRelayUI", safeAreaRoot.transform, typeof(PlantPhotoRelayMinigameUIController));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        Stretch(waitingPanel.GetComponent<RectTransform>(), new Vector2(220f, 700f), new Vector2(-220f, -700f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, "Esperando al resto del grupo.", 28, TextAlignmentOptions.Center);
        Stretch(waitingStatus.rectTransform, new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(36f, 36f), new Vector2(-36f, -36f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(24, 24, 24, 24);
        gameplayLayout.spacing = 12f;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childForceExpandHeight = false;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, config.DisplayName, 36, TextAlignmentOptions.Center);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        var topPanel = CreatePanel("TopPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        topPanel.AddComponent<LayoutElement>().preferredHeight = 220f;
        var topLayout = topPanel.AddComponent<VerticalLayoutGroup>();
        topLayout.padding = new RectOffset(18, 18, 18, 18);
        topLayout.spacing = 8f;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandHeight = false;

        var primaryStatusRow = CreateUiObject("PrimaryStatusRow", topPanel.transform, typeof(HorizontalLayoutGroup));
        primaryStatusRow.AddComponent<LayoutElement>().preferredHeight = 32f;
        var primaryStatusLayout = primaryStatusRow.GetComponent<HorizontalLayoutGroup>();
        primaryStatusLayout.spacing = 12f;
        primaryStatusLayout.childControlWidth = true;
        primaryStatusLayout.childControlHeight = true;
        primaryStatusLayout.childForceExpandWidth = true;
        primaryStatusLayout.childForceExpandHeight = false;

        var roundLabel = CreateText("RoundLabel", primaryStatusRow.transform, "Ronda: 1/3", 22, TextAlignmentOptions.Left);
        roundLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var phaseLabel = CreateText("PhaseLabel", primaryStatusRow.transform, "Fase: Pista", 22, TextAlignmentOptions.Left);
        phaseLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var secondaryStatusRow = CreateUiObject("SecondaryStatusRow", topPanel.transform, typeof(HorizontalLayoutGroup));
        secondaryStatusRow.AddComponent<LayoutElement>().preferredHeight = 32f;
        var secondaryStatusLayout = secondaryStatusRow.GetComponent<HorizontalLayoutGroup>();
        secondaryStatusLayout.spacing = 12f;
        secondaryStatusLayout.childControlWidth = true;
        secondaryStatusLayout.childControlHeight = true;
        secondaryStatusLayout.childForceExpandWidth = true;
        secondaryStatusLayout.childForceExpandHeight = false;

        var timerLabel = CreateText("TimerLabel", secondaryStatusRow.transform, "Tiempo: 00:12", 22, TextAlignmentOptions.Left);
        timerLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var roleLabel = CreateText("RoleLabel", secondaryStatusRow.transform, "Rol local: Observador", 20, TextAlignmentOptions.Left);
        roleLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var detailsRow = CreateUiObject("DetailsRow", topPanel.transform, typeof(HorizontalLayoutGroup));
        detailsRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var detailsLayout = detailsRow.GetComponent<HorizontalLayoutGroup>();
        detailsLayout.spacing = 12f;
        detailsLayout.childControlWidth = true;
        detailsLayout.childControlHeight = true;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = true;

        var clueContainer = CreateUiObject("ClueContainer", detailsRow.transform, typeof(VerticalLayoutGroup));
        clueContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var clueContainerLayout = clueContainer.GetComponent<VerticalLayoutGroup>();
        clueContainerLayout.padding = new RectOffset(0, 0, 0, 0);
        clueContainerLayout.spacing = 4f;
        clueContainerLayout.childControlWidth = true;
        clueContainerLayout.childControlHeight = true;
        clueContainerLayout.childForceExpandHeight = false;

        var clueLabel = CreateText("ClueLabel", clueContainer.transform, "Pista: ...", 20, TextAlignmentOptions.TopLeft);
        clueLabel.enableWordWrapping = true;
        clueLabel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var statusContainer = CreateUiObject("StatusContainer", detailsRow.transform, typeof(VerticalLayoutGroup));
        statusContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var statusContainerLayout = statusContainer.GetComponent<VerticalLayoutGroup>();
        statusContainerLayout.padding = new RectOffset(0, 0, 0, 0);
        statusContainerLayout.spacing = 4f;
        statusContainerLayout.childControlWidth = true;
        statusContainerLayout.childControlHeight = true;
        statusContainerLayout.childForceExpandHeight = false;

        var statusLabel = CreateText("StatusLabel", statusContainer.transform, "Estado compartido", 18, TextAlignmentOptions.TopLeft);
        statusLabel.enableWordWrapping = true;
        statusLabel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var centerArea = CreateUiObject("CenterArea", gameplayPanel.transform, typeof(VerticalLayoutGroup));
        var centerLayout = centerArea.GetComponent<VerticalLayoutGroup>();
        centerLayout.spacing = 12f;
        centerLayout.childControlWidth = true;
        centerLayout.childControlHeight = true;
        centerLayout.childForceExpandWidth = true;
        centerLayout.childForceExpandHeight = false;
        centerArea.AddComponent<LayoutElement>().flexibleHeight = 1f;
        centerArea.GetComponent<LayoutElement>().minHeight = 520f;

        var inputPanel = CreatePanel("InputPanel", centerArea.transform, config.VisualSettings.PanelColor);
        inputPanel.AddComponent<LayoutElement>().preferredHeight = 280f;
        var inputLayout = inputPanel.AddComponent<VerticalLayoutGroup>();
        inputLayout.padding = new RectOffset(18, 18, 18, 18);
        inputLayout.spacing = 8f;
        inputLayout.childControlWidth = true;
        inputLayout.childControlHeight = true;
        inputLayout.childForceExpandHeight = false;

        var helperLabel = CreateText("HelperLabel", inputPanel.transform, "Ayuda contextual", 18, TextAlignmentOptions.TopLeft);
        helperLabel.enableWordWrapping = true;
        helperLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        var inputField = CreateInputField("CommonNameInputField", inputPanel.transform, "Escribe un nombre comun...");
        inputField.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

        var suggestionPanel = CreatePanel("SuggestionPanel", inputPanel.transform, new Color(1f, 1f, 1f, 0.3f));
        suggestionPanel.AddComponent<LayoutElement>().preferredHeight = 140f;
        var suggestionLayout = suggestionPanel.AddComponent<VerticalLayoutGroup>();
        suggestionLayout.padding = new RectOffset(12, 12, 12, 12);
        suggestionLayout.spacing = 6f;
        suggestionLayout.childControlWidth = true;
        suggestionLayout.childControlHeight = true;
        suggestionLayout.childForceExpandHeight = false;
        var suggestionTemplateObject = CreateButton("SuggestionTemplate", suggestionPanel.transform, "Sugerencia", config.VisualSettings.SecondaryAccentColor);
        suggestionTemplateObject.AddComponent<PlantPhotoRelaySuggestionEntryView>();
        suggestionTemplateObject.gameObject.SetActive(false);
        var suggestionTemplate = suggestionTemplateObject.GetComponent<PlantPhotoRelaySuggestionEntryView>();

        var confirmPhotographerSelectionButtonObject = CreateButton("ConfirmPhotographerSelectionButton", inputPanel.transform, "Confirmar planta fotografiada", config.VisualSettings.AccentColor);
        confirmPhotographerSelectionButtonObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        var confirmPhotographerSelectionButton = confirmPhotographerSelectionButtonObject.GetComponent<Button>();
        var confirmPhotographerSelectionButtonLabel = confirmPhotographerSelectionButtonObject.GetComponentInChildren<TMP_Text>();

        var submitGuessButtonObject = CreateButton("SubmitGuessButton", inputPanel.transform, "Enviar adivinanza", config.VisualSettings.SecondaryAccentColor);
        submitGuessButtonObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        var submitGuessButton = submitGuessButtonObject.GetComponent<Button>();
        var submitGuessButtonLabel = submitGuessButtonObject.GetComponentInChildren<TMP_Text>();

        var photoPanel = CreatePanel("PhotoPanel", centerArea.transform, config.VisualSettings.PanelColor);
        photoPanel.AddComponent<LayoutElement>().flexibleHeight = 1f;
        photoPanel.GetComponent<LayoutElement>().minHeight = 260f;
        var photoLayout = photoPanel.AddComponent<VerticalLayoutGroup>();
        photoLayout.padding = new RectOffset(18, 18, 18, 18);
        photoLayout.spacing = 8f;
        photoLayout.childControlWidth = true;
        photoLayout.childControlHeight = true;
        photoLayout.childForceExpandHeight = false;

        var photoStateLabel = CreateText("PhotoStateLabel", photoPanel.transform, "Todavia no hay foto compartida.", 18, TextAlignmentOptions.Left);
        photoStateLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        var photoPreviewRoot = CreatePanel("PhotoPreviewRoot", photoPanel.transform, new Color(1f, 1f, 1f, 0.18f));
        photoPreviewRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
        photoPreviewRoot.GetComponent<LayoutElement>().minHeight = 240f;
        var photoPreviewImage = CreateUiObject("PhotoPreviewImage", photoPreviewRoot.transform, typeof(Image)).GetComponent<Image>();
        Stretch(photoPreviewImage.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        photoPreviewImage.color = new Color(1f, 1f, 1f, 0.92f);
        photoPreviewImage.enabled = false;

        var captureButtonObject = CreateButton("CaptureButton", photoPanel.transform, "Hacer foto", config.VisualSettings.AccentColor);
        captureButtonObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        var captureButton = captureButtonObject.GetComponent<Button>();
        var captureButtonLabel = captureButtonObject.GetComponentInChildren<TMP_Text>();

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform);
        tutorialPopup.gameObject.SetActive(false);
        var resultPopup = CreateResultPopup(uiRoot.transform);
        resultPopup.gameObject.SetActive(false);
        waitingPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        var failureFeedback = MinigameFailureFeedbackSetupUtility.CreateOrUpdateFailureFeedback(uiRoot, gameplayPanel);

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<PlantPhotoRelayMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        MinigameFailureFeedbackSetupUtility.AssignToUiController(serializedUiController, failureFeedback);
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("plantPhotoRelayMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("roundLabel").objectReferenceValue = roundLabel;
        serializedUiController.FindProperty("phaseLabel").objectReferenceValue = phaseLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("clueLabel").objectReferenceValue = clueLabel;
        serializedUiController.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedUiController.FindProperty("helperLabel").objectReferenceValue = helperLabel;
        serializedUiController.FindProperty("roleLabel").objectReferenceValue = roleLabel;
        serializedUiController.FindProperty("photoStateLabel").objectReferenceValue = photoStateLabel;
        serializedUiController.FindProperty("photoPreviewImage").objectReferenceValue = photoPreviewImage;
        serializedUiController.FindProperty("commonNameInputField").objectReferenceValue = inputField;
        serializedUiController.FindProperty("captureButton").objectReferenceValue = captureButton;
        serializedUiController.FindProperty("captureButtonLabel").objectReferenceValue = captureButtonLabel;
        serializedUiController.FindProperty("confirmPhotographerSelectionButton").objectReferenceValue = confirmPhotographerSelectionButton;
        serializedUiController.FindProperty("confirmPhotographerSelectionButtonLabel").objectReferenceValue = confirmPhotographerSelectionButtonLabel;
        serializedUiController.FindProperty("submitGuessButton").objectReferenceValue = submitGuessButton;
        serializedUiController.FindProperty("submitGuessButtonLabel").objectReferenceValue = submitGuessButtonLabel;
        serializedUiController.FindProperty("suggestionRoot").objectReferenceValue = suggestionPanel.transform;
        serializedUiController.FindProperty("suggestionTemplate").objectReferenceValue = suggestionTemplate;
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
        var sceneNames = serializedCoordinator.FindProperty("miniGameSceneNames");
        sceneNames.arraySize = Math.Max(sceneNames.arraySize, MinigameIndex + 1);
        sceneNames.GetArrayElementAtIndex(MinigameIndex).stringValue = MinigameSceneName;
        serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(coordinator);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupMainMapScene(CoopMinigameCatalogConfig _)
    {
        CoopMinigameSetupEditorUtility.RefreshMainMapMinigameLauncher();
    }

    private static void UpdateBuildSettings()
    {
        var requiredPaths = new[]
        {
            "Assets/Scenes/UJI.unity",
            "Assets/Scenes/Lobby.unity",
            MinigameScenePath
        };

        var existingScenes = EditorBuildSettings.scenes;
        var buildSceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(existingScenes);
        for (var index = 0; index < requiredPaths.Length; index++)
        {
            var scenePath = requiredPaths[index];
            var alreadyExists = false;
            for (var existingIndex = 0; existingIndex < buildSceneList.Count; existingIndex++)
            {
                if (string.Equals(buildSceneList[existingIndex].path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                buildSceneList.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = buildSceneList.ToArray();
    }

    private static string TryBuildCsvTemplateFromCollaborativeCatalog(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return string.Empty;
        }

        var rows = ParseRows(csvContent);
        if (rows.Count <= 1)
        {
            return string.Empty;
        }

        var headerMap = BuildHeaderMap(rows[0]);
        if (!headerMap.ContainsKey("plantId") ||
            !headerMap.ContainsKey("commonName") ||
            !headerMap.ContainsKey("synonyms") ||
            !headerMap.ContainsKey("surfaceRoughness") ||
            !headerMap.ContainsKey("fruitCategory") ||
            !headerMap.ContainsKey("leafType") ||
            !headerMap.ContainsKey("plantType"))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("commonNameCanonical,displayCommonName,acceptedCommonNameVariants,plantType,surfaceTexture,hasThorns,hasFruit,leafType,sizeCategory");

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsEmptyRow(row))
            {
                continue;
            }

            var canonical = GetCell(row, headerMap, "plantId").Trim();
            var displayCommonName = GetCell(row, headerMap, "commonName").Trim();
            var synonyms = NormalizeVariantList(GetCell(row, headerMap, "synonyms"));
            var plantType = NormalizeDisplayValue(GetCell(row, headerMap, "plantType"));
            var surfaceTexture = NormalizeDisplayValue(GetCell(row, headerMap, "surfaceRoughness"));
            var leafType = NormalizeDisplayValue(GetCell(row, headerMap, "leafType"));
            var hasFruit = InferHasFruit(GetCell(row, headerMap, "fruitCategory"));
            var hasThorns = InferHasThorns(displayCommonName, synonyms);
            var sizeCategory = InferSizeCategory(plantType);

            if (string.IsNullOrWhiteSpace(canonical) || string.IsNullOrWhiteSpace(displayCommonName))
            {
                continue;
            }

            builder.Append(CsvEscape(canonical)).Append(',')
                .Append(CsvEscape(displayCommonName)).Append(',')
                .Append(CsvEscape(synonyms)).Append(',')
                .Append(CsvEscape(plantType)).Append(',')
                .Append(CsvEscape(surfaceTexture)).Append(',')
                .Append(hasThorns ? "true" : "false").Append(',')
                .Append(hasFruit ? "true" : "false").Append(',')
                .Append(CsvEscape(leafType)).Append(',')
                .Append(CsvEscape(sizeCategory)).Append('\n');
        }

        return builder.ToString();
    }

    private static string NormalizeVariantList(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return string.Join("|", rawValue
            .Split('|')
            .Select(value => NormalizeDisplayValue(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static bool InferHasFruit(string rawFruitCategory)
    {
        var normalized = NormalizeDisplayValue(rawFruitCategory);
        return !string.Equals(normalized, "No", StringComparison.OrdinalIgnoreCase);
    }

    private static bool InferHasThorns(string displayCommonName, string synonyms)
    {
        var merged = $"{displayCommonName}|{synonyms}".ToLowerInvariant();
        return merged.Contains("espino", StringComparison.Ordinal) ||
               merged.Contains("acebo", StringComparison.Ordinal) ||
               merged.Contains("chumbera", StringComparison.Ordinal) ||
               merged.Contains("cambr", StringComparison.Ordinal) ||
               merged.Contains("nopal", StringComparison.Ordinal);
    }

    private static string InferSizeCategory(string plantType)
    {
        if (string.IsNullOrWhiteSpace(plantType))
        {
            return "mediano";
        }

        switch (plantType.Trim().ToLowerInvariant())
        {
            case "arbusto":
            case "suculenta":
                return "pequeno";
            case "trepadora":
            case "palmera":
                return "mediano";
            case "arbol":
            case "árbol":
                return "grande";
            default:
                return "mediano";
        }
    }

    private static string NormalizeDisplayValue(string rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue)
            ? string.Empty
            : rawValue.Trim().Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string CsvEscape(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static System.Collections.Generic.List<System.Collections.Generic.List<string>> ParseRows(string csvContent)
    {
        var rows = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();
        var currentRow = new System.Collections.Generic.List<string>();
        var currentField = string.Empty;
        var insideQuotes = false;

        for (var index = 0; index < csvContent.Length; index++)
        {
            var character = csvContent[index];
            if (insideQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                    {
                        currentField += '"';
                        index++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    currentField += character;
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    insideQuotes = true;
                    break;
                case ',':
                    currentRow.Add(currentField);
                    currentField = string.Empty;
                    break;
                case '\r':
                    break;
                case '\n':
                    currentRow.Add(currentField);
                    rows.Add(currentRow);
                    currentRow = new System.Collections.Generic.List<string>();
                    currentField = string.Empty;
                    break;
                default:
                    currentField += character;
                    break;
            }
        }

        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField);
            rows.Add(currentRow);
        }

        return rows;
    }

    private static System.Collections.Generic.Dictionary<string, int> BuildHeaderMap(System.Collections.Generic.IReadOnlyList<string> headerRow)
    {
        var map = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headerRow.Count; index++)
        {
            var key = headerRow[index] == null ? string.Empty : headerRow[index].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
            {
                map.Add(key, index);
            }
        }

        return map;
    }

    private static string GetCell(System.Collections.Generic.IReadOnlyList<string> row, System.Collections.Generic.IReadOnlyDictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var index))
        {
            return string.Empty;
        }

        return index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
    }

    private static bool IsEmptyRow(System.Collections.Generic.IReadOnlyList<string> row)
    {
        for (var index = 0; index < row.Count; index++)
        {
            if (!string.IsNullOrWhiteSpace(row[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureInputSystemUiEventSystem()
    {
        var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static Canvas CreateCanvas(string name)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        var gameObject = new GameObject(name, PrependRectTransform(components));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = CreateUiObject(name, parent, typeof(Image));
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        return text;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color color)
    {
        var buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        buttonObject.GetComponent<Image>().color = color;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        var labelObject = CreateUiObject("Label", buttonObject.transform, typeof(TextMeshProUGUI));
        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 24f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.zero);
        return buttonObject;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText)
    {
        var root = CreateUiObject(name, parent, typeof(Image), typeof(TMP_InputField));
        root.GetComponent<Image>().color = Color.white;

        var textViewport = CreateUiObject("Text Area", root.transform, typeof(RectMask2D));
        Stretch(textViewport.GetComponent<RectTransform>(), new Vector2(16f, 8f), new Vector2(-16f, -8f));

        var textComponent = CreateText("Text", textViewport.transform, string.Empty, 24f, TextAlignmentOptions.Left);
        Stretch(textComponent.rectTransform, Vector2.zero, Vector2.zero);
        var placeholder = CreateText("Placeholder", textViewport.transform, placeholderText, 24f, TextAlignmentOptions.Left);
        placeholder.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.zero);

        var inputField = root.GetComponent<TMP_InputField>();
        inputField.textViewport = textViewport.GetComponent<RectTransform>();
        inputField.textComponent = textComponent as TextMeshProUGUI;
        inputField.placeholder = placeholder;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        return inputField;
    }

    private static TutorialPopupController CreateTutorialPopup(Transform parent)
    {
        return TutorialPopupPrefabUtility.InstantiateTutorialPopup(parent);
    }

    private static MinigameResultView CreateResultPopup(Transform parent)
    {
        var popupRoot = CreateUiObject("ResultPopup", parent, typeof(MinigameResultView));
        Stretch(popupRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", popupRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);
        backgroundImage.raycastTarget = false;

        var contentPanel = CreatePanel("ContentPanel", popupRoot.transform, new Color(1f, 1f, 1f, 0.96f));
        var contentRect = contentPanel.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(
            popupRoot.GetComponent<RectTransform>(),
            0.86f,
            0.42f,
            new Vector2(320f, 320f),
            new Vector2(720f, 560f),
            new Vector2(32f, 32f));

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var title = CreateText("TitleLabel", contentPanel.transform, "Resultado", 34, TextAlignmentOptions.Center);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        var score = CreateText("ScoreLabel", contentPanel.transform, "0/10", 48, TextAlignmentOptions.Center);
        score.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;

        var summary = CreateText("SummaryLabel", contentPanel.transform, "Aciertos: 0\nErrores: 0", 24, TextAlignmentOptions.Center);
        summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;

        var returnButtonObject = CreateButton("ReturnButton", contentPanel.transform, "Volver", new Color(0.23f, 0.45f, 0.33f, 1f));
        returnButtonObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var waitingHostLabel = CreateText("WaitingHostLabel", contentPanel.transform, "Esperando a que el host continue.", 22, TextAlignmentOptions.Center);
        waitingHostLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        var resultView = popupRoot.GetComponent<MinigameResultView>();
        var serializedResultView = new SerializedObject(resultView);
        serializedResultView.FindProperty("titleLabel").objectReferenceValue = title;
        serializedResultView.FindProperty("scoreLabel").objectReferenceValue = score;
        serializedResultView.FindProperty("summaryLabel").objectReferenceValue = summary;
        serializedResultView.FindProperty("returnButton").objectReferenceValue = returnButtonObject.GetComponent<Button>();
        serializedResultView.FindProperty("returnButtonLabel").objectReferenceValue = returnButtonObject.GetComponentInChildren<TMP_Text>();
        serializedResultView.FindProperty("waitingHostLabel").objectReferenceValue = waitingHostLabel;
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();
        return resultView;
    }

    private static Type[] PrependRectTransform(Type[] extraComponents)
    {
        var result = new Type[extraComponents.Length + 1];
        result[0] = typeof(RectTransform);
        for (var index = 0; index < extraComponents.Length; index++)
        {
            result[index + 1] = extraComponents[index];
        }

        return result;
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
