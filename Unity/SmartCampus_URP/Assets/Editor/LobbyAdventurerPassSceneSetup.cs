using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyAdventurerPassSceneSetup
{
    [MenuItem("SmartCampus/Lobby/Rebuild Adventurer Pass Panel")]
    public static void RebuildFromMenu()
    {
        Debug.Log(Run());
    }

    public static string Run()
    {
        var prefabsFolder = EnsureFolder("Assets/Prefabs", "Lobby");
        var markerFolder = EnsureFolder("Assets", "MarkerAppearance");
        var shapeFolder = EnsureFolder(markerFolder, "Shapes");
        var configFolder = EnsureFolder("Assets", "ScriptableObjects");
        configFolder = EnsureFolder(configFolder, "Lobby");
        var artFolder = EnsureFolder("Assets", "GeneratedUI");
        artFolder = EnsureFolder(artFolder, "Lobby");

        var previewMaterial = LoadOrCreatePreviewMaterial($"{artFolder}/LobbyMarkerPreview.mat");
        var cubePrefab = CreateShapePrefab(shapeFolder, PrimitiveType.Cube, "MarkerShapeCube", previewMaterial);
        var spherePrefab = CreateShapePrefab(shapeFolder, PrimitiveType.Sphere, "MarkerShapeSphere", previewMaterial);
        var cylinderPrefab = CreateShapePrefab(shapeFolder, PrimitiveType.Cylinder, "MarkerShapeCylinder", previewMaterial);
        var renderTexture = LoadOrCreatePreviewTexture($"{artFolder}/LobbyMarkerPreview.renderTexture");
        var catalog = LoadOrCreateCatalog(
            $"{configFolder}/PlayerMarkerAppearanceCatalogConfig.asset",
            cubePrefab,
            spherePrefab,
            cylinderPrefab);

        var lobbyScene = EditorSceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
        if (!lobbyScene.isLoaded)
        {
            lobbyScene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);
        }

        SanitizeLobbyButtonPrefab();
        SanitizeLobbyPanelPrefabs();

        var surfacePanel = GameObject.Find("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel");
        if (surfacePanel == null)
        {
            return "SurfacePanel not found in Lobby scene.";
        }

        var coopSession = GameObject.Find("CoopSession");
        if (coopSession == null)
        {
            return "CoopSession root not found in Lobby scene.";
        }

        var profileService = coopSession.GetComponent<LocalPlayerMarkerProfileService>();
        if (profileService == null)
        {
            profileService = Undo.AddComponent<LocalPlayerMarkerProfileService>(coopSession);
        }

        var profileSerialized = new SerializedObject(profileService);
        profileSerialized.FindProperty("appearanceCatalog").objectReferenceValue = catalog;
        profileSerialized.FindProperty("defaultDisplayName").stringValue = "Aventurero";
        profileSerialized.FindProperty("maxDisplayNameLength").intValue = 18;
        profileSerialized.ApplyModifiedPropertiesWithoutUndo();
        profileService.Reload();
        profileService.SetDisplayName("Aventurero");
        profileService.SetShapeId(catalog.DefaultShapeId);
        profileService.SetColorId(catalog.DefaultColorId);

        var existingPanel = surfacePanel.transform.Find("AdventurerPassPanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel.gameObject);
        }

        var panel = BuildPanel(surfacePanel.transform, renderTexture, catalog, profileService);
        RebindMultiplayerMenuControllerReferences();
        var panelPrefabPath = $"{prefabsFolder}/LobbyAdventurerPassPanel.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(panel, panelPrefabPath, InteractionMode.AutomatedAction);
        RebindLobbySceneButtons();

        EditorSceneManager.MarkSceneDirty(lobbyScene);
        EditorSceneManager.SaveScene(lobbyScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"Created/updated {prefab.name} with catalog {catalog.name}.";
    }

    private static GameObject BuildPanel(
        Transform surfacePanel,
        RenderTexture renderTexture,
        PlayerMarkerAppearanceCatalogConfig catalog,
        LocalPlayerMarkerProfileService profileService)
    {
        var panel = new GameObject(
            "AdventurerPassPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement),
            typeof(VerticalLayoutGroup),
            typeof(LobbyAdventurerPassUIController));
        panel.transform.SetParent(surfacePanel, false);
        panel.transform.SetSiblingIndex(surfacePanel.Find("ScrollFrame").GetSiblingIndex() + 1);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(0f, 392f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.95f, 0.9f, 0.82f, 0.94f);
        panelImage.raycastTarget = false;

        var panelLayoutElement = panel.GetComponent<LayoutElement>();
        panelLayoutElement.preferredHeight = 392f;
        panelLayoutElement.minHeight = 352f;

        var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(28, 28, 24, 24);
        panelLayout.spacing = 18f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        var titleColor = new Color(0.3f, 0.23f, 0.17f, 1f);
        var bodyColor = new Color(0.37f, 0.29f, 0.22f, 1f);

        CreateText(panel.transform, "SectionTitle", "Pase de aventurero", 42, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        CreateText(panel.transform, "SectionSubtitle", "Elige tu nombre, color y forma antes de salir al mapa.", 24, FontStyles.Normal, TextAlignmentOptions.Left, bodyColor);

        var bodyRow = new GameObject("BodyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        bodyRow.transform.SetParent(panel.transform, false);
        var bodyRowLayout = bodyRow.GetComponent<HorizontalLayoutGroup>();
        bodyRowLayout.spacing = 20f;
        bodyRowLayout.childControlHeight = true;
        bodyRowLayout.childControlWidth = true;
        bodyRowLayout.childForceExpandHeight = true;
        bodyRowLayout.childForceExpandWidth = false;
        bodyRowLayout.childAlignment = TextAnchor.UpperLeft;
        bodyRow.GetComponent<LayoutElement>().flexibleHeight = 1f;

        var previewColumn = CreateCardColumn(bodyRow.transform, "PreviewColumn", new Color(0.36f, 0.59f, 0.85f, 0.12f), 320f);
        CreateText(previewColumn.transform, "PreviewTitle", "Tu credencial visual", 24, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        var rawImage = CreatePreviewFrame(previewColumn.transform, renderTexture);
        var previewFrameLayout = rawImage.transform.parent.GetComponent<LayoutElement>();
        previewFrameLayout.preferredHeight = 300f;

        var customizeColumn = CreateCardColumn(bodyRow.transform, "CustomizeColumn", new Color(1f, 1f, 1f, 0.24f), 500f, flexibleWidth: 1f);
        CreateText(customizeColumn.transform, "IdentityTitle", "Nombre del aventurero", 26, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        CreateText(customizeColumn.transform, "IdentityHint", "Se guardara en tu perfil local del mapa.", 20, FontStyles.Normal, TextAlignmentOptions.Left, bodyColor);
        var inputField = CreatePlayerNameInput(customizeColumn.transform);

        CreateText(customizeColumn.transform, "ShapeTitle", "Forma del marcador", 26, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        var shapeOptions = CreateShapeButtons(customizeColumn.transform);
        CreateText(customizeColumn.transform, "ColorTitle", "Color del marcador", 26, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        var colorOptions = CreateColorButtons(customizeColumn.transform, catalog);

        var previewRoot = CreatePreviewRig(panel.transform, renderTexture, out var previewCamera);
        ApplyControllerReferences(panel, profileService, catalog, inputField, rawImage, previewCamera, previewRoot, shapeOptions, colorOptions);
        return panel;
    }

    private static TMP_InputField CreatePlayerNameInput(Transform parent)
    {
        var menuController = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
        var joinField = typeof(MultiplayerMenuController)
            .GetField("joinCodeInput", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(menuController) as TMP_InputField;
        if (joinField == null)
        {
            throw new MissingReferenceException("MultiplayerMenuController.joinCodeInput not found.");
        }

        var inputClone = Object.Instantiate(joinField.gameObject, parent, false);
        inputClone.name = "PlayerNameInput";
        var inputRect = inputClone.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(0f, 72f);

        var inputField = inputClone.GetComponent<TMP_InputField>();
        inputField.characterLimit = 18;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        var placeholder = inputClone.transform.Find("Text Area/Placeholder")?.GetComponent<TextMeshProUGUI>();
        if (placeholder != null)
        {
            placeholder.text = "Escribe tu nombre";
        }

        var inputText = inputClone.transform.Find("Text Area/Text")?.GetComponent<TextMeshProUGUI>();
        if (inputText != null)
        {
            inputText.fontSize = 30f;
        }

        return inputField;
    }

    private static RawImage CreatePreviewFrame(Transform parent, RenderTexture renderTexture)
    {
        var previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        previewFrame.transform.SetParent(parent, false);
        previewFrame.GetComponent<Image>().color = new Color(0.26f, 0.44f, 0.62f, 0.9f);
        previewFrame.GetComponent<Image>().raycastTarget = false;
        previewFrame.GetComponent<LayoutElement>().preferredHeight = 210f;

        var previewImage = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        previewImage.transform.SetParent(previewFrame.transform, false);
        var imageRect = previewImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(10f, 10f);
        imageRect.offsetMax = new Vector2(-10f, -10f);

        var rawImage = previewImage.GetComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        return rawImage;
    }

    private static Transform CreatePreviewRig(Transform panelRoot, RenderTexture renderTexture, out Camera previewCamera)
    {
        var previewRig = new GameObject("PreviewRig");
        previewRig.transform.SetParent(panelRoot, false);
        previewRig.transform.localPosition = new Vector3(10000f, 10000f, 10000f);

        var previewRoot = new GameObject("PreviewRoot");
        previewRoot.transform.SetParent(previewRig.transform, false);

        var lightRoot = new GameObject("PreviewLight", typeof(Light));
        lightRoot.transform.SetParent(previewRig.transform, false);
        lightRoot.transform.localPosition = new Vector3(0f, 1.8f, -1.2f);
        lightRoot.transform.localRotation = Quaternion.Euler(40f, -25f, 0f);
        var light = lightRoot.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.96f, 0.9f, 1f);

        var cameraRoot = new GameObject("PreviewCamera", typeof(Camera));
        cameraRoot.transform.SetParent(previewRig.transform, false);
        cameraRoot.transform.localPosition = new Vector3(0f, 0f, -4.5f);
        previewCamera = cameraRoot.GetComponent<Camera>();
        previewCamera.targetTexture = renderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.2f, 0.37f, 0.55f, 1f);
        previewCamera.fieldOfView = 28f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 20f;
        previewCamera.enabled = true;
        return previewRoot.transform;
    }

    private static LobbyMarkerShapeOptionView[] CreateShapeButtons(Transform parent)
    {
        var shapeData = new[]
        {
            new { Id = "cube", Name = "Cubo" },
            new { Id = "sphere", Name = "Esfera" },
            new { Id = "cylinder", Name = "Cilindro" }
        };

        var row = new GameObject("ShapeOptionsRow", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rowLayout = row.GetComponent<GridLayoutGroup>();
        rowLayout.cellSize = new Vector2(235f, 72f);
        rowLayout.spacing = new Vector2(10f, 10f);
        rowLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rowLayout.constraintCount = 2;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        var rowLayoutElement = row.GetComponent<LayoutElement>();
        rowLayoutElement.minHeight = 72f;
        rowLayoutElement.preferredHeight = 154f;

        var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lobby/LobbyButton.prefab");
        var optionViews = new LobbyMarkerShapeOptionView[shapeData.Length];

        for (var index = 0; index < shapeData.Length; index++)
        {
            var buttonInstance = PrefabUtility.InstantiatePrefab(buttonPrefab, row.transform) as GameObject;
            buttonInstance.name = $"Shape_{shapeData[index].Name}";
            var buttonRect = buttonInstance.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 72f);

            var layoutElement = buttonInstance.GetComponent<LayoutElement>() ?? buttonInstance.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 235f;
            layoutElement.preferredHeight = 72f;

            var label = buttonInstance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = shapeData[index].Name;
                label.fontSize = 24f;
                label.color = new Color(0.96f, 0.87f, 0.74f, 1f);
            }

            var outline = CreateSelectionOutline(buttonInstance.transform, new Color(0.98f, 0.93f, 0.78f, 1f));
            var option = buttonInstance.GetComponent<LobbyMarkerShapeOptionView>() ?? buttonInstance.AddComponent<LobbyMarkerShapeOptionView>();
            var serializedOption = new SerializedObject(option);
            serializedOption.FindProperty("shapeId").stringValue = shapeData[index].Id;
            serializedOption.FindProperty("button").objectReferenceValue = buttonInstance.GetComponent<Button>();
            serializedOption.FindProperty("label").objectReferenceValue = label;
            serializedOption.FindProperty("selectionOutline").objectReferenceValue = outline;
            serializedOption.FindProperty("backgroundImage").objectReferenceValue = buttonInstance.GetComponent<Image>();
            serializedOption.ApplyModifiedPropertiesWithoutUndo();
            optionViews[index] = option;
        }

        return optionViews;
    }

    private static LobbyMarkerColorOptionView[] CreateColorButtons(Transform parent, PlayerMarkerAppearanceCatalogConfig catalog)
    {
        var colors = catalog.Colors;
        var gridRoot = new GameObject("ColorOptionsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        gridRoot.transform.SetParent(parent, false);
        var grid = gridRoot.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(92f, 86f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperLeft;
        gridRoot.GetComponent<LayoutElement>().preferredHeight = 182f;

        var optionViews = new LobbyMarkerColorOptionView[colors.Count];
        for (var index = 0; index < colors.Count; index++)
        {
            var color = colors[index];
            var button = CreateColorButton(gridRoot.transform, color.DisplayName, color.Color);
            var option = button.GetComponent<LobbyMarkerColorOptionView>();
            var optionSerialized = new SerializedObject(option);
            optionSerialized.FindProperty("colorId").stringValue = color.ColorId;
            optionSerialized.ApplyModifiedPropertiesWithoutUndo();
            optionViews[index] = option;
        }

        return optionViews;
    }

    private static Button CreateColorButton(Transform parent, string displayName, Color swatchColor)
    {
        var root = new GameObject(
            $"Color_{displayName}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LobbyMarkerColorOptionView));
        root.transform.SetParent(parent, false);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 86f);

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.96f, 0.91f, 0.82f, 0.92f);

        var button = root.GetComponent<Button>();
        var buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(0.98f, 0.95f, 0.88f, 1f);
        buttonColors.pressedColor = new Color(0.88f, 0.82f, 0.74f, 1f);
        buttonColors.selectedColor = buttonColors.highlightedColor;
        buttonColors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        button.colors = buttonColors;

        var swatchRoot = new GameObject("Swatch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        swatchRoot.transform.SetParent(root.transform, false);
        var swatchRect = swatchRoot.GetComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0.5f, 0.5f);
        swatchRect.anchorMax = new Vector2(0.5f, 0.5f);
        swatchRect.sizeDelta = new Vector2(74f, 56f);
        swatchRect.anchoredPosition = new Vector2(0f, 8f);
        var swatchImage = swatchRoot.GetComponent<Image>();
        swatchImage.color = swatchColor;

        var label = CreateText(root.transform, "Label", string.Empty, 20, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.29f, 0.22f, 0.17f, 1f))
            .GetComponent<TextMeshProUGUI>();
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 5f);
        labelRect.sizeDelta = new Vector2(0f, 24f);

        var outline = CreateSelectionOutline(root.transform, new Color(0.48f, 0.31f, 0.12f, 1f));
        var option = root.GetComponent<LobbyMarkerColorOptionView>();
        var optionSerialized = new SerializedObject(option);
        optionSerialized.FindProperty("button").objectReferenceValue = button;
        optionSerialized.FindProperty("label").objectReferenceValue = label;
        optionSerialized.FindProperty("swatchImage").objectReferenceValue = swatchImage;
        optionSerialized.FindProperty("selectionOutline").objectReferenceValue = outline;
        optionSerialized.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    private static Image CreateSelectionOutline(Transform parent, Color color)
    {
        var outlineRoot = new GameObject("SelectionOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineRoot.transform.SetParent(parent, false);
        var outlineRect = outlineRoot.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(5f, 5f);
        outlineRect.offsetMax = new Vector2(-5f, -5f);

        var image = outlineRoot.GetComponent<Image>();
        image.color = color;
        image.enabled = false;
        return image;
    }

    private static GameObject CreateCardColumn(Transform parent, string name, Color backgroundColor, float preferredWidth, float flexibleWidth = 0f)
    {
        var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(Image));
        column.transform.SetParent(parent, false);
        column.GetComponent<Image>().color = backgroundColor;
        column.GetComponent<Image>().raycastTarget = false;

        var layout = column.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var layoutElement = column.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.minWidth = preferredWidth;
        layoutElement.flexibleWidth = flexibleWidth;
        return column;
    }

    private static void ApplyControllerReferences(
        GameObject panel,
        LocalPlayerMarkerProfileService profileService,
        PlayerMarkerAppearanceCatalogConfig catalog,
        TMP_InputField inputField,
        RawImage rawImage,
        Camera previewCamera,
        Transform previewRoot,
        LobbyMarkerShapeOptionView[] shapeOptions,
        LobbyMarkerColorOptionView[] colorOptions)
    {
        var controller = panel.GetComponent<LobbyAdventurerPassUIController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("profileService").objectReferenceValue = profileService;
        serializedController.FindProperty("appearanceCatalog").objectReferenceValue = catalog;
        serializedController.FindProperty("playerNameInput").objectReferenceValue = inputField;
        serializedController.FindProperty("previewImage").objectReferenceValue = rawImage;
        serializedController.FindProperty("previewCamera").objectReferenceValue = previewCamera;
        serializedController.FindProperty("previewRoot").objectReferenceValue = previewRoot;

        var shapeArray = serializedController.FindProperty("shapeOptions");
        shapeArray.arraySize = shapeOptions.Length;
        for (var index = 0; index < shapeOptions.Length; index++)
        {
            shapeArray.GetArrayElementAtIndex(index).objectReferenceValue = shapeOptions[index];
        }

        var colorArray = serializedController.FindProperty("colorOptions");
        colorArray.arraySize = colorOptions.Length;
        for (var index = 0; index < colorOptions.Length; index++)
        {
            colorArray.GetArrayElementAtIndex(index).objectReferenceValue = colorOptions[index];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        InvokePrivate(controller, "ConfigureOptionViews");
        profileService.EnsureInitialized();
        InvokePrivate(controller, "SyncUiFromProfile");
        InvokePrivate(controller, "RebuildPreview");
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
    }

    private static void InvokePrivate(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)?
            .Invoke(target, null);
    }

    private static void RebindMultiplayerMenuControllerReferences()
    {
        var controller = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            return;
        }

        var contentRoot = controller.transform.Find("SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content");
        if (contentRoot == null)
        {
            return;
        }

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("hostButton").objectReferenceValue = contentRoot.Find("HomePanel/CreateSessionButton")?.GetComponent<Button>();
        serializedController.FindProperty("joinButton").objectReferenceValue = contentRoot.Find("JoinPanel/JoinSessionButton")?.GetComponent<Button>();
        serializedController.FindProperty("startMatchButton").objectReferenceValue = contentRoot.Find("SessionPanel/ActionsRoot/StartMatchButton")?.GetComponent<Button>();
        serializedController.FindProperty("leaveSessionButton").objectReferenceValue = contentRoot.Find("SessionPanel/ActionsRoot/LeaveSessionButton")?.GetComponent<Button>();
        serializedController.FindProperty("copyJoinCodeButton").objectReferenceValue = contentRoot.Find("SessionPanel/ActionsRoot/CopyJoinCodeButton")?.GetComponent<Button>();
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void SanitizeLobbyButtonPrefab()
    {
        const string lobbyButtonPrefabPath = "Assets/Prefabs/Lobby/LobbyButton.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(lobbyButtonPrefabPath);
        try
        {
            var button = prefabRoot.GetComponent<Button>();
            if (button != null)
            {
                ClearPersistentListeners(button);
                EditorUtility.SetDirty(button);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, lobbyButtonPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SanitizeLobbyPanelPrefabs()
    {
        var panelPrefabPaths = new[]
        {
            "Assets/Prefabs/Lobby/HomePanel.prefab",
            "Assets/Prefabs/Lobby/HostPanel.prefab",
            "Assets/Prefabs/Lobby/JoinPanel.prefab",
            "Assets/Prefabs/Lobby/SessionPanel.prefab"
        };

        for (var index = 0; index < panelPrefabPaths.Length; index++)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(panelPrefabPaths[index]);
            try
            {
                var buttons = prefabRoot.GetComponentsInChildren<Button>(true);
                for (var buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                {
                    ClearPersistentListeners(buttons[buttonIndex]);
                    EditorUtility.SetDirty(buttons[buttonIndex]);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, panelPrefabPaths[index]);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void RebindLobbySceneButtons()
    {
        var controller = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            throw new MissingReferenceException("MultiplayerMenuController not found in Lobby scene.");
        }

        var createSessionButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HomePanel/CreateSessionButton");
        ClearPersistentListeners(createSessionButton);
        UnityEventTools.AddPersistentListener(createSessionButton.onClick, controller.HostSession);
        PersistSceneButton(createSessionButton);

        var openJoinButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HomePanel/OpenJoinPanelButton");
        ClearPersistentListeners(openJoinButton);
        UnityEventTools.AddPersistentListener(openJoinButton.onClick, controller.ShowJoinPanel);
        PersistSceneButton(openJoinButton);

        var hostSessionButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HostPanel/HostSessionButton");
        ClearPersistentListeners(hostSessionButton);
        UnityEventTools.AddPersistentListener(hostSessionButton.onClick, controller.HostSession);
        PersistSceneButton(hostSessionButton);

        var hostBackButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HostPanel/BackButton");
        ClearPersistentListeners(hostBackButton);
        UnityEventTools.AddPersistentListener(hostBackButton.onClick, controller.ShowHomePanel);
        PersistSceneButton(hostBackButton);

        var joinSessionButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/JoinPanel/JoinSessionButton");
        ClearPersistentListeners(joinSessionButton);
        UnityEventTools.AddPersistentListener(joinSessionButton.onClick, controller.JoinSession);
        PersistSceneButton(joinSessionButton);

        var joinBackButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/JoinPanel/BackButton");
        ClearPersistentListeners(joinBackButton);
        UnityEventTools.AddPersistentListener(joinBackButton.onClick, controller.ShowHomePanel);
        PersistSceneButton(joinBackButton);

        var copyJoinCodeButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/CopyJoinCodeButton");
        ClearPersistentListeners(copyJoinCodeButton);
        UnityEventTools.AddPersistentListener(copyJoinCodeButton.onClick, controller.CopyJoinCode);
        PersistSceneButton(copyJoinCodeButton);

        var startMatchButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/StartMatchButton");
        ClearPersistentListeners(startMatchButton);
        UnityEventTools.AddPersistentListener(startMatchButton.onClick, controller.StartMatch);
        PersistSceneButton(startMatchButton);

        var leaveSessionButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/LeaveSessionButton");
        ClearPersistentListeners(leaveSessionButton);
        UnityEventTools.AddPersistentListener(leaveSessionButton.onClick, controller.LeaveSession);
        PersistSceneButton(leaveSessionButton);
    }

    private static Button GetSceneButton(string path)
    {
        var transform = GameObject.Find(path)?.transform;
        if (transform == null)
        {
            throw new MissingReferenceException($"Button path not found: {path}");
        }

        var button = transform.GetComponent<Button>();
        if (button == null)
        {
            throw new MissingComponentException($"Button component missing at path: {path}");
        }

        return button;
    }

    private static void PersistSceneButton(Button button)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
    }

    private static void ClearPersistentListeners(Button button)
    {
        for (var index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, index);
        }
    }

    private static Material LoadOrCreatePreviewMaterial(string assetPath)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material != null)
        {
            return material;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader)
        {
            color = new Color(0.3f, 0.7f, 0.95f, 1f)
        };

        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static RenderTexture LoadOrCreatePreviewTexture(string assetPath)
    {
        var renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(assetPath);
        if (renderTexture != null)
        {
            return renderTexture;
        }

        renderTexture = new RenderTexture(512, 512, 24)
        {
            name = "LobbyMarkerPreview",
            antiAliasing = 4
        };

        AssetDatabase.CreateAsset(renderTexture, assetPath);
        return renderTexture;
    }

    private static GameObject CreateShapePrefab(string folder, PrimitiveType primitiveType, string assetName, Material previewMaterial)
    {
        var assetPath = $"{folder}/{assetName}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        var primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = assetName;
        var collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = primitive.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = previewMaterial;
        }

        PrefabUtility.SaveAsPrefabAsset(primitive, assetPath);
        Object.DestroyImmediate(primitive);
        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    private static PlayerMarkerAppearanceCatalogConfig LoadOrCreateCatalog(
        string assetPath,
        GameObject cubePrefab,
        GameObject spherePrefab,
        GameObject cylinderPrefab)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PlayerMarkerAppearanceCatalogConfig>(assetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PlayerMarkerAppearanceCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, assetPath);
        }

        var serializedCatalog = new SerializedObject(catalog);

        var shapes = serializedCatalog.FindProperty("shapes");
        shapes.arraySize = 3;
        ConfigureShape(shapes.GetArrayElementAtIndex(0), "cube", "Cubo", cubePrefab, Vector3.one, new Vector3(1.2f, 1.2f, 1.2f), new Vector3(18f, -25f, 0f));
        ConfigureShape(shapes.GetArrayElementAtIndex(1), "sphere", "Esfera", spherePrefab, Vector3.one, new Vector3(1.2f, 1.2f, 1.2f), new Vector3(20f, -20f, 0f));
        ConfigureShape(shapes.GetArrayElementAtIndex(2), "cylinder", "Cilindro", cylinderPrefab, new Vector3(0.92f, 1.05f, 0.92f), new Vector3(0.95f, 1.25f, 0.95f), new Vector3(15f, -30f, 0f));

        var colors = serializedCatalog.FindProperty("colors");
        colors.arraySize = 8;
        ConfigureColor(colors.GetArrayElementAtIndex(0), "sky", "Cielo", new Color(0.31f, 0.68f, 0.95f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(1), "jade", "Jade", new Color(0.22f, 0.72f, 0.58f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(2), "sun", "Sol", new Color(0.96f, 0.73f, 0.25f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(3), "coral", "Coral", new Color(0.93f, 0.45f, 0.38f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(4), "berry", "Baya", new Color(0.76f, 0.38f, 0.71f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(5), "ocean", "Oceano", new Color(0.21f, 0.41f, 0.87f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(6), "mint", "Menta", new Color(0.68f, 0.89f, 0.72f, 1f));
        ConfigureColor(colors.GetArrayElementAtIndex(7), "sand", "Arena", new Color(0.82f, 0.67f, 0.44f, 1f));

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void ConfigureShape(SerializedProperty property, string shapeId, string displayName, GameObject visualPrefab, Vector3 markerScale, Vector3 previewScale, Vector3 previewEulerAngles)
    {
        property.FindPropertyRelative("shapeId").stringValue = shapeId;
        property.FindPropertyRelative("displayName").stringValue = displayName;
        property.FindPropertyRelative("visualPrefab").objectReferenceValue = visualPrefab;
        property.FindPropertyRelative("markerScale").vector3Value = markerScale;
        property.FindPropertyRelative("previewScale").vector3Value = previewScale;
        property.FindPropertyRelative("previewEulerAngles").vector3Value = previewEulerAngles;
    }

    private static void ConfigureColor(SerializedProperty property, string colorId, string displayName, Color color)
    {
        property.FindPropertyRelative("colorId").stringValue = colorId;
        property.FindPropertyRelative("displayName").stringValue = displayName;
        property.FindPropertyRelative("color").colorValue = color;
    }

    private static string EnsureFolder(string parent, string name)
    {
        var fullPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }

        return fullPath;
    }

    private static GameObject CreateText(
        Transform parent,
        string name,
        string text,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(0f, fontSize + 20f);

        var label = root.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = color;
        label.enableAutoSizing = false;
        return root;
    }
}
