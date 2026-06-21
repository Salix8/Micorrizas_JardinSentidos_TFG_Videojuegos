using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LobbyAdventurerPassSceneSetup
{
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string CatalogPath = "Assets/ScriptableObjects/Lobby/PlayerMarkerAppearanceCatalogConfig.asset";
    private const string AvatarFolder = "Assets/CoopMinigames/Theme/Generated/Avatars";

    private static readonly AvatarSeed[] AvatarSeeds =
    {
        new("bee", "Abeja", $"{AvatarFolder}/TeamAvatarBee.png"),
        new("hedgehog", "Erizo", $"{AvatarFolder}/TeamAvatarHedgehog.png"),
        new("mushroom", "Seta", $"{AvatarFolder}/TeamAvatarMushroom.png"),
        new("robin", "Petirrojo", $"{AvatarFolder}/TeamAvatarRobin.png"),
        new("sprout", "Brote", $"{AvatarFolder}/TeamAvatarSprout.png"),
        new("water-drop", "Gota", $"{AvatarFolder}/TeamAvatarWaterDrop.png")
    };

    [MenuItem("SmartCampus/Lobby/Rebuild Adventurer Pass Panel")]
    public static void RebuildFromMenu()
    {
        Debug.Log(Run());
    }

    public static string Run()
    {
        var prefabsFolder = EnsureFolder("Assets/Prefabs", "Lobby");
        var configFolder = EnsureFolder("Assets", "ScriptableObjects");
        configFolder = EnsureFolder(configFolder, "Lobby");

        var catalog = LoadOrCreateCatalog(CatalogPath);
        var lobbyScene = EditorSceneManager.GetSceneByPath(LobbyScenePath);
        if (!lobbyScene.isLoaded)
        {
            lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
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

        ConfigureProfileService(profileService, catalog);
        RemoveLegacyChildren(surfacePanel.transform);

        var existingPanel = surfacePanel.transform.Find("AdventurerPassPanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel.gameObject);
        }

        var panel = BuildPanel(surfacePanel.transform, catalog, profileService);
        RebindMultiplayerMenuControllerReferences();

        var panelPrefabPath = $"{prefabsFolder}/LobbyAdventurerPassPanel.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(panel, panelPrefabPath, InteractionMode.AutomatedAction);
        RebindLobbySceneButtons();

        EditorSceneManager.MarkSceneDirty(lobbyScene);
        EditorSceneManager.SaveScene(lobbyScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"Created/updated {prefab.name} with avatar catalog {catalog.name}.";
    }

    private static void ConfigureProfileService(LocalPlayerMarkerProfileService profileService, PlayerMarkerAppearanceCatalogConfig catalog)
    {
        var profileSerialized = new SerializedObject(profileService);
        profileSerialized.FindProperty("appearanceCatalog").objectReferenceValue = catalog;
        profileSerialized.FindProperty("defaultDisplayName").stringValue = "Aventurero";
        profileSerialized.FindProperty("maxDisplayNameLength").intValue = 18;
        profileSerialized.ApplyModifiedPropertiesWithoutUndo();

        profileService.Reload();
        profileService.SetDisplayName("Aventurero");
        profileService.SetAvatarId(catalog.DefaultAvatarId);
        EditorUtility.SetDirty(profileService);
    }

    private static GameObject BuildPanel(
        Transform surfacePanel,
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

        var scrollFrame = surfacePanel.Find("ScrollFrame");
        if (scrollFrame != null)
        {
            panel.transform.SetSiblingIndex(scrollFrame.GetSiblingIndex() + 1);
        }

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
        CreateText(panel.transform, "SectionSubtitle", "Elige tu nombre y el avatar 2D que marcara tu posicion en el mapa.", 24, FontStyles.Normal, TextAlignmentOptions.Left, bodyColor);

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
        CreateText(previewColumn.transform, "PreviewTitle", "Tu marcador GPS", 24, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        var previewImage = CreatePreviewFrame(previewColumn.transform);

        var customizeColumn = CreateCardColumn(bodyRow.transform, "CustomizeColumn", new Color(1f, 1f, 1f, 0.24f), 500f, flexibleWidth: 1f);
        CreateText(customizeColumn.transform, "IdentityTitle", "Nombre del aventurero", 26, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        CreateText(customizeColumn.transform, "IdentityHint", "Se guardara en tu perfil local del mapa.", 20, FontStyles.Normal, TextAlignmentOptions.Left, bodyColor);
        var inputField = CreatePlayerNameInput(customizeColumn.transform);

        CreateText(customizeColumn.transform, "AvatarTitle", "Avatar del mapa", 26, FontStyles.Bold, TextAlignmentOptions.Left, titleColor);
        var avatarOptions = CreateAvatarButtons(customizeColumn.transform, catalog);

        ApplyControllerReferences(panel, profileService, catalog, inputField, previewImage, avatarOptions);
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

    private static Image CreatePreviewFrame(Transform parent)
    {
        var previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        previewFrame.transform.SetParent(parent, false);
        var frameImage = previewFrame.GetComponent<Image>();
        frameImage.color = new Color(0.25f, 0.34f, 0.18f, 0.96f);
        frameImage.raycastTarget = false;
        previewFrame.GetComponent<LayoutElement>().preferredHeight = 300f;

        var previewImage = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewImage.transform.SetParent(previewFrame.transform, false);
        var imageRect = previewImage.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(190f, 190f);
        imageRect.anchoredPosition = Vector2.zero;

        var image = previewImage.GetComponent<Image>();
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static LobbyMarkerAvatarOptionView[] CreateAvatarButtons(Transform parent, PlayerMarkerAppearanceCatalogConfig catalog)
    {
        var avatars = catalog.Avatars;
        var gridRoot = new GameObject("AvatarOptionsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        gridRoot.transform.SetParent(parent, false);
        var grid = gridRoot.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(98f, 98f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;

        var rows = Mathf.Max(1, Mathf.CeilToInt(avatars.Count / 3f));
        gridRoot.GetComponent<LayoutElement>().preferredHeight = rows * 98f + (rows - 1) * 12f;

        var optionViews = new LobbyMarkerAvatarOptionView[avatars.Count];
        for (var index = 0; index < avatars.Count; index++)
        {
            optionViews[index] = CreateAvatarButton(gridRoot.transform, avatars[index]);
        }

        return optionViews;
    }

    private static LobbyMarkerAvatarOptionView CreateAvatarButton(Transform parent, PlayerMarkerAvatarDefinition avatar)
    {
        var root = new GameObject(
            $"Avatar_{avatar.DisplayName}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(LobbyMarkerAvatarOptionView));
        root.transform.SetParent(parent, false);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(98f, 98f);
        root.GetComponent<LayoutElement>().preferredWidth = 98f;
        root.GetComponent<LayoutElement>().preferredHeight = 98f;

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.93f, 0.86f, 0.68f, 0.98f);

        var button = root.GetComponent<Button>();
        var buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(1f, 0.95f, 0.78f, 1f);
        buttonColors.pressedColor = new Color(0.78f, 0.62f, 0.32f, 1f);
        buttonColors.selectedColor = Color.white;
        buttonColors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        button.colors = buttonColors;

        var avatarRoot = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        avatarRoot.transform.SetParent(root.transform, false);
        var avatarRect = avatarRoot.GetComponent<RectTransform>();
        avatarRect.anchorMin = Vector2.zero;
        avatarRect.anchorMax = Vector2.one;
        avatarRect.offsetMin = new Vector2(10f, 10f);
        avatarRect.offsetMax = new Vector2(-10f, -10f);

        var avatarImage = avatarRoot.GetComponent<Image>();
        avatarImage.sprite = avatar.AvatarSprite;
        avatarImage.preserveAspect = true;
        avatarImage.color = Color.white;
        avatarImage.raycastTarget = false;

        var selectionBorder = CreateSelectionBorder(root.transform, new Color(0.13f, 0.31f, 0.14f, 1f));

        var option = root.GetComponent<LobbyMarkerAvatarOptionView>();
        var optionSerialized = new SerializedObject(option);
        optionSerialized.FindProperty("avatarId").stringValue = avatar.AvatarId;
        optionSerialized.FindProperty("button").objectReferenceValue = button;
        optionSerialized.FindProperty("avatarImage").objectReferenceValue = avatarImage;
        optionSerialized.FindProperty("selectionBorderRoot").objectReferenceValue = selectionBorder;
        optionSerialized.FindProperty("backgroundImage").objectReferenceValue = rootImage;
        optionSerialized.ApplyModifiedPropertiesWithoutUndo();
        option.Configure(avatar.AvatarId, avatar.AvatarSprite);
        return option;
    }

    private static GameObject CreateSelectionBorder(Transform parent, Color color)
    {
        var borderRoot = new GameObject("SelectionBorder", typeof(RectTransform));
        borderRoot.transform.SetParent(parent, false);
        var borderRect = borderRoot.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(3f, 3f);
        borderRect.offsetMax = new Vector2(-3f, -3f);

        CreateBorderSegment(borderRoot.transform, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), new Vector2(0f, 0f));
        CreateBorderSegment(borderRoot.transform, "Bottom", color, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 5f));
        CreateBorderSegment(borderRoot.transform, "Left", color, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 5f), new Vector2(5f, -5f));
        CreateBorderSegment(borderRoot.transform, "Right", color, new Vector2(1f, 0f), Vector2.one, new Vector2(-5f, 5f), new Vector2(0f, -5f));

        borderRoot.SetActive(false);
        return borderRoot;
    }

    private static void CreateBorderSegment(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var segment = new GameObject($"Border{name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segment.transform.SetParent(parent, false);
        var rect = segment.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = segment.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static GameObject CreateCardColumn(Transform parent, string name, Color backgroundColor, float preferredWidth, float flexibleWidth = 0f)
    {
        var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(Image));
        column.transform.SetParent(parent, false);
        var columnImage = column.GetComponent<Image>();
        columnImage.color = backgroundColor;
        columnImage.raycastTarget = false;

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
        Image previewImage,
        LobbyMarkerAvatarOptionView[] avatarOptions)
    {
        var controller = panel.GetComponent<LobbyAdventurerPassUIController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("profileService").objectReferenceValue = profileService;
        serializedController.FindProperty("appearanceCatalog").objectReferenceValue = catalog;
        serializedController.FindProperty("playerNameInput").objectReferenceValue = inputField;
        serializedController.FindProperty("previewImage").objectReferenceValue = previewImage;
        serializedController.FindProperty("previewFrameImage").objectReferenceValue = previewImage.transform.parent.GetComponent<Image>();

        var avatarArray = serializedController.FindProperty("avatarOptions");
        avatarArray.arraySize = avatarOptions.Length;
        for (var index = 0; index < avatarOptions.Length; index++)
        {
            avatarArray.GetArrayElementAtIndex(index).objectReferenceValue = avatarOptions[index];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        profileService.EnsureInitialized();
        InvokePrivate(controller, "ConfigureOptionViews");
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

    private static void RemoveLegacyChildren(Transform root)
    {
        RemoveChildByName(root, "ShapeOptionsRow");
        RemoveChildByName(root, "ColorOptionsGrid");
        RemoveChildByName(root, "PreviewRig");
        RemoveChildByName(root, "PreviewRoot");
        RemoveChildByName(root, "PreviewCamera");
    }

    private static void RemoveChildByName(Transform root, string childName)
    {
        var children = root.GetComponentsInChildren<Transform>(true);
        for (var index = children.Length - 1; index >= 0; index--)
        {
            if (children[index] != root && string.Equals(children[index].name, childName, StringComparison.Ordinal))
            {
                Object.DestroyImmediate(children[index].gameObject);
            }
        }
    }

    private static PlayerMarkerAppearanceCatalogConfig LoadOrCreateCatalog(string assetPath)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PlayerMarkerAppearanceCatalogConfig>(assetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PlayerMarkerAppearanceCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, assetPath);
        }

        var serializedCatalog = new SerializedObject(catalog);
        var avatars = serializedCatalog.FindProperty("avatars");
        avatars.arraySize = AvatarSeeds.Length;

        for (var index = 0; index < AvatarSeeds.Length; index++)
        {
            ConfigureAvatar(avatars.GetArrayElementAtIndex(index), AvatarSeeds[index]);
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void ConfigureAvatar(SerializedProperty property, AvatarSeed avatarSeed)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(avatarSeed.SpritePath);
        if (sprite == null)
        {
            throw new MissingReferenceException($"Avatar sprite not found at {avatarSeed.SpritePath}");
        }

        property.FindPropertyRelative("avatarId").stringValue = avatarSeed.Id;
        property.FindPropertyRelative("displayName").stringValue = avatarSeed.DisplayName;
        property.FindPropertyRelative("avatarSprite").objectReferenceValue = sprite;
        property.FindPropertyRelative("markerScale").vector3Value = new Vector3(0.2f, 0.2f, 0.2f);
        property.FindPropertyRelative("previewSize").vector2Value = new Vector2(190f, 190f);
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
        serializedController.FindProperty("teamNameInput").objectReferenceValue = contentRoot.Find("SessionPanel/TeamNameInput")?.GetComponent<TMP_InputField>();
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
        if (hostSessionButton != null)
        {
            ClearPersistentListeners(hostSessionButton);
            UnityEventTools.AddPersistentListener(hostSessionButton.onClick, controller.HostSession);
            PersistSceneButton(hostSessionButton);
        }

        var hostBackButton = GetSceneButton("Canvas/MultiplayerMenuController/SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HostPanel/BackButton");
        if (hostBackButton != null)
        {
            ClearPersistentListeners(hostBackButton);
            UnityEventTools.AddPersistentListener(hostBackButton.onClick, controller.ShowHomePanel);
            PersistSceneButton(hostBackButton);
        }

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
            return null;
        }

        return transform.GetComponent<Button>();
    }

    private static void PersistSceneButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
    }

    private static void ClearPersistentListeners(Button button)
    {
        if (button == null)
        {
            return;
        }

        for (var index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, index);
        }
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

    private readonly struct AvatarSeed
    {
        public AvatarSeed(string id, string displayName, string spritePath)
        {
            Id = id;
            DisplayName = displayName;
            SpritePath = spritePath;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string SpritePath { get; }
    }
}
