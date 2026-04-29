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
        if (deviceGpsService != null)
        {
            var reading = deviceGpsService.CurrentReading;
            summaryBuilder.AppendLine($"Disp local: {reading.Latitude:F6}, {reading.Longitude:F6}");
        }
        else
        {
            summaryBuilder.AppendLine("Sin servicio GPS local");
        }

        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("[Sync Red]");
        if (gpsStateSync != null && gpsStateSync.PlayerStates.Count > 0)
        {
            foreach (var state in gpsStateSync.PlayerStates)
            {
                var label = BuildPlayerLabel(state.ClientId);
                summaryBuilder.AppendLine($"{label}: {state.Latitude:F6}, {state.Longitude:F6}");
            }
        }
        else if (gpsStateSync != null)
        {
            summaryBuilder.AppendLine("Sin estados sincronizados todavia");
        }
        else
        {
            summaryBuilder.AppendLine("Sin servicio de sincronizacion GPS");
        }

        detailsBuilder.AppendLine("GPS Debug Detallado");
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[Local Unity]");
        if (deviceGpsService != null)
        {
            var reading = deviceGpsService.CurrentReading;
            detailsBuilder.AppendLine("Disp local");
            detailsBuilder.AppendLine($"Lat {reading.Latitude:F6} | Lon {reading.Longitude:F6}");
            detailsBuilder.AppendLine($"Alt {reading.Altitude:F2} | Prec {reading.HorizontalAccuracy:F1}m");
            detailsBuilder.AppendLine($"Estado {reading.Status} | Fix {reading.HasFix}");
            detailsBuilder.AppendLine($"Timestamp {reading.DeviceTimestamp:F0}");
            detailsBuilder.AppendLine($"Permiso {deviceGpsService.HasLocationPermission} | SO habilitado {deviceGpsService.IsLocationServiceEnabledByUser}");
            detailsBuilder.AppendLine($"Tracking {deviceGpsService.IsTrackingRequested} | Intentos {deviceGpsService.StartupAttemptCount}");
            detailsBuilder.AppendLine($"Diag: {deviceGpsService.LastDiagnosticMessage}");
        }
        else
        {
            detailsBuilder.AppendLine("Sin servicio GPS local");
        }

        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine("[Sync Red]");
        if (gpsStateSync != null && gpsStateSync.PlayerStates.Count > 0)
        {
            foreach (var state in gpsStateSync.PlayerStates)
            {
                var label = BuildPlayerLabel(state.ClientId);
                detailsBuilder.AppendLine(label);
                detailsBuilder.AppendLine($"Lat {state.Latitude:F6} | Lon {state.Longitude:F6}");
                detailsBuilder.AppendLine($"Alt {state.Altitude:F2} | Prec {state.HorizontalAccuracy:F1}m");
                detailsBuilder.AppendLine($"Estado {(LocationServiceStatus)state.GpsStatus} | Fix {state.HasFix}");
                detailsBuilder.AppendLine($"Timestamp {state.DeviceTimestamp:F0}");
                detailsBuilder.AppendLine();
            }
        }
        else if (gpsStateSync != null)
        {
            detailsBuilder.AppendLine("Sin estados sincronizados todavia");
        }
        else
        {
            detailsBuilder.AppendLine("Sin servicio de sincronizacion GPS");
        }

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

    private void AppendPlayerState(CoopPlayerGpsState state)
    {
        var label = BuildPlayerLabel(state.ClientId);
        summaryBuilder.AppendLine($"{label}: {state.Latitude:F6}, {state.Longitude:F6}");

        detailsBuilder.AppendLine(label);
        detailsBuilder.AppendLine($"Lat {state.Latitude:F6} | Lon {state.Longitude:F6}");
        detailsBuilder.AppendLine($"Alt {state.Altitude:F2} | Prec {state.HorizontalAccuracy:F1}m");
        detailsBuilder.AppendLine($"Estado {(LocationServiceStatus)state.GpsStatus} | Fix {state.HasFix}");
        detailsBuilder.AppendLine($"Timestamp {state.DeviceTimestamp:F0}");
        detailsBuilder.AppendLine();
    }

    private void AppendLocalOnlyState(DeviceGpsReading reading)
    {
        var label = "Disp local";
        summaryBuilder.AppendLine($"{label}: {reading.Latitude:F6}, {reading.Longitude:F6}");

        detailsBuilder.AppendLine(label);
        detailsBuilder.AppendLine($"Lat {reading.Latitude:F6} | Lon {reading.Longitude:F6}");
        detailsBuilder.AppendLine($"Alt {reading.Altitude:F2} | Prec {reading.HorizontalAccuracy:F1}m");
        detailsBuilder.AppendLine($"Estado {reading.Status} | Fix {reading.HasFix}");
        detailsBuilder.AppendLine($"Timestamp {reading.DeviceTimestamp:F0}");

        if (deviceGpsService != null)
        {
            detailsBuilder.AppendLine($"Permiso {deviceGpsService.HasLocationPermission} | SO habilitado {deviceGpsService.IsLocationServiceEnabledByUser}");
            detailsBuilder.AppendLine($"Tracking {deviceGpsService.IsTrackingRequested} | Intentos {deviceGpsService.StartupAttemptCount}");
            detailsBuilder.AppendLine($"Diag: {deviceGpsService.LastDiagnosticMessage}");
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

    private void ResolveReferences()
    {
        deviceGpsService ??= FindFirstObjectByType<DeviceGpsService>(FindObjectsInactive.Include);
        gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
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
