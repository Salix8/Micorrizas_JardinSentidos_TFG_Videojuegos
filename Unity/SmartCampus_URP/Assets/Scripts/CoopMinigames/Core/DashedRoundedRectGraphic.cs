using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class DashedRoundedRectGraphic : MaskableGraphic
    {
        [SerializeField] [Min(0f)] private float cornerRadius = 30f;
        [SerializeField] [Min(1f)] private float lineWidth = 3f;
        [SerializeField] [Min(2f)] private float dashLength = 18f;
        [SerializeField] [Min(1f)] private float gapLength = 14f;
        [SerializeField] [Range(4, 24)] private int cornerSegments = 10;

        public void Configure(Color lineColor, float radius, float width, float dash, float gap)
        {
            color = lineColor;
            cornerRadius = Mathf.Max(0f, radius);
            lineWidth = Mathf.Max(1f, width);
            dashLength = Mathf.Max(2f, dash);
            gapLength = Mathf.Max(1f, gap);
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

            rect.xMin += lineWidth * 0.5f;
            rect.xMax -= lineWidth * 0.5f;
            rect.yMin += lineWidth * 0.5f;
            rect.yMax -= lineWidth * 0.5f;

            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            var points = BuildPath(rect, radius, Mathf.Max(4, cornerSegments));
            AddDashedPath(vh, points, color, lineWidth, dashLength, gapLength);
        }

        private static void AddDashedPath(VertexHelper vh, IReadOnlyList<Vector2> points, Color32 lineColor, float width, float dash, float gap)
        {
            var drawRemaining = dash;
            var skipRemaining = 0f;

            for (var index = 0; index < points.Count; index++)
            {
                var start = points[index];
                var end = points[index + 1 >= points.Count ? 0 : index + 1];
                var segment = end - start;
                var length = segment.magnitude;
                if (length <= Mathf.Epsilon)
                {
                    continue;
                }

                var direction = segment / length;
                var consumed = 0f;
                while (consumed < length)
                {
                    if (skipRemaining > 0f)
                    {
                        var skipped = Mathf.Min(skipRemaining, length - consumed);
                        consumed += skipped;
                        skipRemaining -= skipped;
                        if (skipRemaining <= 0f)
                        {
                            drawRemaining = dash;
                        }

                        continue;
                    }

                    var drawn = Mathf.Min(drawRemaining, length - consumed);
                    AddLineQuad(vh, start + direction * consumed, start + direction * (consumed + drawn), width, lineColor);
                    consumed += drawn;
                    drawRemaining -= drawn;
                    if (drawRemaining <= 0f)
                    {
                        skipRemaining = gap;
                    }
                }
            }
        }

        private static void AddLineQuad(VertexHelper vh, Vector2 from, Vector2 to, float width, Color32 lineColor)
        {
            var direction = (to - from).normalized;
            var normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            var start = vh.currentVertCount;
            vh.AddVert(from - normal, lineColor, Vector2.zero);
            vh.AddVert(from + normal, lineColor, Vector2.zero);
            vh.AddVert(to + normal, lineColor, Vector2.zero);
            vh.AddVert(to - normal, lineColor, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static List<Vector2> BuildPath(Rect rect, float radius, int segments)
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
