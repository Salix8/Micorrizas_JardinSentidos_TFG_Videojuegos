using System;
using System.Collections;
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
    [SerializeField] private float minMarkerScale = 4f;
    [SerializeField] private float maxMarkerScale = 30f;
    [SerializeField] private float distanceScaleFactor = 0.02f;

#if UNITY_EDITOR
    [Header("Editor Simulation")]
    [SerializeField] private bool simulateLocationInEditor = false;
    [SerializeField] private double simulatedLatitude = 39.9936;
    [SerializeField] private double simulatedLongitude = -0.0665;
    [SerializeField] private double simulatedAltitude = 0.0;
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
#if UNITY_EDITOR
    private bool loggedEditorSimulation;
#endif

    public bool HasLocationFix => hasLocationFix;
    public ArcGISPoint CurrentGeographicPosition => currentGeographicPosition;
    public Vector3 CurrentEnginePosition => currentEnginePosition;
    public LocationInfo LastLocationInfo => lastLocationInfo;

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
        if (!startOnEnable || !Application.isPlaying)
        {
            return;
        }

        StartTracking();
    }

    private void OnDisable()
    {
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
    }

    private void Update()
    {
        if (!Application.isPlaying || mapComponent == null)
        {
            return;
        }

        ResolveCameraIfNeeded();

#if UNITY_EDITOR
        if (ShouldUseEditorSimulation())
        {
            if (!loggedEditorSimulation)
            {
                Debug.Log("ArcGISMobileGpsBlueDot is using the simulated editor location. Disable 'Simulate Location In Editor' on the component to use live device GPS in a build.", this);
                loggedEditorSimulation = true;
            }

            ApplyGeographicPosition(
                simulatedLatitude,
                simulatedLongitude,
                simulatedAltitude,
                new LocationInfo());

            return;
        }
#endif

        PollDeviceLocation();
        RefreshCurrentEnginePosition();
        UpdateMarkerScale();
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

        ConfigureMarkerMaterial();
    }

    private void ConfigureMarkerMaterial()
    {
        if (markerRenderer == null)
        {
            return;
        }

        var shader = Shader.Find("HDRP/Unlit") ??
                     Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Standard") ??
                     Shader.Find("Unlit/Color");

        if (shader == null)
        {
            return;
        }

        var material = new Material(shader)
        {
            name = "GPS Blue Dot Material"
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", markerColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", markerColor);
        }

        if (material.HasProperty("_EmissiveColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissiveColor", markerColor * 2f);
        }

        markerRenderer.sharedMaterial = material;
        markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;
        markerRenderer.lightProbeUsage = LightProbeUsage.Off;
        markerRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
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

#if UNITY_EDITOR
    private bool ShouldUseEditorSimulation()
    {
        return simulateLocationInEditor && !Application.isMobilePlatform;
    }
#endif
}
