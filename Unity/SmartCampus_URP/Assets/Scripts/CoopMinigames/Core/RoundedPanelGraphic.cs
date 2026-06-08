using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class RoundedPanelGraphic : MaskableGraphic
    {
        [SerializeField] [Min(0f)] private float cornerRadius = 28f;
        [SerializeField] [Min(0f)] private float borderWidth = 2f;
        [SerializeField] private Color fillColor = new(1f, 1f, 1f, 0.92f);
        [SerializeField] private Color borderColor = new(0.84f, 0.79f, 0.63f, 1f);
        [SerializeField] [Range(4, 24)] private int cornerSegments = 10;

        public void Configure(Color fill, Color border, float radius, float width)
        {
            fillColor = fill;
            borderColor = border;
            cornerRadius = Mathf.Max(0f, radius);
            borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            var outer = BuildRoundedRect(rect, radius, Mathf.Max(4, cornerSegments));
            AddFilledPolygon(vh, outer, fillColor);

            if (borderWidth <= 0f)
            {
                return;
            }

            var inset = Mathf.Min(borderWidth, rect.width * 0.5f, rect.height * 0.5f);
            var innerRect = new Rect(rect.xMin + inset, rect.yMin + inset, rect.width - inset * 2f, rect.height - inset * 2f);
            var inner = BuildRoundedRect(innerRect, Mathf.Max(0f, radius - inset), Mathf.Max(4, cornerSegments));
            AddRing(vh, outer, inner, borderColor);
        }

        private static void AddFilledPolygon(VertexHelper vh, IReadOnlyList<Vector2> points, Color32 fill)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            var center = Vector2.zero;
            for (var index = 0; index < points.Count; index++)
            {
                center += points[index];
            }

            center /= points.Count;
            var centerIndex = vh.currentVertCount;
            vh.AddVert(center, fill, Vector2.zero);
            for (var index = 0; index < points.Count; index++)
            {
                vh.AddVert(points[index], fill, Vector2.zero);
            }

            for (var index = 0; index < points.Count; index++)
            {
                var next = index + 1 >= points.Count ? 0 : index + 1;
                vh.AddTriangle(centerIndex, centerIndex + 1 + index, centerIndex + 1 + next);
            }
        }

        private static void AddRing(VertexHelper vh, IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, Color32 border)
        {
            if (outer == null || inner == null || outer.Count != inner.Count || outer.Count < 3)
            {
                return;
            }

            for (var index = 0; index < outer.Count; index++)
            {
                var next = index + 1 >= outer.Count ? 0 : index + 1;
                var start = vh.currentVertCount;
                vh.AddVert(outer[index], border, Vector2.zero);
                vh.AddVert(outer[next], border, Vector2.zero);
                vh.AddVert(inner[next], border, Vector2.zero);
                vh.AddVert(inner[index], border, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static List<Vector2> BuildRoundedRect(Rect rect, float radius, int segments)
        {
            var points = new List<Vector2>(segments * 4 + 4);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);
            return points;
        }

        private static void AddCorner(List<Vector2> points, Vector2 center, float radius, float fromDegrees, float toDegrees, int segments)
        {
            for (var index = 0; index <= segments; index++)
            {
                var angle = Mathf.Lerp(fromDegrees, toDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }
}
