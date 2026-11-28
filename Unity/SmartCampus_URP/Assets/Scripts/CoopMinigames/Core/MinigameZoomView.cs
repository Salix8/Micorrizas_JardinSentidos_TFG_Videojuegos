using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace SmartCampus.Coop.Minigames
{
    /// <summary>
    /// Zoom local (no sincronizado) del area de juego de un minijuego, sin modos:
    /// un dedo juega, dos dedos pellizcan para ampliar y arrastran para mover la vista,
    /// y un doble toque con dos dedos vuelve a 1x. Se coloca sobre el panel interactivo:
    /// sus hijos pasan a un contenedor escalable y la barra superior, el tutorial y los
    /// resultados quedan fuera del zoom.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinigameZoomView : MonoBehaviour
    {
        private const float TapMaxDurationSeconds = 0.3f;
        private const float TapMaxMovementPixels = 30f;
        private const float DoubleTapMaxIntervalSeconds = 0.45f;
        private const float EditorWheelZoomFactor = 1.1f;

        private float maximumZoom = 2f;
        private float zoomStep = 0.25f;
        private float pinchSensitivity = 1f;
        private bool showZoomButtons;

        private RectTransform viewport;
        private RectTransform content;
        private RectMask2D viewportMask;
        private CanvasGroup contentGroup;
        private Canvas rootCanvas;
        private LocalZoomControlsView controls;
        private float zoom = 1f;
        private bool isInitialized;

        private bool trackingTwoFingers;
        private bool pinchAllowed;
        private int firstTouchId;
        private int secondTouchId;
        private float previousDistance;
        private Vector2 previousMidpoint;
        private Vector2 startMidpoint;
        private float startDistance;
        private float twoFingerStartTime;
        private float maxTwoFingerMovement;
        private float lastTwoFingerTapTime = -10f;
        private Vector2 previousMousePosition;

        public float CurrentZoom => zoom;
        public RectTransform Content => content;

        public void Configure(float max, float step, float sensitivity, bool showButtons)
        {
            maximumZoom = Mathf.Max(1f, max);
            zoomStep = Mathf.Max(0.01f, step);
            pinchSensitivity = Mathf.Max(0.01f, sensitivity);
            showZoomButtons = showButtons;
        }

        private IEnumerator Start()
        {
            // Espera un frame para que los controladores de la escena resuelvan sus referencias antes de envolver la jerarquia.
            yield return null;
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            viewport = transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            isInitialized = true;
            var parentCanvas = GetComponentInParent<Canvas>();
            rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;

            var originalChildren = new Transform[transform.childCount];
            for (var i = 0; i < originalChildren.Length; i++)
            {
                originalChildren[i] = transform.GetChild(i);
            }

            var contentObject = new GameObject("ZoomContent", typeof(RectTransform), typeof(CanvasGroup));
            contentObject.transform.SetParent(transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            contentGroup = contentObject.GetComponent<CanvasGroup>();

            // El layout del panel pasa al contenedor, y el panel sigue informando a su padre del tamano preferido.
            var originalLayout = GetComponent<LayoutGroup>();
            if (originalLayout != null)
            {
                var copiedLayout = (LayoutGroup)contentObject.AddComponent(originalLayout.GetType());
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(originalLayout), copiedLayout);
                originalLayout.enabled = false;
                var proxy = gameObject.AddComponent<MinigameZoomLayoutProxy>();
                proxy.Initialize(content);
            }

            foreach (var child in originalChildren)
            {
                child.SetParent(content, false);
            }

            viewportMask = GetComponent<RectMask2D>() ?? gameObject.AddComponent<RectMask2D>();
            viewportMask.enabled = false;

            if (showZoomButtons)
            {
                controls = LocalZoomControlsView.Create(transform, () => SetZoom(zoom + zoomStep), () => SetZoom(zoom - zoomStep), ResetView);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Disable();
#endif
            StopTracking();
            MinigameZoomGesture.End();
            SetContentInteractable(true);
        }

        private void OnDestroy()
        {
            if (controls != null)
            {
                Destroy(controls.gameObject);
            }
        }

        private void Update()
        {
            if (!isInitialized || content == null)
            {
                return;
            }

            UpdateTouchGesture();
#if UNITY_EDITOR || UNITY_STANDALONE
            UpdateMouseFallback();
#endif
            SetContentInteractable(!MinigameZoomGesture.BlocksInteraction);
        }

        private void UpdateTouchGesture()
        {
#if ENABLE_INPUT_SYSTEM
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            var count = touches.Count;
            if (count >= 2)
            {
                var first = touches[0];
                var second = touches[1];
                var firstPosition = first.screenPosition;
                var secondPosition = second.screenPosition;
                var distance = Vector2.Distance(firstPosition, secondPosition);
                var midpoint = (firstPosition + secondPosition) * 0.5f;

                if (!trackingTwoFingers || first.touchId != firstTouchId || second.touchId != secondTouchId)
                {
                    var wasTracking = trackingTwoFingers;
                    trackingTwoFingers = true;
                    firstTouchId = first.touchId;
                    secondTouchId = second.touchId;
                    startDistance = distance;
                    startMidpoint = midpoint;
                    twoFingerStartTime = Time.unscaledTime;
                    maxTwoFingerMovement = wasTracking ? float.MaxValue : 0f;
                    pinchAllowed = wasTracking
                        ? pinchAllowed
                        : IsInsideViewport(firstPosition) && IsInsideViewport(secondPosition);
                    if (pinchAllowed)
                    {
                        MinigameZoomGesture.Begin();
                    }
                }
                else if (pinchAllowed)
                {
                    maxTwoFingerMovement = Mathf.Max(
                        maxTwoFingerMovement,
                        Mathf.Abs(distance - startDistance),
                        Vector2.Distance(midpoint, startMidpoint));

                    if (previousDistance > 1f && distance > 1f)
                    {
                        var targetZoom = zoom * Mathf.Pow(distance / previousDistance, pinchSensitivity);
                        ApplyZoom(targetZoom, previousMidpoint, midpoint);
                    }
                }

                previousDistance = distance;
                previousMidpoint = midpoint;
                return;
            }

            if (trackingTwoFingers)
            {
                if (pinchAllowed &&
                    Time.unscaledTime - twoFingerStartTime <= TapMaxDurationSeconds &&
                    maxTwoFingerMovement <= TapMaxMovementPixels)
                {
                    RegisterTwoFingerTap();
                }

                StopTracking();
            }

            if (count == 0)
            {
                MinigameZoomGesture.End();
            }
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // Para probar en el editor: Ctrl + rueda amplia alrededor del raton y el boton central arrastra la vista.
        private void UpdateMouseFallback()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null)
            {
                return;
            }

            var mousePosition = mouse.position.ReadValue();
            var ctrlPressed = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
            var wheel = mouse.scroll.ReadValue().y;
            if (ctrlPressed && Mathf.Abs(wheel) > 0.01f && IsInsideViewport(mousePosition))
            {
                var factor = wheel > 0f ? EditorWheelZoomFactor : 1f / EditorWheelZoomFactor;
                ApplyZoom(zoom * factor, mousePosition, mousePosition);
            }

            if (mouse.middleButton.isPressed && zoom > 1.001f)
            {
                if (!mouse.middleButton.wasPressedThisFrame)
                {
                    ApplyZoom(zoom, previousMousePosition, mousePosition);
                }
            }

            previousMousePosition = mousePosition;
#endif
        }
#endif

        private void RegisterTwoFingerTap()
        {
            if (Time.unscaledTime - lastTwoFingerTapTime <= DoubleTapMaxIntervalSeconds)
            {
                lastTwoFingerTapTime = -10f;
                ResetView();
                return;
            }

            lastTwoFingerTapTime = Time.unscaledTime;
        }

        private void StopTracking()
        {
            trackingTwoFingers = false;
            pinchAllowed = false;
            previousDistance = 0f;
        }

        public void ResetView()
        {
            if (content == null)
            {
                return;
            }

            zoom = 1f;
            content.localScale = Vector3.one;
            content.anchoredPosition = Vector2.zero;
            UpdateMask();
        }

        private void SetZoom(float value)
        {
            if (viewport == null)
            {
                return;
            }

            var center = viewport.TransformPoint(viewport.rect.center);
            var screenCenter = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), center);
            ApplyZoom(value, screenCenter, screenCenter);
        }

        /// <summary>Aplica el zoom manteniendo bajo <paramref name="currentScreenFocus"/> el punto que estaba bajo <paramref name="previousScreenFocus"/>.</summary>
        public void ApplyZoom(float targetZoom, Vector2 previousScreenFocus, Vector2 currentScreenFocus)
        {
            if (content == null || viewport == null)
            {
                return;
            }

            var newZoom = LocalZoomService.ClampZoom(targetZoom, maximumZoom);
            var eventCamera = GetEventCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, previousScreenFocus, eventCamera, out var previousLocal) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, currentScreenFocus, eventCamera, out var currentLocal))
            {
                return;
            }

            var center = viewport.rect.center;
            var newPosition = LocalZoomService.ZoomAroundFocus(
                content.anchoredPosition,
                zoom,
                newZoom,
                previousLocal - center,
                currentLocal - center);

            zoom = newZoom;
            content.localScale = new Vector3(zoom, zoom, 1f);
            content.anchoredPosition = LocalZoomService.ClampPosition(newPosition, viewport.rect.size, zoom);
            UpdateMask();
        }

        private void UpdateMask()
        {
            if (viewportMask != null)
            {
                // A 1x no se recorta nada para no cortar cartas que salen del panel al deslizarlas.
                viewportMask.enabled = zoom > 1.001f;
            }
        }

        private void SetContentInteractable(bool interactable)
        {
            if (contentGroup != null && contentGroup.interactable != interactable)
            {
                contentGroup.interactable = interactable;
            }
        }

        private bool IsInsideViewport(Vector2 screenPosition)
        {
            return viewport != null &&
                   viewport.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPosition, GetEventCamera());
        }

        private Camera GetEventCamera()
        {
            return rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }
    }

    /// <summary>
    /// Tras mover los hijos al contenedor de zoom, el panel informa a su layout padre del mismo
    /// tamano preferido que daba su layout original, para que la pantalla no cambie a 1x.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinigameZoomLayoutProxy : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        private RectTransform source;

        public void Initialize(RectTransform contentSource)
        {
            source = contentSource;
            SetDirty();
        }

        public float minWidth => source == null ? -1f : LayoutUtility.GetMinWidth(source);
        public float preferredWidth => source == null ? -1f : LayoutUtility.GetPreferredWidth(source);
        public float flexibleWidth => source == null ? -1f : LayoutUtility.GetFlexibleWidth(source);
        public float minHeight => source == null ? -1f : LayoutUtility.GetMinHeight(source);
        public float preferredHeight => source == null ? -1f : LayoutUtility.GetPreferredHeight(source);
        public float flexibleHeight => source == null ? -1f : LayoutUtility.GetFlexibleHeight(source);
        public int layoutPriority => 0;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public void SetLayoutHorizontal()
        {
        }

        public void SetLayoutVertical()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        private void SetDirty()
        {
            if (IsActive())
            {
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
            }
        }
    }
}
