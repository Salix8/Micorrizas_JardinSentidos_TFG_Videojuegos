using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsivePanelLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private RectTransform referenceRectTransform;
        [SerializeField] [Range(0.1f, 1f)] private float widthRatio = 0.9f;
        [SerializeField] [Range(0.1f, 1f)] private float heightRatio = 0.9f;
        [SerializeField] private Vector2 minSize = new(280f, 200f);
        [SerializeField] private Vector2 maxSize = new(960f, 1400f);
        [SerializeField] private Vector2 outerMargin = new(24f, 24f);

        private Vector2 lastReferenceSize;

        private void Awake()
        {
            ResolveReferences();
            RefreshLayout();
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (targetRectTransform == null)
            {
                return;
            }

            var referenceSize = GetReferenceSize();
            if (referenceSize != lastReferenceSize)
            {
                RefreshLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }

        public void Configure(
            RectTransform configuredReferenceRectTransform,
            float configuredWidthRatio,
            float configuredHeightRatio,
            Vector2 configuredMinSize,
            Vector2 configuredMaxSize,
            Vector2 configuredOuterMargin)
        {
            referenceRectTransform = configuredReferenceRectTransform;
            widthRatio = Mathf.Clamp01(configuredWidthRatio);
            heightRatio = Mathf.Clamp01(configuredHeightRatio);
            minSize = configuredMinSize;
            maxSize = configuredMaxSize;
            outerMargin = configuredOuterMargin;
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            ResolveReferences();

            if (targetRectTransform == null)
            {
                return;
            }

            var referenceSize = GetReferenceSize();
            lastReferenceSize = referenceSize;

            var availableWidth = Mathf.Max(0f, referenceSize.x - (outerMargin.x * 2f));
            var availableHeight = Mathf.Max(0f, referenceSize.y - (outerMargin.y * 2f));
            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            var preferredWidth = Mathf.Clamp(availableWidth * widthRatio, minSize.x, maxSize.x);
            var preferredHeight = Mathf.Clamp(availableHeight * heightRatio, minSize.y, maxSize.y);

            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(availableWidth, preferredWidth));
            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(availableHeight, preferredHeight));
        }

        private Vector2 GetReferenceSize()
        {
            if (referenceRectTransform != null)
            {
                return referenceRectTransform.rect.size;
            }

            if (targetRectTransform != null && targetRectTransform.parent is RectTransform parentRectTransform)
            {
                return parentRectTransform.rect.size;
            }

            return new Vector2(Screen.width, Screen.height);
        }

        private void ResolveReferences()
        {
            targetRectTransform ??= GetComponent<RectTransform>();
            if (referenceRectTransform == null && targetRectTransform != null && targetRectTransform.parent is RectTransform parentRectTransform)
            {
                referenceRectTransform = parentRectTransform;
            }

            widthRatio = Mathf.Clamp(widthRatio, 0.1f, 1f);
            heightRatio = Mathf.Clamp(heightRatio, 0.1f, 1f);
            minSize.x = Mathf.Max(120f, minSize.x);
            minSize.y = Mathf.Max(120f, minSize.y);
            maxSize.x = Mathf.Max(minSize.x, maxSize.x);
            maxSize.y = Mathf.Max(minSize.y, maxSize.y);
            outerMargin.x = Mathf.Max(0f, outerMargin.x);
            outerMargin.y = Mathf.Max(0f, outerMargin.y);
        }
    }
}
