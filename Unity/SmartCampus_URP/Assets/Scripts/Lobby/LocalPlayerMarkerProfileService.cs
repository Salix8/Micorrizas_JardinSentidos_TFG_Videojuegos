using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalPlayerMarkerProfileService : MonoBehaviour
{
    private const string DisplayNamePlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.DisplayName";
    private const string AvatarIdPlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.AvatarId";
    private const string LegacyShapeIdPlayerPrefsKey = "SmartCampus.Lobby.PlayerMarker.ShapeId";

    [Header("Defaults")]
    [SerializeField] private PlayerMarkerAppearanceCatalogConfig appearanceCatalog;
    [SerializeField] private string defaultDisplayName = "Aventurero";
    [SerializeField] [Min(3)] private int maxDisplayNameLength = 18;
    [SerializeField] private bool saveSelectionToPlayerPrefs = true;

    private string currentDisplayName = string.Empty;
    private string currentAvatarId = string.Empty;
    private bool isInitialized;

    public string CurrentDisplayName
    {
        get
        {
            EnsureInitialized();
            return currentDisplayName;
        }
    }

    public string CurrentAvatarId
    {
        get
        {
            EnsureInitialized();
            return currentAvatarId;
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

    public void SetAvatarId(string avatarId)
    {
        var resolvedAvatarId = appearanceCatalog != null
            ? appearanceCatalog.ResolveAvatarIdOrDefault(avatarId)
            : NormalizeIdentifier(avatarId);

        if (string.Equals(currentAvatarId, resolvedAvatarId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        currentAvatarId = resolvedAvatarId;
        Save();
        ProfileChanged?.Invoke();
    }

    public void Reload()
    {
        currentDisplayName = NormalizeDisplayName(PlayerPrefs.GetString(DisplayNamePlayerPrefsKey, defaultDisplayName));
        var savedAvatarId = PlayerPrefs.GetString(
            AvatarIdPlayerPrefsKey,
            PlayerPrefs.GetString(LegacyShapeIdPlayerPrefsKey, appearanceCatalog != null ? appearanceCatalog.DefaultAvatarId : string.Empty));
        currentAvatarId = appearanceCatalog != null
            ? appearanceCatalog.ResolveAvatarIdOrDefault(savedAvatarId)
            : NormalizeIdentifier(savedAvatarId);
        isInitialized = true;
    }

    public void EnsureInitialized()
    {
        if (isInitialized &&
            !string.IsNullOrWhiteSpace(currentDisplayName) &&
            !string.IsNullOrWhiteSpace(currentAvatarId))
        {
            return;
        }

        Reload();
    }

    public bool TryGetSelectedAvatar(out PlayerMarkerAvatarDefinition avatar)
    {
        EnsureInitialized();
        if (appearanceCatalog != null)
        {
            return appearanceCatalog.TryGetAvatar(currentAvatarId, out avatar);
        }

        avatar = null;
        return false;
    }

    private void Save()
    {
        if (!saveSelectionToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetString(DisplayNamePlayerPrefsKey, currentDisplayName);
        PlayerPrefs.SetString(AvatarIdPlayerPrefsKey, currentAvatarId);
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
