using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

[DisallowMultipleComponent]
public sealed class DeviceGpsService : MonoBehaviour
{
    private const float DefaultStartTimeoutSeconds = 20f;
    private const float PermissionWaitTimeoutSeconds = 15f;
    private const double CoordinateEpsilon = 0.0000001d;

    [Header("GPS Polling")]
    [SerializeField] [Min(0.1f)] private float desiredAccuracyInMeters = 5f;
    [SerializeField] [Min(0.1f)] private float updateDistanceInMeters = 1f;
    [SerializeField] [Min(0.1f)] private float pollingIntervalSeconds = 0.5f;
    [SerializeField] [Min(1f)] private float startupTimeoutSeconds = DefaultStartTimeoutSeconds;
    [SerializeField] [Min(0)] private int maxStartupRetries = 2;
    [SerializeField] [Min(0.1f)] private float retryDelaySeconds = 2f;

    [Header("Debug")]
    [SerializeField] private bool logStatusChangesToConsole = true;

    private Coroutine trackingCoroutine;
    private bool isTrackingRequested;
    private DeviceGpsReading currentReading;
    private bool hasLocationPermission;
    private bool isLocationServiceEnabledByUser;
    private int startupAttemptCount;
    private string lastDiagnosticMessage = "Sin inicializar";

    public DeviceGpsReading CurrentReading => currentReading;
    public bool IsTrackingRequested => isTrackingRequested;
    public bool HasLocationPermission => hasLocationPermission;
    public bool IsLocationServiceEnabledByUser => isLocationServiceEnabledByUser;
    public int StartupAttemptCount => startupAttemptCount;
    public string LastDiagnosticMessage => lastDiagnosticMessage;

    public event Action<DeviceGpsReading> ReadingUpdated;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            BeginTracking();
        }
    }

    private void OnDisable()
    {
        EndTracking();
    }

    public void BeginTracking()
    {
        if (isTrackingRequested)
        {
            return;
        }

        isTrackingRequested = true;
        trackingCoroutine = StartCoroutine(TrackDeviceLocationCoroutine());
    }

    public void EndTracking()
    {
        isTrackingRequested = false;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }

        if (Input.location.status != LocationServiceStatus.Stopped)
        {
            Input.location.Stop();
        }
    }

    private IEnumerator TrackDeviceLocationCoroutine()
    {
        startupAttemptCount = 0;

        yield return RequestPermissionIfNeeded();
        if (!hasLocationPermission)
        {
            lastDiagnosticMessage = "Permiso de ubicacion no concedido.";
            UpdateReading(CreateStatusOnlyReading(LocationServiceStatus.Failed, false));
            yield break;
        }

        isLocationServiceEnabledByUser = Input.location.isEnabledByUser;
        if (!isLocationServiceEnabledByUser)
        {
            lastDiagnosticMessage = "La ubicacion del dispositivo esta desactivada en el sistema.";
            UpdateReading(CreateStatusOnlyReading(LocationServiceStatus.Failed, false));
            yield break;
        }

        while (isTrackingRequested)
        {
            startupAttemptCount++;
            StartLocationService();

            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(1f, startupTimeoutSeconds);
            while (isTrackingRequested &&
                   Input.location.status == LocationServiceStatus.Initializing &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                lastDiagnosticMessage = $"Inicializando GPS (intento {startupAttemptCount}/{Mathf.Max(1, maxStartupRetries + 1)}).";
                UpdateReading(CreateReadingFromLocationService(hasFixOverride: false));
                yield return new WaitForSecondsRealtime(pollingIntervalSeconds);
            }

            if (!isTrackingRequested)
            {
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                lastDiagnosticMessage = "GPS inicializado correctamente.";
                break;
            }

            if (startupAttemptCount > maxStartupRetries)
            {
                lastDiagnosticMessage = $"Tiempo de inicializacion agotado tras {startupAttemptCount} intentos.";
                UpdateReading(CreateStatusOnlyReading(LocationServiceStatus.Failed, false));
                yield break;
            }

            lastDiagnosticMessage = $"GPS atascado en Initializing. Reintentando en {retryDelaySeconds:0.0}s.";
            LogDiagnostic(lastDiagnosticMessage, true);
            Input.location.Stop();
            UpdateReading(CreateStatusOnlyReading(LocationServiceStatus.Initializing, false));
            yield return new WaitForSecondsRealtime(retryDelaySeconds);
        }

        while (isTrackingRequested)
        {
            UpdateReading(CreateReadingFromLocationService());
            yield return new WaitForSecondsRealtime(pollingIntervalSeconds);
        }
    }

    private IEnumerator RequestPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        hasLocationPermission = HasLocationPermissionGranted();
        if (!hasLocationPermission)
        {
            Permission.RequestUserPermission(Permission.FineLocation);

            var timeoutAt = Time.realtimeSinceStartup + PermissionWaitTimeoutSeconds;
            while (!HasLocationPermissionGranted() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            hasLocationPermission = HasLocationPermissionGranted();
        }
#else
        hasLocationPermission = true;
#endif
        yield break;
    }

    private void StartLocationService()
    {
        isLocationServiceEnabledByUser = Input.location.isEnabledByUser;
        lastDiagnosticMessage = $"Solicitando GPS del dispositivo (intento {startupAttemptCount}/{Mathf.Max(1, maxStartupRetries + 1)}).";
        LogDiagnostic(lastDiagnosticMessage, false);
        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        UpdateReading(CreateStatusOnlyReading(LocationServiceStatus.Initializing, false));
    }

    private static bool HasLocationPermissionGranted()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.FineLocation) ||
               Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#else
        return true;
#endif
    }

    private DeviceGpsReading CreateReadingFromLocationService(bool? hasFixOverride = null)
    {
        var status = Input.location.status;
        var locationData = Input.location.lastData;
        var hasFix = hasFixOverride ?? status == LocationServiceStatus.Running;

        return new DeviceGpsReading(
            locationData.latitude,
            locationData.longitude,
            locationData.altitude,
            locationData.horizontalAccuracy,
            locationData.timestamp,
            status,
            hasFix,
            Time.realtimeSinceStartupAsDouble);
    }

    private DeviceGpsReading CreateStatusOnlyReading(LocationServiceStatus status, bool hasFix)
    {
        return new DeviceGpsReading(
            currentReading.Latitude,
            currentReading.Longitude,
            currentReading.Altitude,
            currentReading.HorizontalAccuracy,
            currentReading.DeviceTimestamp,
            status,
            hasFix,
            Time.realtimeSinceStartupAsDouble);
    }

    private void UpdateReading(DeviceGpsReading nextReading)
    {
        if (!HasMeaningfulChange(currentReading, nextReading))
        {
            return;
        }

        var previousStatus = currentReading.Status;
        currentReading = nextReading;
        ReadingUpdated?.Invoke(currentReading);

        if (logStatusChangesToConsole && previousStatus != currentReading.Status)
        {
            Debug.Log($"[GPS] Status={currentReading.Status} HasFix={currentReading.HasFix} Lat={currentReading.Latitude:F6} Lon={currentReading.Longitude:F6} Diag={lastDiagnosticMessage}", this);
        }
    }

    private void LogDiagnostic(string message, bool warning)
    {
        if (!logStatusChangesToConsole)
        {
            return;
        }

        if (warning)
        {
            Debug.LogWarning($"[GPS] {message}", this);
            return;
        }

        Debug.Log($"[GPS] {message}", this);
    }

    private static bool HasMeaningfulChange(DeviceGpsReading previous, DeviceGpsReading next)
    {
        if (previous.Status != next.Status || previous.HasFix != next.HasFix)
        {
            return true;
        }

        if (Math.Abs(previous.Latitude - next.Latitude) > CoordinateEpsilon ||
            Math.Abs(previous.Longitude - next.Longitude) > CoordinateEpsilon ||
            Math.Abs(previous.Altitude - next.Altitude) > 0.01d ||
            Math.Abs(previous.HorizontalAccuracy - next.HorizontalAccuracy) > 0.01f)
        {
            return true;
        }

        return Math.Abs(previous.LastUpdatedRealtime - next.LastUpdatedRealtime) >= 1d;
    }
}

public readonly struct DeviceGpsReading
{
    public DeviceGpsReading(
        double latitude,
        double longitude,
        double altitude,
        float horizontalAccuracy,
        double deviceTimestamp,
        LocationServiceStatus status,
        bool hasFix,
        double lastUpdatedRealtime)
    {
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        HorizontalAccuracy = horizontalAccuracy;
        DeviceTimestamp = deviceTimestamp;
        Status = status;
        HasFix = hasFix;
        LastUpdatedRealtime = lastUpdatedRealtime;
    }

    public double Latitude { get; }
    public double Longitude { get; }
    public double Altitude { get; }
    public float HorizontalAccuracy { get; }
    public double DeviceTimestamp { get; }
    public LocationServiceStatus Status { get; }
    public bool HasFix { get; }
    public double LastUpdatedRealtime { get; }
}
