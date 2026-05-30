using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerMarkerAppearanceCatalogConfig",
    menuName = "SmartCampus/Lobby/Player Marker Appearance Catalog")]
public sealed class PlayerMarkerAppearanceCatalogConfig : ScriptableObject
{
    [SerializeField] private List<PlayerMarkerShapeDefinition> shapes = new();
    [SerializeField] private List<PlayerMarkerColorDefinition> colors = new();

    public IReadOnlyList<PlayerMarkerShapeDefinition> Shapes => shapes;
    public IReadOnlyList<PlayerMarkerColorDefinition> Colors => colors;

    public string DefaultShapeId => GetFirstShapeId();
    public string DefaultColorId => GetFirstColorId();

    public bool TryGetShape(string shapeId, out PlayerMarkerShapeDefinition shape)
    {
        var normalizedId = NormalizeId(shapeId);
        for (var index = 0; index < shapes.Count; index++)
        {
            var candidate = shapes[index];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.ShapeId))
            {
                continue;
            }

            if (string.Equals(candidate.ShapeId, normalizedId, StringComparison.OrdinalIgnoreCase))
            {
                shape = candidate;
                return true;
            }
        }

        shape = null;
        return false;
    }

    public bool TryGetColor(string colorId, out PlayerMarkerColorDefinition color)
    {
        var normalizedId = NormalizeId(colorId);
        for (var index = 0; index < colors.Count; index++)
        {
            var candidate = colors[index];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.ColorId))
            {
                continue;
            }

            if (string.Equals(candidate.ColorId, normalizedId, StringComparison.OrdinalIgnoreCase))
            {
                color = candidate;
                return true;
            }
        }

        color = null;
        return false;
    }

    public string ResolveShapeIdOrDefault(string shapeId)
    {
        return TryGetShape(shapeId, out var shape) ? NormalizeId(shape.ShapeId) : DefaultShapeId;
    }

    public string ResolveColorIdOrDefault(string colorId)
    {
        return TryGetColor(colorId, out var color) ? NormalizeId(color.ColorId) : DefaultColorId;
    }

    private string GetFirstShapeId()
    {
        for (var index = 0; index < shapes.Count; index++)
        {
            if (shapes[index] != null && !string.IsNullOrWhiteSpace(shapes[index].ShapeId))
            {
                return NormalizeId(shapes[index].ShapeId);
            }
        }

        return string.Empty;
    }

    private string GetFirstColorId()
    {
        for (var index = 0; index < colors.Count; index++)
        {
            if (colors[index] != null && !string.IsNullOrWhiteSpace(colors[index].ColorId))
            {
                return NormalizeId(colors[index].ColorId);
            }
        }

        return string.Empty;
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

[Serializable]
public sealed class PlayerMarkerShapeDefinition
{
    [SerializeField] private string shapeId = "shape";
    [SerializeField] private string displayName = "Forma";
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private Vector3 markerScale = Vector3.one;
    [SerializeField] private Vector3 previewScale = Vector3.one;
    [SerializeField] private Vector3 previewEulerAngles = new(18f, -28f, 0f);

    public string ShapeId => shapeId;
    public string DisplayName => displayName;
    public GameObject VisualPrefab => visualPrefab;
    public Vector3 MarkerScale => markerScale;
    public Vector3 PreviewScale => previewScale;
    public Vector3 PreviewEulerAngles => previewEulerAngles;
}

[Serializable]
public sealed class PlayerMarkerColorDefinition
{
    [SerializeField] private string colorId = "color";
    [SerializeField] private string displayName = "Color";
    [SerializeField] private Color color = Color.white;

    public string ColorId => colorId;
    public string DisplayName => displayName;
    public Color Color => color;
}
