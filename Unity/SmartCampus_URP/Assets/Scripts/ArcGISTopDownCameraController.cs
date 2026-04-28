using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(ArcGISLocationComponent))]
public sealed class ArcGISTopDownCameraController : MonoBehaviour
{
    private const double EarthRadiusMeters = 6378137.0;

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ArcGISLocationComponent cameraLocation;

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

    private readonly List<RaycastResult> uiRaycastResults = new();
    private ArcGISSpatialReference wgs84;
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

    public void CenterOnDefaultPosition()
    {
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
            if (uiRaycastResults[index].gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
            {
                return true;
            }
        }

        return false;
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
