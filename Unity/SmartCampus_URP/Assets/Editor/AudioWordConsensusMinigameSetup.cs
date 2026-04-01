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
using SmartCampus.Coop.Minigames.AudioWordConsensus;

public static class AudioWordConsensusMinigameSetup
{
    private const string RootFolder = "Assets/CoopMinigames";
    private const string ConfigFolder = RootFolder + "/Configs";
    private const string SceneFolder = "Assets/Scenes";
    private const string TutorialConfigPath = ConfigFolder + "/AudioWordConsensusTutorialContent.asset";
    private const string MinigameConfigPath = ConfigFolder + "/AudioWordConsensusMinigameConfig.asset";
    private const string CatalogConfigPath = ConfigFolder + "/CoopMinigameCatalog.asset";
    private const string MinigameScenePath = SceneFolder + "/AudioWordConsensusMinigame.unity";
    private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
    private const string MainMapScenePath = SceneFolder + "/UJI.unity";
    private const string MinigameSceneName = "AudioWordConsensusMinigame";
    private const int MinigameIndex = 2;

    [MenuItem("Tools/Coop/Setup Audio Word Consensus Minigame")]
    public static void SetupAudioWordConsensusMinigame()
    {
        EnsureFolders();

        var tutorialContent = CreateOrUpdateTutorialContent();
        var minigameConfig = CreateOrUpdateMinigameConfig(tutorialContent);
        var catalogConfig = CreateOrUpdateCatalogConfig();

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
        serializedObject.FindProperty("title").stringValue = "Sonido y consenso";
        serializedObject.FindProperty("subtitle").stringValue = "Una persona escucha, el resto compara palabras";
        serializedObject.FindProperty("bodyText").stringValue =
            "En cada ronda un dispositivo solo puede reproducir un sonido. Ese jugador no recibe ninguna palabra.\n\n" +
            "El resto de dispositivos recibe una palabra distinta. Solo una coincide con el sonido reproducido.\n\n" +
            "Debatid rapidamente y decidid quien debe pulsar. El minijuego termina cuando todos han sido emisor una vez o cuando se agota el tiempo.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static AudioWordConsensusMinigameConfig CreateOrUpdateMinigameConfig(MinigameTutorialContentConfig tutorialContent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<AudioWordConsensusMinigameConfig>(MinigameConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            AssetDatabase.CreateAsset(asset, MinigameConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        serializedObject.FindProperty("displayName").stringValue = "Minijuego 2 - Sonido y consenso";
        serializedObject.FindProperty("tutorialContent").objectReferenceValue = tutorialContent;
        serializedObject.FindProperty("successMessage").stringValue = "Rondas completadas";
        serializedObject.FindProperty("returnToMapButtonLabel").stringValue = "Volver al mapa";
        serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
        serializedObject.FindProperty("timeLimitSeconds").floatValue = 120f;
        serializedObject.FindProperty("feedbackDurationSeconds").floatValue = 1.2f;
        serializedObject.FindProperty("timeoutMessage").stringValue = "Tiempo agotado";
        serializedObject.FindProperty("missingAudioClipLabel").stringValue = "Asigna un AudioClip en el asset";

        var roundDefinitions = serializedObject.FindProperty("roundDefinitions");
        roundDefinitions.arraySize = 6;
        for (var index = 0; index < roundDefinitions.arraySize; index++)
        {
            var round = roundDefinitions.GetArrayElementAtIndex(index);
            round.FindPropertyRelative("promptLabel").stringValue = $"Sonido {index + 1}";
            round.FindPropertyRelative("correctWord").stringValue = $"Palabra correcta {index + 1}";

            var distractorWords = round.FindPropertyRelative("distractorWords");
            distractorWords.arraySize = 5;
            for (var distractorIndex = 0; distractorIndex < distractorWords.arraySize; distractorIndex++)
            {
                distractorWords.GetArrayElementAtIndex(distractorIndex).stringValue = $"Distractor {index + 1}-{distractorIndex + 1}";
            }
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static CoopMinigameCatalogConfig CreateOrUpdateCatalogConfig()
    {
        var asset = AssetDatabase.LoadAssetAtPath<CoopMinigameCatalogConfig>(CatalogConfigPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CoopMinigameCatalogConfig>();
            AssetDatabase.CreateAsset(asset, CatalogConfigPath);
        }

        var serializedObject = new SerializedObject(asset);
        var entries = serializedObject.FindProperty("entries");
        var targetIndex = FindCatalogEntryIndex(entries, MinigameIndex);
        if (targetIndex < 0)
        {
            targetIndex = entries.arraySize;
            entries.InsertArrayElementAtIndex(targetIndex);
        }

        var entry = entries.GetArrayElementAtIndex(targetIndex);
        entry.FindPropertyRelative("minigameIndex").intValue = MinigameIndex;
        entry.FindPropertyRelative("displayName").stringValue = "Minijuego 2 - Sonido y consenso";
        entry.FindPropertyRelative("description").stringValue = "Un dispositivo reproduce un sonido y el resto compara palabras diferentes para decidir en grupo cual debe pulsarse.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static int FindCatalogEntryIndex(SerializedProperty entries, int minigameIndex)
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

    private static void SetupMinigameScene(AudioWordConsensusMinigameConfig config)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateEventSystemIfMissing();

        var sessionObject = new GameObject("AudioWordConsensusSession", typeof(Unity.Netcode.NetworkObject), typeof(AudioWordConsensusMinigameSession));
        var session = sessionObject.GetComponent<AudioWordConsensusMinigameSession>();
        var serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("audioWordConsensusMinigameConfig").objectReferenceValue = config;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        var canvas = CreateCanvas("AudioWordConsensusCanvas");
        var safeAreaRoot = CreateUiObject("SafeAreaRoot", canvas.transform, typeof(SafeAreaFitter));
        Stretch(safeAreaRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var background = CreateUiObject("Background", safeAreaRoot.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = config.VisualSettings.BackgroundColor;

        var uiRoot = CreateUiObject("AudioWordConsensusUI", safeAreaRoot.transform, typeof(AudioWordConsensusMinigameUIController), typeof(AudioSource));
        Stretch(uiRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var waitingPanel = CreatePanel("WaitingPanel", uiRoot.transform, new Color(0.12f, 0.17f, 0.21f, 0.86f));
        waitingPanel.AddComponent<ResponsivePanelLayoutController>().Configure(canvas.GetComponent<RectTransform>(), 0.82f, 0.18f, new Vector2(280f, 180f), new Vector2(720f, 260f), new Vector2(24f, 24f));
        var waitingStatus = CreateText("WaitingStatus", waitingPanel.transform, font, "Esperando al resto del grupo.", 28, TextAnchor.MiddleCenter);
        Stretch(waitingStatus.GetComponent<RectTransform>(), new Vector2(28f, 28f), new Vector2(-28f, -28f));

        var gameplayPanel = CreateUiObject("GameplayPanel", uiRoot.transform, typeof(Image), typeof(VerticalLayoutGroup));
        Stretch(gameplayPanel.GetComponent<RectTransform>(), new Vector2(32f, 32f), new Vector2(-32f, -32f));
        gameplayPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        var gameplayLayout = gameplayPanel.GetComponent<VerticalLayoutGroup>();
        gameplayLayout.padding = new RectOffset(24, 24, 24, 24);
        gameplayLayout.spacing = 18f;
        gameplayLayout.childControlHeight = true;
        gameplayLayout.childControlWidth = true;
        gameplayLayout.childForceExpandHeight = false;

        var titleLabel = CreateText("TitleLabel", gameplayPanel.transform, font, config.DisplayName, 42, TextAnchor.MiddleCenter);
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var statusPanel = CreatePanel("StatusPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        statusPanel.AddComponent<LayoutElement>().preferredHeight = 300f;
        var statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(24, 24, 20, 20);
        statusLayout.spacing = 10f;
        statusLayout.childControlHeight = true;
        statusLayout.childControlWidth = true;
        statusLayout.childForceExpandHeight = false;

        var roundLabel = CreateText("RoundLabel", statusPanel.transform, font, "Ronda: 1/1", 24, TextAnchor.MiddleLeft);
        roundLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var timerLabel = CreateText("TimerLabel", statusPanel.transform, font, "Tiempo restante: 02:00", 24, TextAnchor.MiddleLeft);
        timerLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var scoreLabel = CreateText("ScoreLabel", statusPanel.transform, font, "Aciertos: 0   Fallos: 0", 24, TextAnchor.MiddleLeft);
        scoreLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        var statusLabel = CreateText("StatusLabel", statusPanel.transform, font, "Preparando la ronda cooperativa.", 22, TextAnchor.UpperLeft);
        statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
        var roleLabel = CreateText("RoleLabel", statusPanel.transform, font, "Tu rol se mostrara aqui.", 20, TextAnchor.UpperLeft);
        roleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var interactionPanel = CreatePanel("InteractionPanel", gameplayPanel.transform, config.VisualSettings.PanelColor);
        interactionPanel.AddComponent<LayoutElement>().flexibleHeight = 1f;
        interactionPanel.GetComponent<LayoutElement>().minHeight = 420f;
        var interactionLayout = interactionPanel.AddComponent<VerticalLayoutGroup>();
        interactionLayout.padding = new RectOffset(24, 24, 24, 24);
        interactionLayout.spacing = 20f;
        interactionLayout.childControlHeight = true;
        interactionLayout.childControlWidth = true;
        interactionLayout.childForceExpandHeight = false;

        var localWordLabel = CreateText("LocalWordLabel", interactionPanel.transform, font, "Palabra local", 30, TextAnchor.MiddleCenter);
        localWordLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;

        var playSoundButton = CreateButton("PlaySoundButton", interactionPanel.transform, font, "Reproducir sonido", 24, config.VisualSettings.PrimaryButtonColor);
        playSoundButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
        var submitWordButton = CreateButton("SubmitWordButton", interactionPanel.transform, font, "Pulsar palabra", 24, config.VisualSettings.ReceiverButtonColor);
        submitWordButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        var tutorialPopup = CreateTutorialPopup(uiRoot.transform, font);
        tutorialPopup.gameObject.SetActive(false);
        var resultPopup = CreateResultPopup(uiRoot.transform, font);
        resultPopup.gameObject.SetActive(false);

        var serializedUiController = new SerializedObject(uiRoot.GetComponent<AudioWordConsensusMinigameUIController>());
        serializedUiController.FindProperty("minigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("tutorialPopupController").objectReferenceValue = tutorialPopup;
        serializedUiController.FindProperty("minigameResultView").objectReferenceValue = resultPopup;
        serializedUiController.FindProperty("waitingPanel").objectReferenceValue = waitingPanel;
        serializedUiController.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
        serializedUiController.FindProperty("waitingStatusLabel").objectReferenceValue = waitingStatus;
        serializedUiController.FindProperty("audioWordConsensusMinigameSession").objectReferenceValue = session;
        serializedUiController.FindProperty("localAudioSource").objectReferenceValue = uiRoot.GetComponent<AudioSource>();
        serializedUiController.FindProperty("titleLabel").objectReferenceValue = titleLabel;
        serializedUiController.FindProperty("roundLabel").objectReferenceValue = roundLabel;
        serializedUiController.FindProperty("timerLabel").objectReferenceValue = timerLabel;
        serializedUiController.FindProperty("scoreLabel").objectReferenceValue = scoreLabel;
        serializedUiController.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedUiController.FindProperty("roleLabel").objectReferenceValue = roleLabel;
        serializedUiController.FindProperty("localWordLabel").objectReferenceValue = localWordLabel;
        serializedUiController.FindProperty("playSoundButton").objectReferenceValue = playSoundButton.GetComponent<Button>();
        serializedUiController.FindProperty("playSoundButtonLabel").objectReferenceValue = playSoundButton.GetComponentInChildren<Text>();
        serializedUiController.FindProperty("submitWordButton").objectReferenceValue = submitWordButton.GetComponent<Button>();
        serializedUiController.FindProperty("submitWordButtonLabel").objectReferenceValue = submitWordButton.GetComponentInChildren<Text>();
        serializedUiController.ApplyModifiedPropertiesWithoutUndo();

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
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupMainMapScene(CoopMinigameCatalogConfig catalogConfig)
    {
        var scene = EditorSceneManager.OpenScene(MainMapScenePath, OpenSceneMode.Single);
        var launcherController = UnityEngine.Object.FindFirstObjectByType<CoopMinigameLauncherUIController>(FindObjectsInactive.Include);
        if (launcherController == null)
        {
            throw new InvalidOperationException("La escena del mapa principal necesita un CoopMinigameLauncherUIController.");
        }

        var serializedLauncherController = new SerializedObject(launcherController);
        serializedLauncherController.FindProperty("minigameCatalogConfig").objectReferenceValue = catalogConfig;
        serializedLauncherController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(launcherController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.92f, 0.88f, new Vector2(320f, 460f), new Vector2(860f, 1180f), new Vector2(24f, 24f));
        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        var closeButton = CreateButton("CloseButton", contentPanel.transform, font, "X", 20, new Color(0.21f, 0.42f, 0.46f, 1f));
        var title = CreateText("TitleLabel", contentPanel.transform, font, "Tutorial", 36, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        var subtitle = CreateText("SubtitleLabel", contentPanel.transform, font, "Subtitulo", 24, TextAnchor.MiddleCenter);
        subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        var body = CreateText("BodyLabel", contentPanel.transform, font, "Contenido del tutorial", 22, TextAnchor.UpperLeft);
        body.gameObject.AddComponent<LayoutElement>().preferredHeight = 220f;
        var illustration = CreateUiObject("Illustration", contentPanel.transform, typeof(Image));
        illustration.AddComponent<LayoutElement>().preferredHeight = 220f;
        var videoSurface = CreateUiObject("VideoSurface", contentPanel.transform, typeof(RawImage));
        videoSurface.AddComponent<LayoutElement>().preferredHeight = 220f;
        var customContentRoot = CreateUiObject("CustomContentRoot", contentPanel.transform);
        customContentRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
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
        contentPanel.AddComponent<ResponsivePanelLayoutController>().Configure(popupRoot.GetComponent<RectTransform>(), 0.84f, 0.42f, new Vector2(320f, 320f), new Vector2(700f, 560f), new Vector2(24f, 24f));
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
        serializedResultView.FindProperty("returnButtonLabel").objectReferenceValue = returnButton.GetComponentInChildren<Text>();
        serializedResultView.FindProperty("waitingHostLabel").objectReferenceValue = waitingLabel;
        serializedResultView.FindProperty("successfulActionsLabel").stringValue = "Rondas acertadas";
        serializedResultView.FindProperty("failedActionsLabel").stringValue = "Rondas falladas";
        serializedResultView.ApplyModifiedPropertiesWithoutUndo();
        return resultView;
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
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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

    private static GameObject CreateButton(string name, Transform parent, Font font, string label, int fontSize, Color backgroundColor)
    {
        var buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = image.color * 1.1f;
        colors.pressedColor = image.color * 0.9f;
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
}
