using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    public sealed class LocalZoomControlsView : MonoBehaviour
    {
        private RectTransform safeArea;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private Transform owner;
        private Canvas controlsCanvas;
        private GraphicRaycaster controlsRaycaster;

        public static LocalZoomControlsView Create(Transform owner, Action zoomIn, Action zoomOut, Action reset)
        {
            var root = new GameObject("LocalZoomControls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LocalZoomControlsView));
            if (owner != null && owner.gameObject.scene.IsValid())
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            }
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var view = root.GetComponent<LocalZoomControlsView>();
            view.owner = owner;
            view.controlsCanvas = canvas;
            view.controlsRaycaster = root.GetComponent<GraphicRaycaster>();
            view.Build(zoomIn, zoomOut, reset);
            return view;
        }

        private void Build(Action zoomIn, Action zoomOut, Action reset)
        {
            var area = new GameObject("SafeArea", typeof(RectTransform));
            area.transform.SetParent(transform, false);
            safeArea = area.GetComponent<RectTransform>();
            ApplySafeArea();
            var bar = new GameObject("ZoomBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(safeArea, false);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -12f);
            rect.sizeDelta = new Vector2(288f, 76f);
            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            AddButton(bar.transform, "−", zoomOut);
            AddButton(bar.transform, "+", zoomIn);
            AddButton(bar.transform, "1:1", reset);
        }

        private static TMP_Text AddButton(Transform parent, string label, Action callback)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.25f, 0.23f, 0.94f);
            buttonObject.GetComponent<Button>().onClick.AddListener(() => callback?.Invoke());
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            Stretch(textObject.GetComponent<RectTransform>());
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 34;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var isVisible = owner.gameObject.activeInHierarchy;
            controlsCanvas.enabled = isVisible;
            controlsRaycaster.enabled = isVisible;
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
    }
}
