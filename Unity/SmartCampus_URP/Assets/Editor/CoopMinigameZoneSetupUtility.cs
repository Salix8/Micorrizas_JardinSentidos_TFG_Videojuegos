using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CoopMinigameZoneSetupUtility
{
    private const string UjiScenePath = "Assets/Scenes/UJI.unity";

    private static readonly ZoneSeed[] ZoneSeeds =
    {
        new("TriggerVista", 1, new Vector3(0f, 0f, 0f), new Color(0.26f, 0.56f, 0.9f, 0.25f)),
        new("TriggerOido", 2, new Vector3(35f, 0f, 0f), new Color(0.8f, 0.46f, 0.18f, 0.25f)),
        new("TriggerTacto", 3, new Vector3(70f, 0f, 0f), new Color(0.58f, 0.3f, 0.76f, 0.25f)),
        new("TriggerGusto", 4, new Vector3(105f, 0f, 0f), new Color(0.22f, 0.62f, 0.38f, 0.25f)),
        new("TriggerOlfato", 5, new Vector3(140f, 0f, 0f), new Color(0.84f, 0.38f, 0.48f, 0.25f))
    };

    [MenuItem("Tools/Coop/Setup GPS Minigame Zones In UJI")]
    public static void SetupGpsMinigameZonesInUji()
    {
        var scene = EditorSceneManager.OpenScene(UjiScenePath, OpenSceneMode.Single);

        var markerController = UnityEngine.Object.FindFirstObjectByType<CoopGpsMarkerController>(FindObjectsInactive.Include);
        if (markerController == null)
        {
            throw new InvalidOperationException("No se ha encontrado CoopGpsMarkerController en UJI.");
        }

        var root = GetOrCreateRoot("MinigameTriggers");
        var zoneDefinitions = new CoopMinigameZoneDefinition[ZoneSeeds.Length];

        for (var index = 0; index < ZoneSeeds.Length; index++)
        {
            var seed = ZoneSeeds[index];
            var alreadyExists = root.transform.Find(seed.Name) != null;
            var zoneObject = GetOrCreateWorldChild(root.transform, seed.Name);
            if (!alreadyExists)
            {
                zoneObject.transform.position = seed.Position;
                zoneObject.transform.rotation = Quaternion.identity;
                zoneObject.transform.localScale = Vector3.one;
            }

            var boxCollider = zoneObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = zoneObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(24f, 6f, 24f);
            }

            boxCollider.isTrigger = true;

            var sphereCollider = zoneObject.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(sphereCollider);
            }

            var zoneDefinition = zoneObject.GetComponent<CoopMinigameZoneDefinition>();
            if (zoneDefinition == null)
            {
                zoneDefinition = zoneObject.AddComponent<CoopMinigameZoneDefinition>();
            }
            ApplySerializedZoneSettings(zoneDefinition, seed);
            zoneDefinitions[index] = zoneDefinition;
        }

        var countdownUi = CreateOrUpdateCountdownCanvas();
        var triggerController = root.GetComponent<CoopMinigameZoneTriggerController>() ?? root.AddComponent<CoopMinigameZoneTriggerController>();
        var serializedTriggerController = new SerializedObject(triggerController);
        serializedTriggerController.FindProperty("gpsMarkerController").objectReferenceValue = markerController;
        serializedTriggerController.FindProperty("countdownUiController").objectReferenceValue = countdownUi;
        var zoneArray = serializedTriggerController.FindProperty("zoneDefinitions");
        zoneArray.arraySize = zoneDefinitions.Length;
        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            zoneArray.GetArrayElementAtIndex(index).objectReferenceValue = zoneDefinitions[index];
        }

        serializedTriggerController.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(triggerController);
        EditorUtility.SetDirty(countdownUi);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ApplySerializedZoneSettings(CoopMinigameZoneDefinition zoneDefinition, ZoneSeed seed)
    {
        var serializedZone = new SerializedObject(zoneDefinition);
        serializedZone.FindProperty("miniGameNumber").intValue = seed.MiniGameNumber;
        serializedZone.FindProperty("displayName").stringValue = seed.Name.Replace("Trigger", string.Empty);
        serializedZone.FindProperty("maxAcceptedAccuracyMeters").floatValue = 15f;
        serializedZone.FindProperty("insideToleranceMeters").floatValue = 0.35f;
        serializedZone.FindProperty("gizmoColor").colorValue = seed.GizmoColor;
        serializedZone.FindProperty("zoneCollider").objectReferenceValue = zoneDefinition.GetComponent<BoxCollider>();
        serializedZone.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CoopMinigameZoneCountdownUIController CreateOrUpdateCountdownCanvas()
    {
        var canvasObject = GetOrCreateUiRoot("MinigameTriggerCountdownCanvas");
        var canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        var panelObject = GetOrCreateUiChild(canvasObject.transform, "CountdownPanel", typeof(Image));
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.12f);
        panelRect.anchorMax = new Vector2(0.5f, 0.12f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(340f, 340f);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.11f, 0.07f, 0.04f, 0.55f);
        panelImage.raycastTarget = false;

        var ringBackgroundObject = GetOrCreateUiChild(panelObject.transform, "ProgressBackground", typeof(Image));
        ConfigureFullStretchRect(ringBackgroundObject.GetComponent<RectTransform>(), 24f);
        var ringBackgroundImage = ringBackgroundObject.GetComponent<Image>();
        ringBackgroundImage.color = new Color(0.95f, 0.88f, 0.76f, 0.18f);
        ringBackgroundImage.raycastTarget = false;

        var ringFillObject = GetOrCreateUiChild(panelObject.transform, "ProgressFill", typeof(Image));
        ConfigureFullStretchRect(ringFillObject.GetComponent<RectTransform>(), 24f);
        var ringFillImage = ringFillObject.GetComponent<Image>();
        ringFillImage.color = new Color(0.22f, 0.56f, 0.95f, 0.95f);
        ringFillImage.type = Image.Type.Filled;
        ringFillImage.fillMethod = Image.FillMethod.Radial360;
        ringFillImage.fillOrigin = (int)Image.Origin360.Top;
        ringFillImage.fillClockwise = false;
        ringFillImage.fillAmount = 0f;
        ringFillImage.raycastTarget = false;

        var centerPlateObject = GetOrCreateUiChild(panelObject.transform, "CenterPlate", typeof(Image));
        ConfigureFullStretchRect(centerPlateObject.GetComponent<RectTransform>(), 72f);
        var centerPlateImage = centerPlateObject.GetComponent<Image>();
        centerPlateImage.color = new Color(0.95f, 0.92f, 0.86f, 0.94f);
        centerPlateImage.raycastTarget = false;

        var titleObject = GetOrCreateUiChild(centerPlateObject.transform, "TitleLabel", typeof(TextMeshProUGUI));
        ConfigureAnchoredRect(titleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.72f), new Vector2(250f, 48f));
        var titleText = ConfigureText(titleObject.GetComponent<TextMeshProUGUI>(), 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.color = new Color(0.17f, 0.11f, 0.07f, 1f);

        var subtitleObject = GetOrCreateUiChild(centerPlateObject.transform, "SubtitleLabel", typeof(TextMeshProUGUI));
        ConfigureAnchoredRect(subtitleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), new Vector2(250f, 72f));
        var subtitleText = ConfigureText(subtitleObject.GetComponent<TextMeshProUGUI>(), 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        subtitleText.color = new Color(0.24f, 0.16f, 0.1f, 1f);

        var timerObject = GetOrCreateUiChild(centerPlateObject.transform, "TimerLabel", typeof(TextMeshProUGUI));
        ConfigureAnchoredRect(timerObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(170f, 64f));
        var timerText = ConfigureText(timerObject.GetComponent<TextMeshProUGUI>(), 38f, FontStyles.Bold, TextAlignmentOptions.Center);
        timerText.color = new Color(0.12f, 0.08f, 0.05f, 1f);

        var controller = panelObject.GetComponent<CoopMinigameZoneCountdownUIController>();
        if (controller == null)
        {
            controller = panelObject.AddComponent<CoopMinigameZoneCountdownUIController>();
        }
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("contentRoot").objectReferenceValue = panelObject;
        serializedController.FindProperty("progressFillImage").objectReferenceValue = ringFillImage;
        serializedController.FindProperty("titleLabel").objectReferenceValue = titleText;
        serializedController.FindProperty("subtitleLabel").objectReferenceValue = subtitleText;
        serializedController.FindProperty("timerLabel").objectReferenceValue = timerText;
        serializedController.FindProperty("defaultTitle").stringValue = "Zona cooperativa";
        serializedController.FindProperty("subtitleFormat").stringValue = "Manteneos dentro de {0}";
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        panelObject.SetActive(false);
        EditorUtility.SetDirty(canvasObject);
        EditorUtility.SetDirty(panelObject);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(name);
    }

    private static GameObject GetOrCreateUiRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(name, typeof(RectTransform));
    }

    private static GameObject GetOrCreateWorldChild(Transform parent, string name, params Type[] componentTypes)
    {
        return GetOrCreateChild(parent, name, false, componentTypes);
    }

    private static GameObject GetOrCreateUiChild(Transform parent, string name, params Type[] componentTypes)
    {
        return GetOrCreateChild(parent, name, true, componentTypes);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name, bool useRectTransform, params Type[] componentTypes)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var types = BuildComponentTypeArray(useRectTransform, componentTypes);
            var gameObject = types.Length == 0 ? new GameObject(name) : new GameObject(name, types);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        foreach (var type in componentTypes)
        {
            if (child.GetComponent(type) == null)
            {
                child.gameObject.AddComponent(type);
            }
        }

        return child.gameObject;
    }

    private static Type[] BuildComponentTypeArray(bool useRectTransform, Type[] componentTypes)
    {
        var extraCount = componentTypes == null ? 0 : componentTypes.Length;
        if (!useRectTransform && extraCount == 0)
        {
            return Array.Empty<Type>();
        }

        var result = new Type[extraCount + (useRectTransform ? 1 : 0)];
        var offset = 0;
        if (useRectTransform)
        {
            result[0] = typeof(RectTransform);
            offset = 1;
        }

        if (extraCount > 0)
        {
            Array.Copy(componentTypes, 0, result, offset, extraCount);
        }

        return result;
    }

    private static void ConfigureFullStretchRect(RectTransform rectTransform, float inset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(inset, inset);
        rectTransform.offsetMax = new Vector2(-inset, -inset);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void ConfigureAnchoredRect(RectTransform rectTransform, Vector2 anchor, Vector2 size)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static TextMeshProUGUI ConfigureText(TextMeshProUGUI text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private readonly struct ZoneSeed
    {
        public ZoneSeed(string name, int miniGameNumber, Vector3 position, Color gizmoColor)
        {
            Name = name;
            MiniGameNumber = miniGameNumber;
            Position = position;
            GizmoColor = gizmoColor;
        }

        public string Name { get; }
        public int MiniGameNumber { get; }
        public Vector3 Position { get; }
        public Color GizmoColor { get; }
    }
}
