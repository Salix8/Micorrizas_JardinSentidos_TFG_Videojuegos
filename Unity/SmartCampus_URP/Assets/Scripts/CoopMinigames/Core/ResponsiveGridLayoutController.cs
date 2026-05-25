using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class ResponsiveGridLayoutController : MonoBehaviour
    {
        [SerializeField] private int minColumns = 1;
        [SerializeField] private int maxColumns = 4;
        [SerializeField] private Vector2 minCellSize = new(120f, 160f);
        [SerializeField] private Vector2 maxCellSize = new(260f, 340f);
        [SerializeField] [Min(0.3f)] private float cardAspectRatio = 0.72f;

        private GridLayoutGroup gridLayoutGroup;
        private RectTransform rectTransform;
        private int lastChildCount = -1;
        private Vector2 lastRectSize;

        private void Awake()
        {
            ResolveReferences();
            RefreshLayout();
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (gridLayoutGroup == null || rectTransform == null)
            {
                return;
            }

            var childCount = rectTransform.childCount;
            if (childCount != lastChildCount || lastRectSize != rectTransform.rect.size)
            {
                RefreshLayout();
            }
        }

        public void Configure(
            int configuredMinColumns,
            int configuredMaxColumns,
            Vector2 configuredMinCellSize,
            Vector2 configuredMaxCellSize,
            float configuredCardAspectRatio)
        {
            minColumns = Mathf.Max(1, configuredMinColumns);
            maxColumns = Mathf.Max(1, configuredMaxColumns);
            minCellSize = configuredMinCellSize;
            maxCellSize = configuredMaxCellSize;
            cardAspectRatio = Mathf.Max(0.3f, configuredCardAspectRatio);
        }

        public void RefreshLayout()
        {
            ResolveReferences();
            if (gridLayoutGroup == null || rectTransform == null)
            {
                return;
            }

            lastChildCount = rectTransform.childCount;
            lastRectSize = rectTransform.rect.size;

            if (lastChildCount <= 0)
            {
                return;
            }

            var availableWidth = Mathf.Max(1f, rectTransform.rect.width - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right);
            var availableHeight = Mathf.Max(1f, rectTransform.rect.height - gridLayoutGroup.padding.top - gridLayoutGroup.padding.bottom);

            var bestColumns = 1;
            var bestCellSize = minCellSize;
            var bestArea = 0f;
            var clampedMinColumns = Mathf.Clamp(minColumns, 1, lastChildCount);
            var maxColumnCount = Mathf.Clamp(maxColumns, clampedMinColumns, lastChildCount);

            for (var columns = clampedMinColumns; columns <= maxColumnCount; columns++)
            {
                var rows = Mathf.CeilToInt(lastChildCount / (float)columns);
                var widthPerCell = (availableWidth - (columns - 1) * gridLayoutGroup.spacing.x) / columns;
                var heightPerCell = (availableHeight - (rows - 1) * gridLayoutGroup.spacing.y) / rows;

                if (widthPerCell <= 0f || heightPerCell <= 0f)
                {
                    continue;
                }

                var cellWidth = widthPerCell;
                var cellHeight = cellWidth / cardAspectRatio;

                if (cellHeight > heightPerCell)
                {
                    cellHeight = heightPerCell;
                    cellWidth = cellHeight * cardAspectRatio;
                }

                var allowsMinWidth = widthPerCell >= minCellSize.x;
                var allowsMinHeight = heightPerCell >= minCellSize.y;
                var clampedMaxWidth = Mathf.Min(maxCellSize.x, widthPerCell);
                var clampedMaxHeight = Mathf.Min(maxCellSize.y, heightPerCell);

                cellWidth = allowsMinWidth
                    ? Mathf.Clamp(cellWidth, minCellSize.x, clampedMaxWidth)
                    : Mathf.Clamp(cellWidth, 1f, clampedMaxWidth);

                cellHeight = allowsMinHeight
                    ? Mathf.Clamp(cellHeight, minCellSize.y, clampedMaxHeight)
                    : Mathf.Clamp(cellHeight, 1f, clampedMaxHeight);

                var area = cellWidth * cellHeight;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestColumns = columns;
                    bestCellSize = new Vector2(cellWidth, cellHeight);
                }
            }

            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = bestColumns;
            gridLayoutGroup.cellSize = bestCellSize;
        }

        private void ResolveReferences()
        {
            gridLayoutGroup ??= GetComponent<GridLayoutGroup>();
            rectTransform ??= GetComponent<RectTransform>();
            minColumns = Mathf.Max(1, minColumns);
            maxColumns = Mathf.Max(minColumns, maxColumns);
        }
    }
}
