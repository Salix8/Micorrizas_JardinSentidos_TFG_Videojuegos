using System;
using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

[DisallowMultipleComponent]
public class ArcGISMobileGpsBlueDot : MonoBehaviour
{
    private const string MarkerRootName = "GPS Blue Dot";
    private const string MarkerVisualName = "Blue Dot Visual";
    private const string RemoteMarkerRootPrefix = "GPS Blue Dot Remote ";
    private const string RemoteMarkerVisualName = "Remote Blue Dot Visual";

    [Header("Location Service")]
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private float desiredAccuracyInMeters = 5f;
    [SerializeField] private float updateDistanceInMeters = 2f;
    [SerializeField] private int initializationTimeoutSeconds = 20;
    [SerializeField] private bool useDeviceAltitude = false;

    [Header("Marker")]
    [SerializeField] private ArcGISSurfacePlacementMode surfacePlacementMode = ArcGISSurfacePlacementMode.RelativeToGround;
    [SerializeField] private float surfaceOffsetMeters = 1.5f;
    [SerializeField] private Color markerColor = new(0.15f, 0.48f, 1f, 1f);
    [SerializeField] private float minMarkerScale = 10f;
    [SerializeField] private float maxMarkerScale = 45f;
    [SerializeField] private float distanceScaleFactor = 0.03f;

    [Header("Co-op Marker")]
    [SerializeField] private Color remoteMarkerColor = new(1f, 0.62f, 0.15f, 1f);
    [SerializeField] private float remoteMarkerScaleMultiplier = 1.15f;

#if UNITY_EDITOR
    [Header("Editor Simulation")]
    [SerializeField] private bool simulateLocationInEditor = false;
    [SerializeField] private double simulatedLatitude = 39.9936;
    [SerializeField] private double simulatedLongitude = -0.0665;
    [SerializeField] private double simulatedAltitude = 0.0;
    [SerializeField] private float simulatedPlayerSpacingMeters = 6f;
#endif

    private ArcGISMapComponent mapComponent;
    private ArcGISSpatialReference wgs84;
    private Camera targetCamera;
    private GameObject markerRoot;
    private ArcGISLocationComponent markerLocationComponent;
    private Transform markerVisual;
    private Renderer markerRenderer;
    private Coroutine locationStartupCoroutine;
    private bool hasLocationFix;
    private double lastLocationTimestamp = double.NegativeInfinity;
    private LocationInfo lastLocationInfo;
    private ArcGISPoint currentGeographicPosition;
    private Vector3 currentEnginePosition;
    private CoopSessionCoordinator coopSessionCoordinator;
    private readonly Dictionary<ulong, RemoteMarkerView> remoteMarkers = new();
#if UNITY_EDITOR
    private bool loggedEditorSimulation;
#endif

    public bool HasLocationFix => hasLocationFix;
    public ArcGISPoint CurrentGeographicPosition => currentGeographicPosition;
    public Vector3 CurrentEnginePosition => currentEnginePosition;
    public LocationInfo LastLocationInfo => lastLocationInfo;
    public ArcGISMapComponent MapComponent => mapComponent;
    public ArcGISSurfacePlacementMode SurfacePlacementMode => surfacePlacementMode;
    public float SurfaceOffsetMeters => surfaceOffsetMeters;
    public float MinMarkerScale => minMarkerScale;
    public float MaxMarkerScale => maxMarkerScale;
    public float DistanceScaleFactor => distanceScaleFactor;

    public event Action<ArcGISPoint, Vector3, LocationInfo> LocationUpdated;

    private void Awake()
    {
        mapComponent = GetComponent<ArcGISMapComponent>() ?? GetComponentInParent<ArcGISMapComponent>();

        if (mapComponent == null)
        {
            Debug.LogWarning($"{nameof(ArcGISMobileGpsBlueDot)} needs to be placed on or under an ArcGISMapComponent.", this);
            enabled = false;
            return;
        }

        wgs84 = ArcGISSpatialReference.WGS84();
        EnsureMarkerExists();
        UpdateMarkerVisibility(false);
    }

    private void OnEnable()
    {
        ResolveSessionCoordinatorIfNeeded();
        AttachToSessionCoordinator();

        if (!startOnEnable || !Application.isPlaying)
        {
            return;
        }

        StartTracking();
    }

    private void OnDisable()
    {
        DetachFromSessionCoordinator();

        if (locationStartupCoroutine != null)
        {
            StopCoroutine(locationStartupCoroutine);
            locationStartupCoroutine = null;
        }

#if !UNITY_EDITOR
        Input.location.Stop();
#else
        if (!ShouldUseEditorSimulation())
        {
            Input.location.Stop();
        }
#endif

        UpdateMarkerVisibility(false);
        ClearRemoteMarkers();
    }

    private void Update()
    {
        if (!Application.isPlaying || mapComponent == null)
        {
            return;
        }

        ResolveCameraIfNeeded();
        ResolveSessionCoordinatorIfNeeded();

#if UNITY_EDITOR
        if (ShouldUseEditorSimulation())
        {
            if (!loggedEditorSimulation)
            {
                Debug.Log("ArcGISMobileGpsBlueDot is using the simulated editor location. Disable 'Simulate Location In Editor' on the component to use live device GPS in a build.", this);
                loggedEditorSimulation = true;
            }

            var simulatedPosition = GetEditorSimulationPosition();
            ApplyGeographicPosition(
                simulatedPosition.latitude,
                simulatedPosition.longitude,
                simulatedPosition.altitude,
                new LocationInfo());

            return;
        }
#endif

        PollDeviceLocation();
        RefreshCurrentEnginePosition();
        UpdateMarkerScale();
        RefreshRemoteMarkers();
    }

    public void StartTracking()
    {
        if (locationStartupCoroutine == null)
        {
            locationStartupCoroutine = StartCoroutine(StartLocationServiceCoroutine());
        }
    }

    private IEnumerator StartLocationServiceCoroutine()
    {
#if UNITY_EDITOR
        if (ShouldUseEditorSimulation())
        {
            locationStartupCoroutine = null;
            yield break;
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services are disabled on this device. The GPS blue dot cannot start.", this);
            locationStartupCoroutine = null;
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);

            var permissionWait = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) &&
                   permissionWait < initializationTimeoutSeconds)
            {
                permissionWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.LogWarning("Location permission was not granted. The GPS blue dot cannot start.", this);
                locationStartupCoroutine = null;
                yield break;
            }
        }
#endif

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        var remainingWait = initializationTimeoutSeconds;
        while (Input.location.status == LocationServiceStatus.Initializing && remainingWait > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingWait--;
        }

        if (remainingWait <= 0)
        {
            Debug.LogWarning("Location service initialization timed out.", this);
            Input.location.Stop();
            locationStartupCoroutine = null;
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("Location service failed to start.", this);
            locationStartupCoroutine = null;
            yield break;
        }

        PollDeviceLocation(force: true);
        locationStartupCoroutine = null;
    }

    private void PollDeviceLocation(bool force = false)
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            return;
        }

        var latestLocation = Input.location.lastData;
        if (!force && Math.Abs(latestLocation.timestamp - lastLocationTimestamp) < 0.001d)
        {
            return;
        }

        lastLocationTimestamp = latestLocation.timestamp;
        lastLocationInfo = latestLocation;

        var altitude = useDeviceAltitude ? latestLocation.altitude : 0.0;
        if (surfacePlacementMode == ArcGISSurfacePlacementMode.AbsoluteHeight)
        {
            altitude += surfaceOffsetMeters;
        }

        ApplyGeographicPosition(latestLocation.latitude, latestLocation.longitude, altitude, latestLocation);
    }

    private void ApplyGeographicPosition(double latitude, double longitude, double altitude, LocationInfo locationInfo)
    {
        if (markerLocationComponent == null)
        {
            EnsureMarkerExists();
        }

        var geographicPoint = new ArcGISPoint(longitude, latitude, altitude, wgs84);

        currentGeographicPosition = geographicPoint;
        hasLocationFix = true;

        if (markerRoot != null && !markerRoot.activeSelf)
        {
            markerRoot.SetActive(true);
        }

        if (markerLocationComponent != null)
        {
            markerLocationComponent.SurfacePlacementMode = surfacePlacementMode;
            markerLocationComponent.SurfacePlacementOffset =
                surfacePlacementMode == ArcGISSurfacePlacementMode.AbsoluteHeight ? 0.0 : surfaceOffsetMeters;
            markerLocationComponent.Position = geographicPoint;
            markerLocationComponent.Rotation = new ArcGISRotation(0.0, 0.0, 0.0);
        }

        UpdateMarkerVisibility(true);
        RefreshCurrentEnginePosition();
        UpdateMarkerScale();
        PublishLocalPositionToSessionCoordinator();
        RefreshRemoteMarkers();
        LocationUpdated?.Invoke(currentGeographicPosition, currentEnginePosition, locationInfo);
    }

    private void RefreshCurrentEnginePosition()
    {
        if (!hasLocationFix || mapComponent == null || currentGeographicPosition == null || !mapComponent.HasSpatialReference())
        {
            return;
        }

        currentEnginePosition = mapComponent.GeographicToEngine(currentGeographicPosition);
    }

    private void EnsureMarkerExists()
    {
        if (markerLocationComponent != null)
        {
            return;
        }

        var existingMarker = transform.Find(MarkerRootName);
        markerRoot = existingMarker != null ? existingMarker.gameObject : new GameObject(MarkerRootName);
        markerRoot.transform.SetParent(transform, false);

        markerLocationComponent = markerRoot.GetComponent<ArcGISLocationComponent>();
        if (markerLocationComponent == null)
        {
            markerLocationComponent = markerRoot.AddComponent<ArcGISLocationComponent>();
        }

        markerLocationComponent.SurfacePlacementMode = surfacePlacementMode;
        markerLocationComponent.SurfacePlacementOffset =
            surfacePlacementMode == ArcGISSurfacePlacementMode.AbsoluteHeight ? 0.0 : surfaceOffsetMeters;

        var existingVisual = markerRoot.transform.Find(MarkerVisualName);
        if (existingVisual != null)
        {
            markerVisual = existingVisual;
            markerRenderer = markerVisual.GetComponent<Renderer>();
        }
        else
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = MarkerVisualName;
            visual.transform.SetParent(markerRoot.transform, false);
            visual.transform.localPosition = Vector3.up * (minMarkerScale * 0.5f);
            visual.transform.localScale = Vector3.one * minMarkerScale;

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            markerVisual = visual.transform;
            markerRenderer = visual.GetComponent<Renderer>();
        }

        ConfigureMarkerMaterial(markerRenderer, markerColor, "GPS Blue Dot Material");
    }

    private void UpdateMarkerVisibility(bool visible)
    {
        if (markerRoot != null)
        {
            markerRoot.SetActive(visible);
        }
    }

    private void UpdateMarkerScale()
    {
        if (!hasLocationFix || markerVisual == null || markerRoot == null || !markerRoot.activeInHierarchy)
        {
            return;
        }

        ResolveCameraIfNeeded();
        if (targetCamera == null)
        {
            return;
        }

        var cameraDistance = Vector3.Distance(targetCamera.transform.position, markerRoot.transform.position);
        var scale = Mathf.Clamp(cameraDistance * distanceScaleFactor, minMarkerScale, maxMarkerScale);
        markerVisual.localPosition = Vector3.up * (scale * 0.5f);
        markerVisual.localScale = Vector3.one * scale;
    }

    private void ResolveSessionCoordinatorIfNeeded()
    {
        if (coopSessionCoordinator != null)
        {
            return;
        }

        coopSessionCoordinator = FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
    }

    private void AttachToSessionCoordinator()
    {
        if (coopSessionCoordinator == null)
        {
            return;
        }

        coopSessionCoordinator.PlayerGpsStatesChanged -= HandlePlayerGpsStatesChanged;
        coopSessionCoordinator.SlotsChanged -= HandlePlayerGpsStatesChanged;
        coopSessionCoordinator.PlayerGpsStatesChanged += HandlePlayerGpsStatesChanged;
        coopSessionCoordinator.SlotsChanged += HandlePlayerGpsStatesChanged;
    }

    private void DetachFromSessionCoordinator()
    {
        if (coopSessionCoordinator == null)
        {
            return;
        }

        coopSessionCoordinator.PlayerGpsStatesChanged -= HandlePlayerGpsStatesChanged;
        coopSessionCoordinator.SlotsChanged -= HandlePlayerGpsStatesChanged;
    }

    private void HandlePlayerGpsStatesChanged()
    {
        RefreshRemoteMarkers();
    }

    private void PublishLocalPositionToSessionCoordinator()
    {
        if (!Application.isPlaying || !hasLocationFix)
        {
            return;
        }

        ResolveSessionCoordinatorIfNeeded();
        if (coopSessionCoordinator == null)
        {
            return;
        }

        coopSessionCoordinator.SubmitLocalPlayerGpsPosition(
            currentGeographicPosition.Y,
            currentGeographicPosition.X,
            currentGeographicPosition.Z);
    }

    private void RefreshRemoteMarkers()
    {
        if (coopSessionCoordinator == null || !coopSessionCoordinator.IsSpawned)
        {
            ClearRemoteMarkers();
            return;
        }

        var localClientId = coopSessionCoordinator.NetworkManager != null
            ? coopSessionCoordinator.NetworkManager.LocalClientId
            : ulong.MaxValue;

        var activeClientIds = new HashSet<ulong>();
        for (var index = 0; index < coopSessionCoordinator.PlayerGpsStateCount; index++)
        {
            if (!coopSessionCoordinator.TryGetPlayerGpsState(index, out var playerGpsState) ||
                !playerGpsState.HasLocationFix ||
                playerGpsState.ClientId == localClientId)
            {
                continue;
            }

            activeClientIds.Add(playerGpsState.ClientId);
            UpdateRemoteMarker(playerGpsState);
        }

        var staleMarkers = new List<ulong>();
        foreach (var markerEntry in remoteMarkers)
        {
            if (!activeClientIds.Contains(markerEntry.Key))
            {
                staleMarkers.Add(markerEntry.Key);
            }
        }

        for (var index = 0; index < staleMarkers.Count; index++)
        {
            RemoveRemoteMarker(staleMarkers[index]);
        }
    }

    private void UpdateRemoteMarker(CoopPlayerGpsState playerGpsState)
    {
        var marker = GetOrCreateRemoteMarker(playerGpsState.ClientId);
        if (marker == null)
        {
            return;
        }

        var geographicPosition = new ArcGISPoint(
            playerGpsState.Longitude,
            playerGpsState.Latitude,
            playerGpsState.Altitude,
            wgs84);

        marker.Location.SurfacePlacementMode = surfacePlacementMode;
        marker.Location.SurfacePlacementOffset =
            surfacePlacementMode == ArcGISSurfacePlacementMode.AbsoluteHeight ? 0.0 : surfaceOffsetMeters;
        marker.Location.Position = geographicPosition;
        marker.Location.Rotation = new ArcGISRotation(0.0, 0.0, 0.0);

        if (!marker.Root.activeSelf)
        {
            marker.Root.SetActive(true);
        }

        UpdateRemoteMarkerScale(marker);
    }

    private RemoteMarkerView GetOrCreateRemoteMarker(ulong clientId)
    {
        if (remoteMarkers.TryGetValue(clientId, out var existingMarker) && existingMarker.Root != null)
        {
            return existingMarker;
        }

        var rootName = RemoteMarkerRootPrefix + clientId;
        var existingRoot = transform.Find(rootName);
        var root = existingRoot != null ? existingRoot.gameObject : new GameObject(rootName);
        root.transform.SetParent(transform, false);

        var location = root.GetComponent<ArcGISLocationComponent>();
        if (location == null)
        {
            location = root.AddComponent<ArcGISLocationComponent>();
        }

        var visualTransform = root.transform.Find(RemoteMarkerVisualName);
        Renderer visualRenderer;
        if (visualTransform == null)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = RemoteMarkerVisualName;
            visual.transform.SetParent(root.transform, false);
            visualRenderer = visual.GetComponent<Renderer>();

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            visualTransform = visual.transform;
        }
        else
        {
            visualRenderer = visualTransform.GetComponent<Renderer>();
        }

        ConfigureMarkerMaterial(visualRenderer, remoteMarkerColor, "GPS Remote Dot Material");
        var marker = new RemoteMarkerView(root, location, visualTransform);
        remoteMarkers[clientId] = marker;
        return marker;
    }

    private void UpdateRemoteMarkerScale(RemoteMarkerView marker)
    {
        if (marker == null || marker.Root == null || marker.Visual == null || !marker.Root.activeInHierarchy)
        {
            return;
        }

        ResolveCameraIfNeeded();
        if (targetCamera == null)
        {
            return;
        }

        var cameraDistance = Vector3.Distance(targetCamera.transform.position, marker.Root.transform.position);
        var scale = Mathf.Clamp(cameraDistance * distanceScaleFactor, minMarkerScale, maxMarkerScale) * remoteMarkerScaleMultiplier;
        marker.Visual.localPosition = Vector3.up * (scale * 0.5f);
        marker.Visual.localScale = Vector3.one * scale;
    }

    private void RemoveRemoteMarker(ulong clientId)
    {
        if (!remoteMarkers.TryGetValue(clientId, out var marker))
        {
            return;
        }

        if (marker.Root != null)
        {
            Destroy(marker.Root);
        }

        remoteMarkers.Remove(clientId);
    }

    private void ClearRemoteMarkers()
    {
        foreach (var markerEntry in remoteMarkers)
        {
            if (markerEntry.Value.Root != null)
            {
                Destroy(markerEntry.Value.Root);
            }
        }

        remoteMarkers.Clear();
    }

    private void ResolveCameraIfNeeded()
    {
        if (targetCamera != null)
        {
            return;
        }

        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            return;
        }

        targetCamera = GetComponentInChildren<Camera>(true);
        if (targetCamera != null)
        {
            return;
        }

        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cameras.Length > 0)
        {
            targetCamera = cameras[0];
        }
    }

    private static void ConfigureMarkerMaterial(Renderer targetRenderer, Color color, string materialName)
    {
        if (targetRenderer == null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Standard") ??
                     Shader.Find("Unlit/Color") ??
                     Shader.Find("HDRP/Unlit");

        if (shader == null)
        {
            return;
        }

        var material = new Material(shader)
        {
            name = materialName
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissiveColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissiveColor", color * 2f);
        }

        targetRenderer.sharedMaterial = material;
        targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
        targetRenderer.receiveShadows = false;
        targetRenderer.lightProbeUsage = LightProbeUsage.Off;
        targetRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

#if UNITY_EDITOR
    private bool ShouldUseEditorSimulation()
    {
        return simulateLocationInEditor && !Application.isMobilePlatform;
    }

    private (double latitude, double longitude, double altitude) GetEditorSimulationPosition()
    {
        var localPlayerSlot = GetEditorSimulationPlayerSlot();
        if (localPlayerSlot <= 0 || simulatedPlayerSpacingMeters <= 0f)
        {
            return (simulatedLatitude, simulatedLongitude, simulatedAltitude);
        }

        var ringIndex = Mathf.CeilToInt(localPlayerSlot / 4f);
        var ringRadiusMeters = simulatedPlayerSpacingMeters * ringIndex;
        var directionIndex = (localPlayerSlot - 1) % 4;

        var eastMeters = 0f;
        var northMeters = 0f;

        switch (directionIndex)
        {
            case 0:
                eastMeters = ringRadiusMeters;
                break;
            case 1:
                northMeters = ringRadiusMeters;
                break;
            case 2:
                eastMeters = -ringRadiusMeters;
                break;
            default:
                northMeters = -ringRadiusMeters;
                break;
        }

        const double metersPerLatitudeDegree = 111320.0;
        var longitudeScale = Math.Cos(simulatedLatitude * Math.PI / 180.0);
        if (Math.Abs(longitudeScale) < 0.0001d)
        {
            longitudeScale = 0.0001d;
        }

        var latitudeOffset = northMeters / metersPerLatitudeDegree;
        var longitudeOffset = eastMeters / (metersPerLatitudeDegree * longitudeScale);

        return (
            simulatedLatitude + latitudeOffset,
            simulatedLongitude + longitudeOffset,
            simulatedAltitude);
    }

    private int GetEditorSimulationPlayerSlot()
    {
        ResolveSessionCoordinatorIfNeeded();
        if (coopSessionCoordinator == null || !coopSessionCoordinator.IsSpawned)
        {
            return 0;
        }

        return Mathf.Max(coopSessionCoordinator.GetLocalPlayerSlot(), 0);
    }
#endif

    private sealed class RemoteMarkerView
    {
        public RemoteMarkerView(GameObject root, ArcGISLocationComponent location, Transform visual)
        {
            Root = root;
            Location = location;
            Visual = visual;
        }

        public GameObject Root { get; }
        public ArcGISLocationComponent Location { get; }
        public Transform Visual { get; }
    }
}
