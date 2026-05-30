using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalPlayerMarkerProfileService : MonoBehaviour
{
    private const string DisplayNamePlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.DisplayName";
    private const string ShapeIdPlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.ShapeId";
    private const string ColorIdPlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.ColorId";

    [Header("Defaults")]
    [SerializeField] private PlayerMarkerAppearanceCatalogConfig appearanceCatalog;
    [SerializeField] private string defaultDisplayName = "Aventurero";
    [SerializeField] [Min(3)] private int maxDisplayNameLength = 18;
    [SerializeField] private bool saveSelectionToPlayerPrefs = true;

    private string currentDisplayName = string.Empty;
    private string currentShapeId = string.Empty;
    private string currentColorId = string.Empty;
    private bool isInitialized;

    public string CurrentDisplayName
    {
        get
        {
            EnsureInitialized();
            return currentDisplayName;
        }
    }

    public string CurrentShapeId
    {
        get
        {
            EnsureInitialized();
            return currentShapeId;
        }
    }

    public string CurrentColorId
    {
        get
        {
            EnsureInitialized();
            return currentColorId;
        }
    }
    public PlayerMarkerAppearanceCatalogConfig AppearanceCatalog => appearanceCatalog;

    public event Action ProfileChanged;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            isInitialized = false;
            EnsureInitialized();
        }
    }

    public void SetDisplayName(string displayName)
    {
        var normalizedName = NormalizeDisplayName(displayName);
        if (string.Equals(currentDisplayName, normalizedName, StringComparison.Ordinal))
        {
            return;
        }

        currentDisplayName = normalizedName;
        Save();
        ProfileChanged?.Invoke();
    }

    public void SetShapeId(string shapeId)
    {
        var resolvedShapeId = appearanceCatalog != null
            ? appearanceCatalog.ResolveShapeIdOrDefault(shapeId)
            : NormalizeIdentifier(shapeId);

        if (string.Equals(currentShapeId, resolvedShapeId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        currentShapeId = resolvedShapeId;
        Save();
        ProfileChanged?.Invoke();
    }

    public void SetColorId(string colorId)
    {
        var resolvedColorId = appearanceCatalog != null
            ? appearanceCatalog.ResolveColorIdOrDefault(colorId)
            : NormalizeIdentifier(colorId);

        if (string.Equals(currentColorId, resolvedColorId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        currentColorId = resolvedColorId;
        Save();
        ProfileChanged?.Invoke();
    }

    public void Reload()
    {
        currentDisplayName = NormalizeDisplayName(PlayerPrefs.GetString(DisplayNamePlayerPrefsKey, defaultDisplayName));
        currentShapeId = appearanceCatalog != null
            ? appearanceCatalog.ResolveShapeIdOrDefault(PlayerPrefs.GetString(ShapeIdPlayerPrefsKey, appearanceCatalog.DefaultShapeId))
            : NormalizeIdentifier(PlayerPrefs.GetString(ShapeIdPlayerPrefsKey, string.Empty));
        currentColorId = appearanceCatalog != null
            ? appearanceCatalog.ResolveColorIdOrDefault(PlayerPrefs.GetString(ColorIdPlayerPrefsKey, appearanceCatalog.DefaultColorId))
            : NormalizeIdentifier(PlayerPrefs.GetString(ColorIdPlayerPrefsKey, string.Empty));
        isInitialized = true;
    }

    public void EnsureInitialized()
    {
        if (isInitialized &&
            !string.IsNullOrWhiteSpace(currentDisplayName) &&
            !string.IsNullOrWhiteSpace(currentShapeId) &&
            !string.IsNullOrWhiteSpace(currentColorId))
        {
            return;
        }

        Reload();
    }

    public bool TryGetSelectedShape(out PlayerMarkerShapeDefinition shape)
    {
        EnsureInitialized();
        if (appearanceCatalog != null)
        {
            return appearanceCatalog.TryGetShape(currentShapeId, out shape);
        }

        shape = null;
        return false;
    }

    public bool TryGetSelectedColor(out PlayerMarkerColorDefinition color)
    {
        EnsureInitialized();
        if (appearanceCatalog != null)
        {
            return appearanceCatalog.TryGetColor(currentColorId, out color);
        }

        color = null;
        return false;
    }

    private void Save()
    {
        if (!saveSelectionToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetString(DisplayNamePlayerPrefsKey, currentDisplayName);
        PlayerPrefs.SetString(ShapeIdPlayerPrefsKey, currentShapeId);
        PlayerPrefs.SetString(ColorIdPlayerPrefsKey, currentColorId);
        PlayerPrefs.Save();
    }

    private string NormalizeDisplayName(string displayName)
    {
        var normalized = string.IsNullOrWhiteSpace(displayName) ? defaultDisplayName : displayName.Trim();
        normalized = normalized.Replace("\r", string.Empty).Replace("\n", string.Empty);

        if (normalized.Length > maxDisplayNameLength)
        {
            normalized = normalized[..maxDisplayNameLength];
        }

        return string.IsNullOrWhiteSpace(normalized) ? defaultDisplayName : normalized;
    }

    private static string NormalizeIdentifier(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
