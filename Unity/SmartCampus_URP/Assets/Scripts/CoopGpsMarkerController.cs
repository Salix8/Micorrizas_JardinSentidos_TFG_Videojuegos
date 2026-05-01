using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
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
    [SerializeField] [Min(0f)] private float markerVisualHeightOffset = 20f;
    [SerializeField] private double markerAltitudeOffsetMeters = 1.5d;
    [SerializeField] private bool allowOfflineLocalMarker = true;
    [SerializeField] private bool overrideMarkerAltitude = true;
    [SerializeField] private double markerAbsoluteAltitudeMeters = 860d;
    [SerializeField] private bool lockMarkerHeightToRoot = true;

    private readonly Dictionary<ulong, PlayerMarkerView> markerViews = new();
    private DeviceGpsReading latestLocalReading;
    private bool hasLocalReading;
    private float nextPublishTime;

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

        if (gpsStateSync != null && NetworkManager.Singleton != null)
        {
            foreach (var state in gpsStateSync.PlayerStates)
            {
                var isLocal = hasLocalClientId && state.ClientId == localClientId;
                activeClientIds.Add(state.ClientId);
                UpdateMarker(state.ClientId, state.Latitude, state.Longitude, state.Altitude, state.HasFix, isLocal);
            }
        }

        if (hasLocalReading && hasLocalClientId)
        {
            activeClientIds.Add(localClientId);
            UpdateMarker(localClientId, latestLocalReading.Latitude, latestLocalReading.Longitude, latestLocalReading.Altitude, latestLocalReading.HasFix, true);
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
            return;
        }

        mapCoordinateProjector?.ApplyGeographicPosition(
            markerView.LocationComponent,
            latitude,
            longitude,
            ResolveMarkerAltitude(altitude));

        ApplyRootHeight(markerView);
        ApplyVisualSettings(markerView);

        if (markerView.Renderer != null && markerView.Renderer.material != null)
        {
            markerView.Renderer.material.color = isLocal ? localPlayerColor : remotePlayerColor;
        }
    }

    public bool TryGetMarkerWorldPosition(ulong clientId, out Vector3 worldPosition)
    {
        if (markerViews.TryGetValue(clientId, out var markerView) &&
            markerView.Root != null &&
            markerView.Root.activeInHierarchy)
        {
            worldPosition = markerView.Root.transform.position;
            return true;
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
        var root = Instantiate(markerTemplate, markerRoot);
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

        return new PlayerMarkerView(root, locationComponent, renderer, visualTransform);
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

    private double ResolveMarkerAltitude(double deviceAltitudeMeters)
    {
        if (overrideMarkerAltitude)
        {
            return markerAbsoluteAltitudeMeters;
        }

        return deviceAltitudeMeters + markerAltitudeOffsetMeters;
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

    private void ApplyRootHeight(PlayerMarkerView markerView)
    {
        if (!lockMarkerHeightToRoot || markerRoot == null || markerView.Root == null)
        {
            return;
        }

        var worldPosition = markerView.Root.transform.position;
        worldPosition.y = markerRoot.position.y;
        markerView.Root.transform.position = worldPosition;
    }

    private void ResolveReferences()
    {
        deviceGpsService ??= GetComponent<DeviceGpsService>();
        mapCoordinateProjector ??= GetComponent<ArcGISMapCoordinateProjector>();

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

    private void SubscribeToGpsSync()
    {
        var resolvedSync = gpsStateSync;
        if (resolvedSync == null)
        {
            resolvedSync = FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        }

        if (resolvedSync == gpsStateSync || resolvedSync == null)
        {
            return;
        }

        if (gpsStateSync != null)
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
        public PlayerMarkerView(GameObject root, ArcGISLocationComponent locationComponent, MeshRenderer renderer, Transform visualTransform)
        {
            Root = root;
            LocationComponent = locationComponent;
            Renderer = renderer;
            VisualTransform = visualTransform;
        }

        public GameObject Root { get; }
        public ArcGISLocationComponent LocationComponent { get; }
        public MeshRenderer Renderer { get; }
        public Transform VisualTransform { get; }
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
