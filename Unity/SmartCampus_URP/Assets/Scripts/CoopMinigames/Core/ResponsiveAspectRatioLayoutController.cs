using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsiveAspectRatioLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private RectTransform referenceRectTransform;
        [SerializeField] [Min(0.2f)] private float widthToHeightRatio = 0.7f;
        [SerializeField] private Vector2 minSize = new(220f, 320f);
        [SerializeField] private Vector2 maxSize = new(960f, 1400f);
        [SerializeField] private Vector2 outerMargin = new(24f, 24f);

        private Vector2 lastReferenceSize;

        public RectTransform ReferenceRectTransform => referenceRectTransform;

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
            float configuredWidthToHeightRatio,
            Vector2 configuredMinSize,
            Vector2 configuredMaxSize,
            Vector2 configuredOuterMargin)
        {
            referenceRectTransform = configuredReferenceRectTransform;
            widthToHeightRatio = Mathf.Max(0.2f, configuredWidthToHeightRatio);
            minSize = configuredMinSize;
            maxSize = configuredMaxSize;
            outerMargin = configuredOuterMargin;
            RefreshLayout();
        }

        public void ConfigureSizing(
            float configuredWidthToHeightRatio,
            Vector2 configuredMinSize,
            Vector2 configuredMaxSize,
            Vector2 configuredOuterMargin)
        {
            Configure(
                referenceRectTransform,
                configuredWidthToHeightRatio,
                configuredMinSize,
                configuredMaxSize,
                configuredOuterMargin);
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

            var availableWidth = Mathf.Max(0f, referenceSize.x - outerMargin.x * 2f);
            var availableHeight = Mathf.Max(0f, referenceSize.y - outerMargin.y * 2f);
            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            var aspectConstrainedWidth = Mathf.Min(availableWidth, availableHeight * widthToHeightRatio);
            var aspectConstrainedHeight = aspectConstrainedWidth / widthToHeightRatio;

            if (aspectConstrainedHeight > availableHeight)
            {
                aspectConstrainedHeight = availableHeight;
                aspectConstrainedWidth = aspectConstrainedHeight * widthToHeightRatio;
            }

            var finalWidth = Mathf.Clamp(aspectConstrainedWidth, minSize.x, maxSize.x);
            var finalHeight = Mathf.Clamp(aspectConstrainedHeight, minSize.y, maxSize.y);

            finalWidth = Mathf.Min(finalWidth, availableWidth);
            finalHeight = Mathf.Min(finalHeight, availableHeight);

            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        }

        private Vector2 GetReferenceSize()
        {
            if (referenceRectTransform != null)
            {
                if (TryGetValidSize(referenceRectTransform, out var referenceSize))
                {
                    return referenceSize;
                }
            }

            if (targetRectTransform != null && targetRectTransform.parent is RectTransform parentRectTransform)
            {
                if (TryGetValidSize(parentRectTransform, out var parentSize))
                {
                    return parentSize;
                }
            }

            var canvas = targetRectTransform == null ? null : targetRectTransform.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.transform is RectTransform canvasRectTransform)
            {
                var canvasSize = canvasRectTransform.rect.size;
                if (canvasSize.x > 0f && canvasSize.y > 0f)
                {
                    return canvasSize;
                }
            }

            return new Vector2(Screen.width, Screen.height);
        }

        private static bool TryGetValidSize(RectTransform rectTransform, out Vector2 size)
        {
            var current = rectTransform;
            while (current != null)
            {
                size = current.rect.size;
                if (size.x > 0f && size.y > 0f)
                {
                    return true;
                }

                current = current.parent as RectTransform;
            }

            size = Vector2.zero;
            return false;
        }

        private void ResolveReferences()
        {
            targetRectTransform ??= GetComponent<RectTransform>();
            if (referenceRectTransform == null && targetRectTransform != null && targetRectTransform.parent is RectTransform parentRectTransform)
            {
                referenceRectTransform = parentRectTransform;
            }

            widthToHeightRatio = Mathf.Max(0.2f, widthToHeightRatio);
            minSize.x = Mathf.Max(120f, minSize.x);
            minSize.y = Mathf.Max(160f, minSize.y);
            maxSize.x = Mathf.Max(minSize.x, maxSize.x);
            maxSize.y = Mathf.Max(minSize.y, maxSize.y);
            outerMargin.x = Mathf.Max(0f, outerMargin.x);
            outerMargin.y = Mathf.Max(0f, outerMargin.y);
        }
    }
}
