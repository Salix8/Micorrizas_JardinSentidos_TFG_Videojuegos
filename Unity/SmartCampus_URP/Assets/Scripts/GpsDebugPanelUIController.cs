using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GpsDebugPanelUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DeviceGpsService deviceGpsService;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private CoopGpsMarkerController gpsMarkerController;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private Button detailsToggleButton;
    [SerializeField] private TMP_Text detailsToggleLabel;

    [Header("Labels")]
    [SerializeField] private string showDetailsLabel = "Mas info";
    [SerializeField] private string hideDetailsLabel = "Menos info";

    [Header("Debug")]
    [SerializeField] private bool showDetailsByDefault;
    [SerializeField] private bool logExpandedReportToConsole = true;
    [SerializeField] [Min(0.1f)] private float refreshIntervalSeconds = 0.25f;

    private readonly StringBuilder summaryBuilder = new();
    private readonly StringBuilder detailsBuilder = new();
    private bool detailsVisible;
    private float nextRefreshTime;

    private void Awake()
    {
        ResolveReferences();
        detailsVisible = showDetailsByDefault;
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (detailsToggleButton != null)
        {
            detailsToggleButton.onClick.RemoveListener(ToggleDetails);
            detailsToggleButton.onClick.AddListener(ToggleDetails);
        }

        RefreshUi(forceConsoleLog: false);
    }

    private void OnDisable()
    {
        if (detailsToggleButton != null)
        {
            detailsToggleButton.onClick.RemoveListener(ToggleDetails);
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
        RefreshUi(forceConsoleLog: false);
    }

    public void ToggleDetails()
    {
        detailsVisible = !detailsVisible;
        RefreshUi(logExpandedReportToConsole);
    }

    private void RefreshUi(bool forceConsoleLog)
    {
        summaryBuilder.Clear();
        detailsBuilder.Clear();

        summaryBuilder.AppendLine("GPS Debug");
        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("[Local Unity]");
        AppendLocalGpsSection(summaryBuilder, includeDetails: false);

        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("[GPS Sincronizado]");
        AppendSyncSection(summaryBuilder, includeDetails: false);

        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("[ArcGIS Proyeccion]");
        AppendProjectionSection(summaryBuilder, includeDetails: false);

        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("[Marcador Mundo]");
        AppendMarkerWorldSection(summaryBuilder, includeDetails: false);

        detailsBuilder.AppendLine("GPS Debug Detallado");
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[Local Unity]");
        AppendLocalGpsSection(detailsBuilder, includeDetails: true);

        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[GPS Sincronizado]");
        AppendSyncSection(detailsBuilder, includeDetails: true);

        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[ArcGIS Proyeccion]");
        AppendProjectionSection(detailsBuilder, includeDetails: true);

        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[Marcador Mundo]");
        AppendMarkerWorldSection(detailsBuilder, includeDetails: true);

        if (summaryText != null)
        {
            summaryText.text = summaryBuilder.ToString().TrimEnd();
        }

        if (detailsText != null)
        {
            detailsText.gameObject.SetActive(detailsVisible);
            detailsText.text = detailsBuilder.ToString().TrimEnd();
        }

        if (detailsToggleLabel != null)
        {
            detailsToggleLabel.text = detailsVisible ? hideDetailsLabel : showDetailsLabel;
        }

        if (forceConsoleLog)
        {
            Debug.Log(detailsBuilder.ToString().TrimEnd(), this);
        }
    }

    private void AppendLocalGpsSection(StringBuilder builder, bool includeDetails)
    {
        if (deviceGpsService == null)
        {
            builder.AppendLine("Sin servicio GPS local");
            return;
        }

        var reading = deviceGpsService.CurrentReading;
        builder.AppendLine($"Estado {reading.Status} | Fix {reading.HasFix}");
        builder.AppendLine($"Lat {reading.Latitude:F6} | Lon {reading.Longitude:F6}");

        if (!includeDetails)
        {
            return;
        }

        builder.AppendLine($"Alt {reading.Altitude:F2} | Prec {reading.HorizontalAccuracy:F1}m");
        builder.AppendLine($"Timestamp {reading.DeviceTimestamp:F0}");
        builder.AppendLine($"Permiso {deviceGpsService.HasLocationPermission} | SO habilitado {deviceGpsService.IsLocationServiceEnabledByUser}");
        builder.AppendLine($"Tracking {deviceGpsService.IsTrackingRequested} | Intentos {deviceGpsService.StartupAttemptCount}");
        builder.AppendLine($"Diag: {deviceGpsService.LastDiagnosticMessage}");
    }

    private void AppendSyncSection(StringBuilder builder, bool includeDetails)
    {
        if (gpsStateSync == null)
        {
            builder.AppendLine("Sin servicio de sincronizacion GPS");
            return;
        }

        if (gpsStateSync.HasSubmittedLocalState)
        {
            var submitted = gpsStateSync.LastSubmittedLocalState;
            builder.AppendLine($"Enviado local: {submitted.Latitude:F6}, {submitted.Longitude:F6}");
            builder.AppendLine($"Estado {(LocationServiceStatus)submitted.GpsStatus} | Fix {submitted.HasFix}");
        }
        else
        {
            builder.AppendLine("Sin envios locales todavia");
        }

        if (gpsStateSync.HasReceivedState)
        {
            var received = gpsStateSync.LastReceivedState;
            builder.AppendLine($"Ultimo recibido {BuildPlayerLabel(received.ClientId)}: {received.Latitude:F6}, {received.Longitude:F6}");
            if (includeDetails)
            {
                builder.AppendLine($"Alt {received.Altitude:F2} | Prec {received.HorizontalAccuracy:F1}m");
                builder.AppendLine($"Timestamp {received.DeviceTimestamp:F0} | Hace {Math.Max(0d, Time.realtimeSinceStartupAsDouble - gpsStateSync.LastReceiveRealtime):F1}s");
            }
        }
        else
        {
            builder.AppendLine("Sin recepciones remotas todavia");
        }
    }

    private void ResolveReferences()
    {
        deviceGpsService ??= FindFirstObjectByType<DeviceGpsService>(FindObjectsInactive.Include);
        gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        gpsMarkerController ??= FindFirstObjectByType<CoopGpsMarkerController>(FindObjectsInactive.Include);
        summaryText ??= FindOrCreateTextByName("SummaryText", 18f, TextAlignmentOptions.TopLeft);
        detailsText ??= FindOrCreateTextByName("DetailsText", 18f, TextAlignmentOptions.TopLeft);
        detailsToggleButton ??= FindComponentInChild<Button>("DetailsToggleButton");

        if (detailsToggleLabel == null)
        {
            var toggleTransform = FindChildRecursive(transform, "DetailsToggleButton");
            if (toggleTransform != null)
            {
                var labelTransform = FindChildRecursive(toggleTransform, "Label");
                if (labelTransform != null)
                {
                    detailsToggleLabel = EnsureTextComponent(
                        labelTransform.gameObject,
                        18f,
                        TextAlignmentOptions.Center);
                }
                else
                {
                    detailsToggleLabel = toggleTransform.GetComponentInChildren<TMP_Text>(true);
                }
            }
        }
    }

    private void AppendProjectionSection(StringBuilder builder, bool includeDetails)
    {
        if (gpsMarkerController == null)
        {
            builder.AppendLine("Sin controlador de marcadores");
            return;
        }

        if (!gpsMarkerController.TryGetLocalMarkerDiagnostics(out var diagnostics))
        {
            builder.AppendLine("Sin diagnostico local de proyeccion");
            return;
        }

        builder.AppendLine($"Lat {diagnostics.Latitude:F6} | Lon {diagnostics.Longitude:F6}");
        builder.AppendLine($"Placement {diagnostics.SurfacePlacementMode} | Offset {diagnostics.SurfacePlacementOffset:F2}");

        if (includeDetails)
        {
            builder.AppendLine($"Fallback demo {diagnostics.IsFallbackPosition}");
            builder.AppendLine($"Parent {diagnostics.ParentName} | ArcGIS init {diagnostics.IsArcGisInitialized}");
        }
    }

    private void AppendMarkerWorldSection(StringBuilder builder, bool includeDetails)
    {
        if (gpsMarkerController == null)
        {
            builder.AppendLine("Sin controlador de marcadores");
            return;
        }

        if (!gpsMarkerController.TryGetLocalMarkerDiagnostics(out var diagnostics))
        {
            builder.AppendLine("Sin diagnostico local de marcador");
            return;
        }

        builder.AppendLine($"Activo {diagnostics.IsActive} | Fix {diagnostics.HasFix} | Fallback {diagnostics.IsFallbackPosition}");
        builder.AppendLine($"World {diagnostics.WorldPosition.x:F1}, {diagnostics.WorldPosition.y:F1}, {diagnostics.WorldPosition.z:F1}");

        if (includeDetails)
        {
            builder.AppendLine($"Transform {diagnostics.TransformPosition.x:F1}, {diagnostics.TransformPosition.y:F1}, {diagnostics.TransformPosition.z:F1}");
            builder.AppendLine($"HP {diagnostics.HasHpTransform} | Universe {diagnostics.HpUniversePosition.x:F1}, {diagnostics.HpUniversePosition.y:F1}, {diagnostics.HpUniversePosition.z:F1}");
        }
    }

    private string BuildPlayerLabel(ulong clientId)
    {
        if (coopSessionCoordinator != null)
        {
            var slot = coopSessionCoordinator.GetPlayerSlot(clientId);
            if (slot >= 0)
            {
                var isLocal = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId;
                return isLocal ? $"Disp {slot + 1} (yo)" : $"Disp {slot + 1}";
            }
        }

        return $"Cliente {clientId}";
    }

    private TMP_Text FindOrCreateTextByName(
        string childName,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        var child = FindChildRecursive(transform, childName);
        return child != null
            ? EnsureTextComponent(child.gameObject, fontSize, alignment)
            : null;
    }

    private T FindComponentInChild<T>(string childName) where T : Component
    {
        var child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }

            var nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static TMP_Text EnsureTextComponent(
        GameObject target,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (target == null)
        {
            return null;
        }

        var existingText = target.GetComponent<TMP_Text>();
        if (existingText != null)
        {
            return existingText;
        }

        var createdText = target.AddComponent<TextMeshProUGUI>();
        createdText.raycastTarget = false;
        createdText.fontSize = fontSize;
        createdText.enableAutoSizing = false;
        createdText.alignment = alignment;
        createdText.textWrappingMode = TextWrappingModes.Normal;
        createdText.overflowMode = TextOverflowModes.Overflow;
        createdText.color = Color.white;
        return createdText;
    }
}
