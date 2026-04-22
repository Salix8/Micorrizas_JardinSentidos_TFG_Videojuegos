using System.Collections.Generic;
using System.Text;
using Esri.GameEngine.Geometry;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UjiMapDebugOverlayUIController : MonoBehaviour
{
    private const string OverlayCanvasName = "UJI Map Debug Overlay Canvas";
    private const string ToggleButtonName = "ToggleMinigamesButton";
    private const string DebugTextName = "DeviceGpsDebugText";

    [Header("References")]
    [SerializeField] private ArcGISMobileGpsBlueDot gpsTracker;
    [SerializeField] private CoopSessionCoordinator sessionCoordinator;
    [SerializeField] private GameObject minigameLauncherCanvas;

    [Header("Debug UI")]
    [SerializeField] private bool createRuntimeUi = false;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Text toggleButtonLabel;
    [SerializeField] private Text debugText;
    [SerializeField] private float refreshIntervalSeconds = 0.25f;
    [SerializeField] private int debugFontSize = 18;
    [SerializeField] private Vector2 debugPanelSize = new(520f, 220f);
    [SerializeField] private Vector2 toggleButtonSize = new(190f, 72f);

    private readonly StringBuilder stringBuilder = new();
    private readonly HashSet<ulong> renderedClientIds = new();
    private float nextRefreshTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (createRuntimeUi)
        {
            EnsureRuntimeUi();
        }

        AttachButton();
        RefreshDebugText();
    }

    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleMinigameLauncher);
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
        RefreshDebugText();
    }

    private void ToggleMinigameLauncher()
    {
        ResolveReferences();

        if (minigameLauncherCanvas == null)
        {
            return;
        }

        minigameLauncherCanvas.SetActive(!minigameLauncherCanvas.activeSelf);
        UpdateToggleButtonLabel();
    }

    private void ResolveReferences()
    {
        if (gpsTracker == null)
        {
            gpsTracker = FindFirstObjectByType<ArcGISMobileGpsBlueDot>(FindObjectsInactive.Include);
        }

        if (sessionCoordinator == null)
        {
            sessionCoordinator = FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        }

        if (minigameLauncherCanvas == null)
        {
            var launcher = GameObject.Find("CoopMinigameLauncherCanvas");
            if (launcher != null)
            {
                minigameLauncherCanvas = launcher;
            }
        }
    }

    private void EnsureRuntimeUi()
    {
        overlayCanvas = FindOrCreateOverlayCanvas();
        toggleButton = FindOrCreateToggleButton(overlayCanvas.transform);
        debugText = FindOrCreateDebugText(overlayCanvas.transform);
    }

    private void AttachButton()
    {
        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(ToggleMinigameLauncher);
        toggleButton.onClick.AddListener(ToggleMinigameLauncher);
        UpdateToggleButtonLabel();
    }

    private void UpdateToggleButtonLabel()
    {
        if (toggleButtonLabel == null)
        {
            return;
        }

        var isVisible = minigameLauncherCanvas == null || minigameLauncherCanvas.activeSelf;
        toggleButtonLabel.text = isVisible ? "Ocultar\nminijuegos" : "Mostrar\nminijuegos";
    }

    private void RefreshDebugText()
    {
        if (debugText == null)
        {
            return;
        }

        stringBuilder.Clear();
        renderedClientIds.Clear();
        AppendGpsTrackerStatus();

        if (gpsTracker != null && gpsTracker.HasLocationFix)
        {
            var localDeviceIndex = GetLocalDeviceIndex();
            AppendDeviceLine(localDeviceIndex, gpsTracker.CurrentGeographicPosition, gpsTracker.CurrentEnginePosition);
            AppendLocalMarkerLine();

            var networkManager = sessionCoordinator != null ? sessionCoordinator.NetworkManager : NetworkManager.Singleton;
            if (networkManager != null)
            {
                renderedClientIds.Add(networkManager.LocalClientId);
            }
        }

        AppendNetworkDeviceLines();

        if (stringBuilder.Length == 0)
        {
            stringBuilder.AppendLine("GPS debug: sin posicion todavia");
        }
        else if (gpsTracker == null || !gpsTracker.HasLocationFix)
        {
            stringBuilder.AppendLine("GPS local: sin fix todavia");
        }

        debugText.text = stringBuilder.ToString();
    }

    private void AppendGpsTrackerStatus()
    {
        if (gpsTracker == null)
        {
            stringBuilder.AppendLine("GPS tracker: no encontrado");
            return;
        }

        stringBuilder
            .Append("GPS: ")
            .Append(gpsTracker.ActiveLocationSource)
            .Append(" status ")
            .Append(gpsTracker.DeviceLocationStatus)
            .Append(" enabled ")
            .Append(gpsTracker.IsDeviceLocationEnabledByUser ? "si" : "no")
            .Append(" fix ")
            .Append(gpsTracker.HasLocationFix ? "si" : "no")
            .AppendLine();

        stringBuilder
            .Append("GPS cfg: accuracy ")
            .Append(gpsTracker.DesiredAccuracyInMeters.ToString("F1"))
            .Append("m update ")
            .Append(gpsTracker.UpdateDistanceInMeters.ToString("F1"))
            .Append("m");

        if (!double.IsNegativeInfinity(gpsTracker.LastLocationTimestamp))
        {
            stringBuilder
                .Append(" ts ")
                .Append(gpsTracker.LastLocationTimestamp.ToString("F1"));
        }

        if (gpsTracker.HasLocationFix)
        {
            stringBuilder
                .Append(" hAcc ")
                .Append(gpsTracker.LastLocationInfo.horizontalAccuracy.ToString("F1"))
                .Append("m");
        }

        stringBuilder.AppendLine();
    }

    private void AppendLocalMarkerLine()
    {
        if (gpsTracker == null)
        {
            return;
        }

        var rootPosition = gpsTracker.MarkerRootWorldPosition;
        var visualPosition = gpsTracker.MarkerVisualWorldPosition;
        stringBuilder
            .Append("Bola local: visible ")
            .Append(gpsTracker.IsMarkerVisible ? "si" : "no")
            .Append(" rootY ")
            .Append(rootPosition.y.ToString("F2"))
            .Append(" visualY ")
            .Append(visualPosition.y.ToString("F2"))
            .AppendLine();
    }

    private void AppendNetworkDeviceLines()
    {
        if (sessionCoordinator == null || !sessionCoordinator.IsSpawned)
        {
            return;
        }

        for (var index = 0; index < sessionCoordinator.PlayerGpsStateCount; index++)
        {
            if (!sessionCoordinator.TryGetPlayerGpsState(index, out var state) ||
                !state.HasLocationFix ||
                renderedClientIds.Contains(state.ClientId))
            {
                continue;
            }

            renderedClientIds.Add(state.ClientId);

            var deviceIndex = sessionCoordinator.GetPlayerSlot(state.ClientId) + 1;
            if (deviceIndex <= 0)
            {
                deviceIndex = index + 1;
            }

            var geographicPosition = new ArcGISPoint(state.Longitude, state.Latitude, state.Altitude, ArcGISSpatialReference.WGS84());
            var enginePosition = TryConvertToEnginePosition(geographicPosition, out var convertedEnginePosition)
                ? convertedEnginePosition
                : new Vector3(0f, (float)state.Altitude, 0f);

            AppendDeviceLine(deviceIndex, geographicPosition, enginePosition);
        }
    }

    private int GetLocalDeviceIndex()
    {
        if (sessionCoordinator == null || !sessionCoordinator.IsSpawned)
        {
            return 1;
        }

        var slot = sessionCoordinator.GetLocalPlayerSlot();
        return slot < 0 ? 1 : slot + 1;
    }

    private bool TryConvertToEnginePosition(ArcGISPoint geographicPosition, out Vector3 enginePosition)
    {
        enginePosition = default;

        if (gpsTracker?.MapComponent == null ||
            geographicPosition == null ||
            !gpsTracker.MapComponent.HasSpatialReference())
        {
            return false;
        }

        enginePosition = gpsTracker.MapComponent.GeographicToEngine(geographicPosition);
        return true;
    }

    private void AppendDeviceLine(int deviceIndex, ArcGISPoint geographicPosition, Vector3 enginePosition)
    {
        if (geographicPosition == null)
        {
            return;
        }

        stringBuilder
            .Append("Disp ")
            .Append(deviceIndex)
            .Append(": alt ")
            .Append(geographicPosition.Z.ToString("F1"))
            .Append(" y ")
            .Append(enginePosition.y.ToString("F2"))
            .Append(" lat ")
            .Append(geographicPosition.Y.ToString("F6"))
            .Append(" lon ")
            .Append(geographicPosition.X.ToString("F6"))
            .AppendLine();
    }

    private Canvas FindOrCreateOverlayCanvas()
    {
        var existingCanvasObject = GameObject.Find(OverlayCanvasName);
        if (existingCanvasObject != null && existingCanvasObject.TryGetComponent<Canvas>(out var existingCanvas))
        {
            return existingCanvas;
        }

        var canvasObject = new GameObject(OverlayCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private Button FindOrCreateToggleButton(Transform parent)
    {
        var existingButton = GameObject.Find(ToggleButtonName);
        if (existingButton != null && existingButton.TryGetComponent<Button>(out var existing))
        {
            toggleButtonLabel = existingButton.GetComponentInChildren<Text>(true);
            return existing;
        }

        var buttonObject = new GameObject(ToggleButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-28f, -28f);
        rectTransform.sizeDelta = toggleButtonSize;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.05f, 0.18f, 0.32f, 0.9f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        toggleButtonLabel = CreateText("Label", buttonObject.transform, 26, TextAnchor.MiddleCenter, Color.white);
        toggleButtonLabel.raycastTarget = false;

        return button;
    }

    private Text FindOrCreateDebugText(Transform parent)
    {
        var existingDebugObject = GameObject.Find(DebugTextName);
        if (existingDebugObject != null && existingDebugObject.TryGetComponent<Text>(out var existing))
        {
            return existing;
        }

        var panelObject = new GameObject("DeviceGpsDebugPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = debugPanelSize;

        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.38f);

        var text = CreateText(DebugTextName, panelObject.transform, debugFontSize, TextAnchor.UpperLeft, Color.white);
        var textRect = text.GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(14f, 10f);
        textRect.offsetMax = new Vector2(-14f, -10f);
        text.raycastTarget = false;

        return text;
    }

    private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }
}
