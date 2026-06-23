using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.HPFramework;
using SmartCampus.Coop.Minigames;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopGpsMarkerController : MonoBehaviour
{
    private const ulong OfflineLocalMarkerClientId = ulong.MaxValue;
    private const int GeneratedCircleSpriteSize = 64;

    [Header("References")]
    [SerializeField] private DeviceGpsService deviceGpsService;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private CoopPlayerProfileSync playerProfileSync;
    [SerializeField] private ArcGISMapCoordinateProjector mapCoordinateProjector;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private Transform markerRoot;

    [Header("Network Publish")]
    [SerializeField] [Min(0.1f)] private float publishIntervalSeconds = 0.5f;

    [Header("Marker Visuals")]
    [SerializeField] private GameObject markerTemplate;
    [SerializeField] private LocalPlayerMarkerProfileService localPlayerMarkerProfileService;
    [SerializeField] private PlayerMarkerAppearanceCatalogConfig localPlayerAppearanceCatalog;
    [SerializeField] private Color localPlayerColor = new(0.12f, 0.52f, 0.95f, 1f);
    [SerializeField] private Color remotePlayerColor = new(1f, 0.52f, 0.08f, 1f);
    [SerializeField] [Min(0.1f)] private float markerScale = 10f;
    [SerializeField] [Min(0f)] private float markerVisualHeightOffset = 12f;
    [SerializeField] private Vector3 markerVisualLocalEulerAngles = new(90f, 0f, 0f);
    [SerializeField] [Min(0.1f)] private float markerCircleDiameter = 1f;
    [SerializeField] [Range(0.1f, 1f)] private float markerAvatarFillRatio = 0.6f;
    [SerializeField] [Range(0f, 0.25f)] private float markerCircleBorderRatio = 0.08f;
    [SerializeField] private Color markerCircleColor = new(1f, 1f, 1f, 0.96f);
    [SerializeField] private Color markerCircleBorderColor = new(0.08f, 0.18f, 0.16f, 0.9f);
    [SerializeField] [Min(0f)] private double markerSurfaceOffsetMeters = 3d;
    [SerializeField] private bool allowOfflineLocalMarker = true;

    [Header("Investigation Modes")]
    [SerializeField] private bool showLocalMarkerWithoutSync;
    [SerializeField] private bool showMarkerWithManualLatLon;
    [SerializeField] private bool showMarkerWithSyncedState = true;
    [SerializeField] private bool showLocalFallbackWhenGpsUnavailableInEditor = true;
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
    private static Sprite generatedCircleSprite;

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
        SubscribeToPlayerProfiles();
        SubscribeToLocalProfile();
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

        if (playerProfileSync != null)
        {
            playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
        }

        if (localPlayerMarkerProfileService != null)
        {
            localPlayerMarkerProfileService.ProfileChanged -= HandleLocalProfileChanged;
        }
    }

    private void Update()
    {
        ResolveReferences();
        SubscribeToGpsSync();
        SubscribeToPlayerProfiles();
        SubscribeToLocalProfile();

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
            if (reading.HasFix)
            {
                UpdateMarker(localMarkerClientId, reading.Latitude, reading.Longitude, reading.Altitude, true, true);
                return;
            }

            if (ShouldShowLocalFallbackMarker(reading))
            {
                UpdateMarker(localMarkerClientId, manualLatitude, manualLongitude, manualAltitudeMeters, false, true, true);
            }
        }
    }

    private void HandleStatesChanged()
    {
        RefreshAllMarkers();
    }

    private void HandleProfilesChanged()
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
            UpdateMarker(localClientId, manualLatitude, manualLongitude, manualAltitudeMeters, false, true, true);
        }
        else if (hasLocalReading && hasLocalClientId)
        {
            if (latestLocalReading.HasFix)
            {
                activeClientIds.Add(localClientId);
                UpdateMarker(localClientId, latestLocalReading.Latitude, latestLocalReading.Longitude, latestLocalReading.Altitude, latestLocalReading.HasFix, true);
            }
            else if (ShouldShowLocalFallbackMarker(latestLocalReading))
            {
                activeClientIds.Add(localClientId);
                UpdateMarker(localClientId, manualLatitude, manualLongitude, manualAltitudeMeters, false, true, true);
            }
        }

        RemoveInactiveMarkers(activeClientIds);
    }

    private void UpdateMarker(
        ulong clientId,
        double latitude,
        double longitude,
        double altitude,
        bool hasFix,
        bool isLocal,
        bool isFallbackPosition = false)
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

        markerView = EnsureMarkerAppearance(markerView, clientId, isLocal);
        markerViews[clientId] = markerView;

        var shouldShowMarker = hasFix || isFallbackPosition;
        markerView.Root.SetActive(shouldShowMarker);
        if (!shouldShowMarker)
        {
            CacheMarkerDiagnostics(clientId, markerView, latitude, longitude, altitude, false, isLocal, isFallbackPosition);
            return;
        }

        mapCoordinateProjector?.ApplyGeographicPosition(
            markerView.LocationComponent,
            latitude,
            longitude,
            altitude);

        ApplySurfacePlacement(markerView);
        ApplyVisualSettings(markerView);

        if (markerView.Renderer != null && markerView.Renderer.material != null && markerView.SpriteRenderer == null)
        {
            markerView.Renderer.material.color = ResolveMarkerColor(isLocal);
        }

        CacheMarkerDiagnostics(clientId, markerView, latitude, longitude, altitude, hasFix, isLocal, isFallbackPosition);
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
        var markerView = BuildMarkerView(root, ResolveDesiredAvatarId(clientId, isLocal));
        ApplyVisualSettings(markerView);

        var hpTransform = root.GetComponent<HPTransform>();
        var hpRoot = root.GetComponentInParent<HPRoot>();

        return new PlayerMarkerView(root, locationComponent, markerView.Renderer, markerView.SpriteRenderer, markerView.VisualTransform, hpTransform, hpRoot, markerView.AvatarId);
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
        markerView.VisualTransform.localRotation = Quaternion.Euler(markerVisualLocalEulerAngles);
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

        if (localPlayerMarkerProfileService == null)
        {
            localPlayerMarkerProfileService = FindFirstObjectByType<LocalPlayerMarkerProfileService>(FindObjectsInactive.Include);
        }

        if (localPlayerAppearanceCatalog == null && localPlayerMarkerProfileService != null)
        {
            localPlayerAppearanceCatalog = localPlayerMarkerProfileService.AppearanceCatalog;
        }

        if (localPlayerAppearanceCatalog == null && playerProfileSync != null)
        {
            localPlayerAppearanceCatalog = playerProfileSync.AppearanceCatalog;
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
        bool isLocal,
        bool isFallbackPosition)
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
            isFallbackPosition,
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

    private bool ShouldShowLocalFallbackMarker(DeviceGpsReading reading)
    {
        return showLocalFallbackWhenGpsUnavailableInEditor &&
               IsEditorOrDevelopmentBuild() &&
               !reading.HasFix;
    }

    private static bool IsEditorOrDevelopmentBuild()
    {
#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
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

    private void SubscribeToLocalProfile()
    {
        if (localPlayerMarkerProfileService == null)
        {
            return;
        }

        localPlayerMarkerProfileService.ProfileChanged -= HandleLocalProfileChanged;
        localPlayerMarkerProfileService.ProfileChanged += HandleLocalProfileChanged;
    }

    private void SubscribeToPlayerProfiles()
    {
        var resolvedSync = playerProfileSync;
        if (resolvedSync == null)
        {
            resolvedSync = FindFirstObjectByType<CoopPlayerProfileSync>(FindObjectsInactive.Include);
        }

        if (resolvedSync == null)
        {
            return;
        }

        if (playerProfileSync != null && playerProfileSync != resolvedSync)
        {
            playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
        }

        playerProfileSync = resolvedSync;
        playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
        playerProfileSync.ProfilesChanged += HandleProfilesChanged;

        if (localPlayerAppearanceCatalog == null)
        {
            localPlayerAppearanceCatalog = playerProfileSync.AppearanceCatalog;
        }
    }

    private void HandleLocalProfileChanged()
    {
        RefreshAllMarkers();
    }

    private PlayerMarkerView EnsureMarkerAppearance(PlayerMarkerView markerView, ulong clientId, bool isLocal)
    {
        var desiredAvatarId = ResolveDesiredAvatarId(clientId, isLocal);
        if (string.Equals(markerView.AvatarId, desiredAvatarId, System.StringComparison.OrdinalIgnoreCase))
        {
            return markerView;
        }

        return BuildMarkerView(markerView.Root, desiredAvatarId, markerView.LocationComponent, markerView.HpTransform, markerView.HpRoot);
    }

    private string ResolveDesiredAvatarId(ulong clientId, bool isLocal)
    {
        if (playerProfileSync != null &&
            playerProfileSync.TryGetProfile(clientId, out var profile) &&
            !profile.AvatarId.IsEmpty)
        {
            return ResolveAvatarIdOrDefault(profile.AvatarId.ToString());
        }

        return isLocal ? ResolveDesiredLocalAvatarId() : ResolveAvatarIdOrDefault(string.Empty);
    }

    private string ResolveDesiredLocalAvatarId()
    {
        if (localPlayerMarkerProfileService == null)
        {
            return ResolveAvatarIdOrDefault(string.Empty);
        }

        return ResolveAvatarIdOrDefault(localPlayerMarkerProfileService.CurrentAvatarId);
    }

    private string ResolveAvatarIdOrDefault(string avatarId)
    {
        if (localPlayerAppearanceCatalog == null)
        {
            return string.IsNullOrWhiteSpace(avatarId) ? string.Empty : avatarId.Trim();
        }

        return localPlayerAppearanceCatalog.ResolveAvatarIdOrDefault(avatarId);
    }

    private Color ResolveMarkerColor(bool isLocal)
    {
        return isLocal ? localPlayerColor : remotePlayerColor;
    }

    private PlayerMarkerView BuildMarkerView(
        GameObject root,
        string desiredAvatarId,
        ArcGISLocationComponent locationComponent = null,
        HPTransform hpTransform = null,
        HPRoot hpRoot = null)
    {
        var resolvedAvatarId = ResolveAvatarIdOrDefault(desiredAvatarId);
        if (!string.IsNullOrWhiteSpace(resolvedAvatarId))
        {
            TryReplaceMarkerVisual(root.transform, resolvedAvatarId);
        }
        else
        {
            RemoveMarkerVisual(root.transform);
        }

        var spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);
        var renderer = spriteRenderer == null ? root.GetComponentInChildren<MeshRenderer>(true) : null;
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.material = new Material(renderer.sharedMaterial);
        }

        locationComponent ??= root.GetComponent<ArcGISLocationComponent>();
        hpTransform ??= root.GetComponent<HPTransform>();
        hpRoot ??= root.GetComponentInParent<HPRoot>();
        var visualRoot = root.transform.Find("Visual");
        var visualTransform = visualRoot != null ? visualRoot : spriteRenderer != null ? spriteRenderer.transform : renderer != null ? renderer.transform : null;
        return new PlayerMarkerView(root, locationComponent, renderer, spriteRenderer, visualTransform, hpTransform, hpRoot, resolvedAvatarId);
    }

    private bool TryReplaceMarkerVisual(Transform markerRootTransform, string desiredAvatarId)
    {
        RemoveMarkerVisual(markerRootTransform);

        if (localPlayerAppearanceCatalog == null ||
            !localPlayerAppearanceCatalog.TryGetAvatar(desiredAvatarId, out var avatarDefinition) ||
            avatarDefinition == null ||
            avatarDefinition.AvatarSprite == null)
        {
            return false;
        }

        var visualInstance = new GameObject("Visual");
        visualInstance.transform.SetParent(markerRootTransform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.Euler(markerVisualLocalEulerAngles);
        visualInstance.transform.localScale = Vector3.one;

        CreateCircleLayer(visualInstance.transform, "CircleBorder", markerCircleDiameter, markerCircleBorderColor, 48);
        CreateCircleLayer(
            visualInstance.transform,
            "CircleBackground",
            markerCircleDiameter * (1f - markerCircleBorderRatio),
            markerCircleColor,
            49);
        CreateAvatarLayer(visualInstance.transform, avatarDefinition.AvatarSprite);
        return true;
    }

    private static void RemoveMarkerVisual(Transform markerRootTransform)
    {
        var existingVisual = markerRootTransform.Find("Visual");
        if (existingVisual == null)
        {
            return;
        }

        existingVisual.gameObject.SetActive(false);
        Destroy(existingVisual.gameObject);
    }

    private void CreateCircleLayer(Transform parent, string layerName, float diameter, Color color, int sortingOrder)
    {
        var layer = new GameObject(layerName, typeof(SpriteRenderer));
        layer.transform.SetParent(parent, false);
        layer.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter);

        var renderer = layer.GetComponent<SpriteRenderer>();
        renderer.sprite = GetGeneratedCircleSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void CreateAvatarLayer(Transform parent, Sprite avatarSprite)
    {
        var layer = new GameObject("AvatarIcon", typeof(SpriteRenderer));
        layer.transform.SetParent(parent, false);
        layer.transform.localScale = ResolveUniformSpriteScale(avatarSprite, markerCircleDiameter * markerAvatarFillRatio);

        var renderer = layer.GetComponent<SpriteRenderer>();
        renderer.sprite = avatarSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 50;
    }

    private static Vector3 ResolveUniformSpriteScale(Sprite sprite, float targetDiameter)
    {
        if (sprite == null)
        {
            return Vector3.one;
        }

        var spriteSize = sprite.bounds.size;
        var maxDimension = Mathf.Max(spriteSize.x, spriteSize.y);
        if (maxDimension <= 0f)
        {
            return Vector3.one;
        }

        return Vector3.one * (Mathf.Max(0.01f, targetDiameter) / maxDimension);
    }

    private static Sprite GetGeneratedCircleSprite()
    {
        if (generatedCircleSprite != null)
        {
            return generatedCircleSprite;
        }

        var texture = new Texture2D(GeneratedCircleSpriteSize, GeneratedCircleSpriteSize, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var center = (GeneratedCircleSpriteSize - 1) * 0.5f;
        var radius = center;
        for (var y = 0; y < GeneratedCircleSpriteSize; y++)
        {
            for (var x = 0; x < GeneratedCircleSpriteSize; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        generatedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, GeneratedCircleSpriteSize, GeneratedCircleSpriteSize),
            new Vector2(0.5f, 0.5f),
            GeneratedCircleSpriteSize);
        generatedCircleSprite.hideFlags = HideFlags.HideAndDontSave;
        return generatedCircleSprite;
    }

    private sealed class PlayerMarkerView
    {
        public PlayerMarkerView(
            GameObject root,
            ArcGISLocationComponent locationComponent,
            MeshRenderer renderer,
            SpriteRenderer spriteRenderer,
            Transform visualTransform,
            HPTransform hpTransform,
            HPRoot hpRoot,
            string avatarId)
        {
            Root = root;
            LocationComponent = locationComponent;
            Renderer = renderer;
            SpriteRenderer = spriteRenderer;
            VisualTransform = visualTransform;
            HpTransform = hpTransform;
            HpRoot = hpRoot;
            AvatarId = avatarId;
        }

        public GameObject Root { get; }
        public ArcGISLocationComponent LocationComponent { get; }
        public MeshRenderer Renderer { get; }
        public SpriteRenderer SpriteRenderer { get; }
        public Transform VisualTransform { get; }
        public HPTransform HpTransform { get; }
        public HPRoot HpRoot { get; }
        public string AvatarId { get; }
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
        bool isFallbackPosition,
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
        IsFallbackPosition = isFallbackPosition;
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
    public bool IsFallbackPosition { get; }
    public bool HasWorldPosition { get; }
    public Vector3 WorldPosition { get; }
    public Vector3 TransformPosition { get; }
    public bool HasHpTransform { get; }
    public Vector3 HpUniversePosition { get; }
    public string ParentName { get; }
    public bool IsArcGisInitialized { get; }
}
