using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerMarkerAppearanceCatalogConfig",
    menuName = "SmartCampus/Lobby/Player Marker Appearance Catalog")]
public sealed class PlayerMarkerAppearanceCatalogConfig : ScriptableObject
{
    [SerializeField] private List<PlayerMarkerAvatarDefinition> avatars = new();

    public IReadOnlyList<PlayerMarkerAvatarDefinition> Avatars => avatars;

    public string DefaultAvatarId => GetFirstAvatarId();

    public bool TryGetAvatar(string avatarId, out PlayerMarkerAvatarDefinition avatar)
    {
        var normalizedId = NormalizeId(avatarId);
        for (var index = 0; index < avatars.Count; index++)
        {
            var candidate = avatars[index];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.AvatarId))
            {
                continue;
            }

            if (string.Equals(candidate.AvatarId, normalizedId, StringComparison.OrdinalIgnoreCase))
            {
                avatar = candidate;
                return true;
            }
        }

        avatar = null;
        return false;
    }

    public string ResolveAvatarIdOrDefault(string avatarId)
    {
        return TryGetAvatar(avatarId, out var avatar) ? NormalizeId(avatar.AvatarId) : DefaultAvatarId;
    }

    private string GetFirstAvatarId()
    {
        for (var index = 0; index < avatars.Count; index++)
        {
            if (avatars[index] != null && !string.IsNullOrWhiteSpace(avatars[index].AvatarId))
            {
                return NormalizeId(avatars[index].AvatarId);
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
public sealed class PlayerMarkerAvatarDefinition
{
    [SerializeField] private string avatarId = "avatar";
    [SerializeField] private string displayName = "Avatar";
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Vector3 markerScale = Vector3.one;
    [SerializeField] private Vector2 previewSize = new(180f, 180f);

    public string AvatarId => avatarId;
    public string DisplayName => displayName;
    public Sprite AvatarSprite => avatarSprite;
    public Vector3 MarkerScale => markerScale;
    public Vector2 PreviewSize => previewSize;
}
