using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.HPFramework;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopGpsMarkerController : MonoBehaviour
{
    private const ulong OfflineLocalMarkerClientId = ulong.MaxValue;

    [Header("References")]
    [SerializeField] private DeviceGpsService deviceGpsService;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private ArcGISMapCoordinateProjector mapCoordinateProjector;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private Transform markerRoot;

    [Header("Network Publish")]
    [SerializeField] [Min(0.1f)] private float publishIntervalSeconds = 0.5f;

    [Header("Marker Visuals")]
    [SerializeField] private GameObject markerTemplate;
    [SerializeField] private Color localPlayerColor = new(0.12f, 0.52f, 0.95f, 1f);
    [SerializeField] private Color remotePlayerColor = new(1f, 0.52f, 0.08f, 1f);
    [SerializeField] [Min(0.1f)] private float markerScale = 10f;
    [SerializeField] [Min(0f)] private float markerVisualHeightOffset = 12f;
    [SerializeField] [Min(0f)] private double markerSurfaceOffsetMeters = 3d;
    [SerializeField] private bool allowOfflineLocalMarker = true;

    [Header("Investigation Modes")]
    [SerializeField] private bool showLocalMarkerWithoutSync;
    [SerializeField] private bool showMarkerWithManualLatLon;
    [SerializeField] private bool showMarkerWithSyncedState = true;
    [SerializeField] private double manualLatitude = 39.9936d;
    [SerializeField] private double manualLongitude = -0.0665d;
    [SerializeField] private double manualAltitudeMeters = 0d;
    [SerializeField] private bool forceMarkersUnderArcGISMap = true;
    [SerializeField] private ArcGISSurfacePlacementMode investigationSurfacePlacementMode = ArcGISSurfacePlacementMode.RelativeToGround;

    private readonly Dictionary<ulong, PlayerMarkerView> markerViews = new();
    private readonly Dictionary<ulong, MarkerDiagnostics> markerDiagnostics = new();
    private DeviceGpsReading latestLocalReading;
    private bool hasLocalReading;
    private float nextPublishTime;
    private ArcGISMapComponent arcGISMap;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (deviceGpsService != null)
        {
            deviceGpsService.ReadingUpdated -= HandleLocalReadingUpdated;
            deviceGpsService.ReadingUpdated += HandleLocalReadingUpdated;
            latestLocalReading = deviceGpsService.CurrentReading;
            hasLocalReading = true;
        }

        SubscribeToGpsSync();
        RefreshAllMarkers();
    }

    private void OnDisable()
    {
        if (deviceGpsService != null)
        {
            deviceGpsService.ReadingUpdated -= HandleLocalReadingUpdated;
        }

        if (gpsStateSync != null)
        {
            gpsStateSync.StatesChanged -= HandleStatesChanged;
        }
    }

    private void Update()
    {
        ResolveReferences();
        SubscribeToGpsSync();

        if (!hasLocalReading)
        {
            return;
        }

        if (showLocalMarkerWithoutSync || !showMarkerWithSyncedState)
        {
            return;
        }

        if (gpsStateSync != null && Time.unscaledTime >= nextPublishTime)
        {
            nextPublishTime = Time.unscaledTime + publishIntervalSeconds;
            gpsStateSync.SubmitLocalReading(latestLocalReading);
        }
    }

    private void HandleLocalReadingUpdated(DeviceGpsReading reading)
    {
        latestLocalReading = reading;
        hasLocalReading = true;

        if (TryGetResolvedLocalMarkerClientId(out var localMarkerClientId))
        {
            UpdateMarker(localMarkerClientId, reading.Latitude, reading.Longitude, reading.Altitude, reading.HasFix, true);
        }
    }

    private void HandleStatesChanged()
    {
        RefreshAllMarkers();
    }

    private void RefreshAllMarkers()
    {
        var activeClientIds = new HashSet<ulong>();
        var hasLocalClientId = TryGetResolvedLocalMarkerClientId(out var localClientId);

        if (showMarkerWithSyncedState && gpsStateSync != null && NetworkManager.Singleton != null)
        {
            foreach (var state in gpsStateSync.PlayerStates)
            {
                var isLocal = hasLocalClientId && state.ClientId == localClientId;
                activeClientIds.Add(state.ClientId);
                UpdateMarker(state.ClientId, state.Latitude, state.Longitude, state.Altitude, state.HasFix, isLocal);
            }
        }

        if (hasLocalClientId && showMarkerWithManualLatLon)
        {
            activeClientIds.Add(localClientId);
            UpdateMarker(localClientId, manualLatitude, manualLongitude, manualAltitudeMeters, true, true);
        }
        else if (hasLocalReading && hasLocalClientId)
        {
            activeClientIds.Add(localClientId);

            if (showLocalMarkerWithoutSync || !showMarkerWithSyncedState || latestLocalReading.HasFix)
            {
                UpdateMarker(localClientId, latestLocalReading.Latitude, latestLocalReading.Longitude, latestLocalReading.Altitude, latestLocalReading.HasFix, true);
            }
        }

        RemoveInactiveMarkers(activeClientIds);
    }

    private void UpdateMarker(ulong clientId, double latitude, double longitude, double altitude, bool hasFix, bool isLocal)
    {
        if (!markerViews.TryGetValue(clientId, out var markerView))
        {
            markerView = CreateMarker(clientId, isLocal);
            if (markerView.Root == null)
            {
                return;
            }

            markerViews.Add(clientId, markerView);
        }

        markerView.Root.SetActive(hasFix);
        if (!hasFix)
        {
            CacheMarkerDiagnostics(clientId, markerView, latitude, longitude, altitude, false, isLocal);
            return;
        }

        mapCoordinateProjector?.ApplyGeographicPosition(
            markerView.LocationComponent,
            latitude,
            longitude,
            altitude);

        ApplySurfacePlacement(markerView);
        ApplyVisualSettings(markerView);

        if (markerView.Renderer != null && markerView.Renderer.material != null)
        {
            markerView.Renderer.material.color = isLocal ? localPlayerColor : remotePlayerColor;
        }

        CacheMarkerDiagnostics(clientId, markerView, latitude, longitude, altitude, hasFix, isLocal);
    }

    public bool TryGetMarkerWorldPosition(ulong clientId, out Vector3 worldPosition)
    {
        if (markerViews.TryGetValue(clientId, out var markerView) &&
            markerView.Root != null &&
            markerView.Root.activeInHierarchy)
        {
            if (TryResolveMarkerWorldPosition(markerView, out worldPosition))
            {
                return true;
            }
        }

        worldPosition = default;
        return false;
    }

    public bool TryGetLocalMarkerWorldPosition(out Vector3 worldPosition)
    {
        if (TryGetResolvedLocalMarkerClientId(out var localClientId))
        {
            return TryGetMarkerWorldPosition(localClientId, out worldPosition);
        }

        worldPosition = default;
        return false;
    }

    public bool TryGetLocalMarkerDiagnostics(out MarkerDiagnostics diagnostics)
    {
        if (TryGetResolvedLocalMarkerClientId(out var localClientId) &&
            markerDiagnostics.TryGetValue(localClientId, out diagnostics))
        {
            return true;
        }

        diagnostics = default;
        return false;
    }

    private void RemoveInactiveMarkers(HashSet<ulong> activeClientIds)
    {
        var staleClientIds = ListPool<ulong>.Get();
        foreach (var pair in markerViews)
        {
            if (!activeClientIds.Contains(pair.Key))
            {
                staleClientIds.Add(pair.Key);
            }
        }

        for (var index = 0; index < staleClientIds.Count; index++)
        {
            var clientId = staleClientIds[index];
            if (markerViews.TryGetValue(clientId, out var markerView))
            {
                Destroy(markerView.Root);
                markerViews.Remove(clientId);
                markerDiagnostics.Remove(clientId);
            }
        }

        ListPool<ulong>.Release(staleClientIds);
    }

    private PlayerMarkerView CreateMarker(ulong clientId, bool isLocal)
    {
        if (markerTemplate == null)
        {
            Debug.LogError("GPS marker template is not configured.", this);
            return default;
        }

        var markerName = isLocal ? "LocalGpsMarker" : $"RemoteGpsMarker_{clientId}";
        var root = Instantiate(markerTemplate, GetMarkerParent());
        root.name = markerName;
        root.SetActive(true);

        var locationComponent = root.GetComponent<ArcGISLocationComponent>();
        var renderer = root.GetComponentInChildren<MeshRenderer>(true);
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.material = new Material(renderer.sharedMaterial);
        }

        var visualTransform = renderer != null ? renderer.transform : null;
        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.up * markerVisualHeightOffset;
            visualTransform.localScale = Vector3.one * markerScale;
        }

        var hpTransform = root.GetComponent<HPTransform>();
        var hpRoot = root.GetComponentInParent<HPRoot>();

        return new PlayerMarkerView(root, locationComponent, renderer, visualTransform, hpTransform, hpRoot);
    }

    private bool TryGetResolvedLocalMarkerClientId(out ulong clientId)
    {
        if (NetworkManager.Singleton != null)
        {
            clientId = NetworkManager.Singleton.LocalClientId;
            return true;
        }

        if (allowOfflineLocalMarker)
        {
            clientId = OfflineLocalMarkerClientId;
            return true;
        }

        clientId = default;
        return false;
    }

    private void ApplyVisualSettings(PlayerMarkerView markerView)
    {
        if (markerView.VisualTransform == null)
        {
            return;
        }

        markerView.VisualTransform.localPosition = Vector3.up * markerVisualHeightOffset;
        markerView.VisualTransform.localScale = Vector3.one * markerScale;
    }

    private void ApplySurfacePlacement(PlayerMarkerView markerView)
    {
        if (markerView.LocationComponent == null)
        {
            return;
        }

        markerView.LocationComponent.SurfacePlacementMode = investigationSurfacePlacementMode;
        markerView.LocationComponent.SurfacePlacementOffset = markerSurfaceOffsetMeters;
    }

    private bool TryResolveMarkerWorldPosition(PlayerMarkerView markerView, out Vector3 worldPosition)
    {
        if (markerView.HpTransform != null && markerView.HpRoot != null)
        {
            worldPosition = markerView.HpRoot.TransformPoint(markerView.HpTransform.UniversePosition).ToVector3();
            return true;
        }

        worldPosition = markerView.Root.transform.position;
        return markerView.Root != null;
    }

    private void ResolveReferences()
    {
        deviceGpsService ??= GetComponent<DeviceGpsService>();
        mapCoordinateProjector ??= GetComponent<ArcGISMapCoordinateProjector>();
        arcGISMap ??= FindFirstObjectByType<ArcGISMapComponent>(FindObjectsInactive.Include);

        if (markerRoot == null)
        {
            markerRoot = transform;
        }

        if (markerTemplate == null)
        {
            var templateTransform = transform.Find("GpsMarkerTemplate");
            if (templateTransform != null)
            {
                markerTemplate = templateTransform.gameObject;
            }
        }

        if (coopSessionCoordinator == null)
        {
            coopSessionCoordinator = FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        }
    }

    private Transform GetMarkerParent()
    {
        if (!forceMarkersUnderArcGISMap || arcGISMap == null)
        {
            return markerRoot != null ? markerRoot : transform;
        }

        if (markerRoot != null && markerRoot.IsChildOf(arcGISMap.transform))
        {
            return markerRoot;
        }

        return arcGISMap.transform;
    }

    private void CacheMarkerDiagnostics(
        ulong clientId,
        PlayerMarkerView markerView,
        double latitude,
        double longitude,
        double altitude,
        bool hasFix,
        bool isLocal)
    {
        var hasWorldPosition = TryResolveMarkerWorldPosition(markerView, out var worldPosition);
        var universePosition = markerView.HpTransform != null ? markerView.HpTransform.UniversePosition.ToVector3() : Vector3.zero;
        markerDiagnostics[clientId] = new MarkerDiagnostics(
            clientId,
            isLocal,
            latitude,
            longitude,
            altitude,
            markerView.LocationComponent != null ? markerView.LocationComponent.SurfacePlacementMode : investigationSurfacePlacementMode,
            markerView.LocationComponent != null ? markerView.LocationComponent.SurfacePlacementOffset : markerSurfaceOffsetMeters,
            markerView.Root != null && markerView.Root.activeSelf,
            hasFix,
            hasWorldPosition,
            worldPosition,
            markerView.Root != null ? markerView.Root.transform.position : Vector3.zero,
            markerView.HpTransform != null,
            universePosition,
            markerView.Root != null && markerView.Root.transform.parent != null ? markerView.Root.transform.parent.name : string.Empty,
            TryReadArcGisInitializedFlag(markerView.LocationComponent));
    }

    private static bool TryReadArcGisInitializedFlag(ArcGISLocationComponent locationComponent)
    {
        if (locationComponent == null)
        {
            return false;
        }

        var type = locationComponent.GetType();
        var property = type.GetProperty("IsInitialized");
        if (property != null && property.PropertyType == typeof(bool))
        {
            return (bool)property.GetValue(locationComponent);
        }

        var field = type.GetField("isInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(locationComponent);
        }

        return false;
    }

    private void SubscribeToGpsSync()
    {
        var resolvedSync = gpsStateSync;
        if (resolvedSync == null)
        {
            resolvedSync = FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        }

        if (resolvedSync == null)
        {
            return;
        }

        if (gpsStateSync != null && gpsStateSync != resolvedSync)
        {
            gpsStateSync.StatesChanged -= HandleStatesChanged;
        }

        gpsStateSync = resolvedSync;
        gpsStateSync.StatesChanged -= HandleStatesChanged;
        gpsStateSync.StatesChanged += HandleStatesChanged;
        RefreshAllMarkers();
    }

    private readonly struct PlayerMarkerView
    {
        public PlayerMarkerView(
            GameObject root,
            ArcGISLocationComponent locationComponent,
            MeshRenderer renderer,
            Transform visualTransform,
            HPTransform hpTransform,
            HPRoot hpRoot)
        {
            Root = root;
            LocationComponent = locationComponent;
            Renderer = renderer;
            VisualTransform = visualTransform;
            HpTransform = hpTransform;
            HpRoot = hpRoot;
        }

        public GameObject Root { get; }
        public ArcGISLocationComponent LocationComponent { get; }
        public MeshRenderer Renderer { get; }
        public Transform VisualTransform { get; }
        public HPTransform HpTransform { get; }
        public HPRoot HpRoot { get; }
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}

public readonly struct MarkerDiagnostics
{
    public MarkerDiagnostics(
        ulong clientId,
        bool isLocal,
        double latitude,
        double longitude,
        double altitude,
        ArcGISSurfacePlacementMode surfacePlacementMode,
        double surfacePlacementOffset,
        bool isActive,
        bool hasFix,
        bool hasWorldPosition,
        Vector3 worldPosition,
        Vector3 transformPosition,
        bool hasHpTransform,
        Vector3 hpUniversePosition,
        string parentName,
        bool isArcGisInitialized)
    {
        ClientId = clientId;
        IsLocal = isLocal;
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        SurfacePlacementMode = surfacePlacementMode;
        SurfacePlacementOffset = surfacePlacementOffset;
        IsActive = isActive;
        HasFix = hasFix;
        HasWorldPosition = hasWorldPosition;
        WorldPosition = worldPosition;
        TransformPosition = transformPosition;
        HasHpTransform = hasHpTransform;
        HpUniversePosition = hpUniversePosition;
        ParentName = parentName;
        IsArcGisInitialized = isArcGisInitialized;
    }

    public ulong ClientId { get; }
    public bool IsLocal { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public double Altitude { get; }
    public ArcGISSurfacePlacementMode SurfacePlacementMode { get; }
    public double SurfacePlacementOffset { get; }
    public bool IsActive { get; }
    public bool HasFix { get; }
    public bool HasWorldPosition { get; }
    public Vector3 WorldPosition { get; }
    public Vector3 TransformPosition { get; }
    public bool HasHpTransform { get; }
    public Vector3 HpUniversePosition { get; }
    public string ParentName { get; }
    public bool IsArcGisInitialized { get; }
}
