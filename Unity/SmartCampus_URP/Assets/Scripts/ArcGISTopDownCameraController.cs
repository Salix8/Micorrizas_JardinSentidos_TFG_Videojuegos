using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(ArcGISLocationComponent))]
public sealed class ArcGISTopDownCameraController : MonoBehaviour
{
    private const double EarthRadiusMeters = 6378137.0;
    private const string RuntimeCanvasName = "MapCameraControlsCanvas";
    private const string RecenterButtonName = "RecenterLocationButton";

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ArcGISLocationComponent cameraLocation;
    [SerializeField] private ArcGISMobileGpsBlueDot gpsTracker;
    [SerializeField] private Button recenterButton;

    [Header("Top Down Camera")]
    [SerializeField] private double fallbackLatitude = 39.9936;
    [SerializeField] private double fallbackLongitude = -0.0665;
    [SerializeField] private double cameraAltitudeMeters = 850.0;
    [SerializeField] private double cameraHeadingDegrees = 0.0;
    [SerializeField] private double cameraPitchDegrees = 0.0;
    [SerializeField] private bool forceUnityTopDownRotation = true;
    [SerializeField] private float unityTopDownYawDegrees = 0f;
    [SerializeField] private float fieldOfView = 45f;
    [SerializeField] private float farClipPlane = 5000f;

    [Header("Pan Bounds")]
    [SerializeField] private double boundsCenterLatitude = 39.9936;
    [SerializeField] private double boundsCenterLongitude = -0.0665;
    [SerializeField] private Vector2 boundsHalfSizeMeters = new(280f, 280f);
    [SerializeField] private float panSensitivity = 1f;
    [SerializeField] private bool followGpsUntilManualPan = true;
    [SerializeField] private bool createRecenterButtonOnStart = true;

    private readonly List<RaycastResult> uiRaycastResults = new();
    private ArcGISSpatialReference wgs84;
    private ArcGISPoint lastGpsPosition;
    private bool hasGpsPosition;
    private bool hasCenteredOnFirstGps;
    private bool hasUserPanned;
    private bool isDragging;
    private int activePointerId;
    private Vector2 lastPointerPosition;
    private double currentLatitude;
    private double currentLongitude;

    private void Awake()
    {
        ResolveReferences();
        wgs84 = ArcGISSpatialReference.WGS84();
        ConfigureCameraProjection();
        SetCameraPosition(fallbackLatitude, fallbackLongitude);
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
        ResolveReferences();
        AttachGpsTracker();
        AttachRecenterButton();
    }

    private void OnDisable()
    {
        if (gpsTracker != null)
        {
            gpsTracker.LocationUpdated -= HandleGpsLocationUpdated;
        }

        if (recenterButton != null)
        {
            recenterButton.onClick.RemoveListener(CenterOnCurrentGps);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        ConfigureCameraProjection();
        ApplyUnityTopDownRotation();
        HandlePanInput();
    }

    public void CenterOnCurrentGps()
    {
        hasUserPanned = false;

        if (gpsTracker != null && gpsTracker.HasLocationFix)
        {
            CenterOnGpsPosition(gpsTracker.CurrentGeographicPosition);
            hasCenteredOnFirstGps = true;
            return;
        }

        if (hasGpsPosition)
        {
            CenterOnGpsPosition(lastGpsPosition);
            hasCenteredOnFirstGps = true;
            return;
        }

        SetCameraPosition(fallbackLatitude, fallbackLongitude);
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (cameraLocation == null)
        {
            cameraLocation = GetComponent<ArcGISLocationComponent>();
        }

        if (gpsTracker == null)
        {
            gpsTracker = FindFirstObjectByType<ArcGISMobileGpsBlueDot>(FindObjectsInactive.Include);
        }

        if (recenterButton == null && createRecenterButtonOnStart && Application.isPlaying)
        {
            recenterButton = FindOrCreateRecenterButton();
        }
    }

    private void AttachGpsTracker()
    {
        if (gpsTracker == null)
        {
            return;
        }

        gpsTracker.LocationUpdated -= HandleGpsLocationUpdated;
        gpsTracker.LocationUpdated += HandleGpsLocationUpdated;

        if (gpsTracker.HasLocationFix)
        {
            CenterOnGpsPosition(gpsTracker.CurrentGeographicPosition);
            hasCenteredOnFirstGps = true;
        }
    }

    private void AttachRecenterButton()
    {
        if (recenterButton == null)
        {
            return;
        }

        recenterButton.onClick.RemoveListener(CenterOnCurrentGps);
        recenterButton.onClick.AddListener(CenterOnCurrentGps);
    }

    private void HandleGpsLocationUpdated(ArcGISPoint geographicPosition, Vector3 _, LocationInfo __)
    {
        hasGpsPosition = true;
        lastGpsPosition = geographicPosition;

        if (!hasCenteredOnFirstGps || (followGpsUntilManualPan && !hasUserPanned))
        {
            CenterOnGpsPosition(geographicPosition);
            hasCenteredOnFirstGps = true;
        }
    }

    private void CenterOnGpsPosition(ArcGISPoint geographicPosition)
    {
        if (geographicPosition == null)
        {
            return;
        }

        SetCameraPosition(geographicPosition.Y, geographicPosition.X);
    }

    private void HandlePanInput()
    {
        if (!TryGetPointerSample(out var pointerSample))
        {
            isDragging = false;
            return;
        }

        if (pointerSample.Phase == PointerSamplePhase.Began)
        {
            if (IsPointerOverBlockingUi(pointerSample.ScreenPosition))
            {
                isDragging = false;
                return;
            }

            isDragging = true;
            activePointerId = pointerSample.PointerId;
            lastPointerPosition = pointerSample.ScreenPosition;
            return;
        }

        if (!isDragging || pointerSample.PointerId != activePointerId)
        {
            return;
        }

        if (pointerSample.Phase == PointerSamplePhase.Ended)
        {
            isDragging = false;
            return;
        }

        var delta = pointerSample.ScreenPosition - lastPointerPosition;
        lastPointerPosition = pointerSample.ScreenPosition;

        if (delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        PanByScreenDelta(delta);
    }

    private void PanByScreenDelta(Vector2 screenDelta)
    {
        var metersPerPixel = CalculateMetersPerPixel();
        var eastMeters = -screenDelta.x * metersPerPixel;
        var northMeters = -screenDelta.y * metersPerPixel;

        var currentOffset = GeographicToMetersOffset(currentLatitude, currentLongitude);
        currentOffset += new Vector2((float)eastMeters, (float)northMeters);
        currentOffset.x = Mathf.Clamp(currentOffset.x, -boundsHalfSizeMeters.x, boundsHalfSizeMeters.x);
        currentOffset.y = Mathf.Clamp(currentOffset.y, -boundsHalfSizeMeters.y, boundsHalfSizeMeters.y);

        var clampedPosition = MetersOffsetToGeographic(currentOffset);
        hasUserPanned = true;
        SetCameraPosition(clampedPosition.latitude, clampedPosition.longitude);
    }

    private float CalculateMetersPerPixel()
    {
        var safeScreenHeight = Mathf.Max(1, Screen.height);
        var visibleMeters = 2f * (float)cameraAltitudeMeters * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        return visibleMeters / safeScreenHeight * Mathf.Max(0.01f, panSensitivity);
    }

    private void SetCameraPosition(double latitude, double longitude)
    {
        var clampedOffset = GeographicToMetersOffset(latitude, longitude);
        clampedOffset.x = Mathf.Clamp(clampedOffset.x, -boundsHalfSizeMeters.x, boundsHalfSizeMeters.x);
        clampedOffset.y = Mathf.Clamp(clampedOffset.y, -boundsHalfSizeMeters.y, boundsHalfSizeMeters.y);

        var clampedPosition = MetersOffsetToGeographic(clampedOffset);
        currentLatitude = clampedPosition.latitude;
        currentLongitude = clampedPosition.longitude;

        if (cameraLocation == null)
        {
            return;
        }

        cameraLocation.Position = new ArcGISPoint(currentLongitude, currentLatitude, cameraAltitudeMeters, wgs84);
        cameraLocation.Rotation = new ArcGISRotation(cameraHeadingDegrees, cameraPitchDegrees, 0.0);
        ApplyUnityTopDownRotation();
    }

    private void ConfigureCameraProjection()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = false;
        targetCamera.fieldOfView = fieldOfView;
        targetCamera.farClipPlane = Mathf.Max(farClipPlane, (float)cameraAltitudeMeters + 500f);
    }

    private void ApplyUnityTopDownRotation()
    {
        if (!forceUnityTopDownRotation)
        {
            return;
        }

        transform.localRotation = Quaternion.Euler(90f, unityTopDownYawDegrees, 0f);
    }

    private Vector2 GeographicToMetersOffset(double latitude, double longitude)
    {
        var originLatitudeRadians = boundsCenterLatitude * Mathf.Deg2Rad;
        var east = (longitude - boundsCenterLongitude) * Mathf.Deg2Rad * EarthRadiusMeters * Math.Cos(originLatitudeRadians);
        var north = (latitude - boundsCenterLatitude) * Mathf.Deg2Rad * EarthRadiusMeters;
        return new Vector2((float)east, (float)north);
    }

    private (double latitude, double longitude) MetersOffsetToGeographic(Vector2 offsetMeters)
    {
        var originLatitudeRadians = boundsCenterLatitude * Mathf.Deg2Rad;
        var latitude = boundsCenterLatitude + offsetMeters.y / EarthRadiusMeters / Mathf.Deg2Rad;
        var longitude = boundsCenterLongitude + offsetMeters.x / (EarthRadiusMeters * Math.Cos(originLatitudeRadians)) / Mathf.Deg2Rad;
        return (latitude, longitude);
    }

    private bool IsPointerOverBlockingUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);

        for (var index = 0; index < uiRaycastResults.Count; index++)
        {
            if (uiRaycastResults[index].gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private Button FindOrCreateRecenterButton()
    {
        var existingButton = GameObject.Find(RecenterButtonName);
        if (existingButton != null && existingButton.TryGetComponent<Button>(out var existing))
        {
            return existing;
        }

        var canvas = FindOrCreateRuntimeCanvas();
        var buttonObject = new GameObject(RecenterButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        var rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-36f, 36f);
        rectTransform.sizeDelta = new Vector2(112f, 112f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.28f, 0.55f, 0.92f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.GetComponent<Text>();
        label.text = "GPS";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 30;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return button;
    }

    private static Canvas FindOrCreateRuntimeCanvas()
    {
        var existingCanvas = GameObject.Find(RuntimeCanvasName);
        if (existingCanvas != null && existingCanvas.TryGetComponent<Canvas>(out var canvas))
        {
            return canvas;
        }

        var canvasObject = new GameObject(RuntimeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private bool TryGetPointerSample(out PointerSample sample)
    {
#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemPointerSample(out sample))
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (TryGetLegacyPointerSample(out sample))
        {
            return true;
        }
#endif
        sample = default;
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetInputSystemPointerSample(out PointerSample sample)
    {
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];
            sample = new PointerSample(touch.touchId, touch.screenPosition, ToPointerPhase(touch.phase));
            return sample.Phase != PointerSamplePhase.None;
        }

        if (Mouse.current != null)
        {
            var mousePosition = Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                sample = new PointerSample(-1, mousePosition, PointerSamplePhase.Began);
                return true;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                sample = new PointerSample(-1, mousePosition, PointerSamplePhase.Ended);
                return true;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                sample = new PointerSample(-1, mousePosition, PointerSamplePhase.Moved);
                return true;
            }
        }

        sample = default;
        return false;
    }

    private static PointerSamplePhase ToPointerPhase(UnityEngine.InputSystem.TouchPhase phase)
    {
        return phase switch
        {
            UnityEngine.InputSystem.TouchPhase.Began => PointerSamplePhase.Began,
            UnityEngine.InputSystem.TouchPhase.Moved => PointerSamplePhase.Moved,
            UnityEngine.InputSystem.TouchPhase.Stationary => PointerSamplePhase.Moved,
            UnityEngine.InputSystem.TouchPhase.Ended => PointerSamplePhase.Ended,
            UnityEngine.InputSystem.TouchPhase.Canceled => PointerSamplePhase.Ended,
            _ => PointerSamplePhase.None
        };
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    private static bool TryGetLegacyPointerSample(out PointerSample sample)
    {
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            sample = new PointerSample(touch.fingerId, touch.position, ToPointerPhase(touch.phase));
            return sample.Phase != PointerSamplePhase.None;
        }

        if (Input.GetMouseButtonDown(0))
        {
            sample = new PointerSample(-1, Input.mousePosition, PointerSamplePhase.Began);
            return true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            sample = new PointerSample(-1, Input.mousePosition, PointerSamplePhase.Ended);
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            sample = new PointerSample(-1, Input.mousePosition, PointerSamplePhase.Moved);
            return true;
        }

        sample = default;
        return false;
    }

    private static PointerSamplePhase ToPointerPhase(TouchPhase phase)
    {
        return phase switch
        {
            TouchPhase.Began => PointerSamplePhase.Began,
            TouchPhase.Moved => PointerSamplePhase.Moved,
            TouchPhase.Stationary => PointerSamplePhase.Moved,
            TouchPhase.Ended => PointerSamplePhase.Ended,
            TouchPhase.Canceled => PointerSamplePhase.Ended,
            _ => PointerSamplePhase.None
        };
    }
#endif

    private readonly struct PointerSample
    {
        public PointerSample(int pointerId, Vector2 screenPosition, PointerSamplePhase phase)
        {
            PointerId = pointerId;
            ScreenPosition = screenPosition;
            Phase = phase;
        }

        public int PointerId { get; }
        public Vector2 ScreenPosition { get; }
        public PointerSamplePhase Phase { get; }
    }

    private enum PointerSamplePhase
    {
        None,
        Began,
        Moved,
        Ended
    }
}
