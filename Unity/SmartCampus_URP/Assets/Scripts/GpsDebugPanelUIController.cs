using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GpsDebugPanelUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DeviceGpsService deviceGpsService;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private Text summaryText;
    [SerializeField] private Text detailsText;
    [SerializeField] private Button detailsToggleButton;
    [SerializeField] private Text detailsToggleLabel;

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
    }
}
