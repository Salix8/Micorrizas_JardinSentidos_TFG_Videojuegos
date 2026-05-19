using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Character Portrait Database", fileName = "CharacterPortraitDatabase")]
    public sealed class CharacterPortraitDatabase : ScriptableObject
    {
        [SerializeField] private Sprite fallbackPortrait;
        [SerializeField] private List<CharacterPortraitEntry> entries = new();

        public Sprite FallbackPortrait => fallbackPortrait;
        public IReadOnlyList<CharacterPortraitEntry> Entries => entries;

        public bool TryGetEntry(string characterId, out CharacterPortraitEntry entry)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var candidate = entries[index];
                if (candidate != null &&
                    string.Equals(candidate.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public Sprite GetPortraitOrFallback(string characterId)
        {
            return TryGetEntry(characterId, out var entry) && entry.Portrait != null
                ? entry.Portrait
                : fallbackPortrait;
        }

        public string GetDisplayNameOrFallback(string characterId)
        {
            if (TryGetEntry(characterId, out var entry) && !string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            return string.IsNullOrWhiteSpace(characterId) ? "Narrador" : characterId;
        }

        public GameObject GetPortraitVisualPrefabOrNull(string characterId)
        {
            return TryGetEntry(characterId, out var entry)
                ? entry.PortraitVisualPrefab
                : null;
        }
    }

    [Serializable]
    public sealed class CharacterPortraitEntry
    {
        [SerializeField] private string characterId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Sprite portrait;
        [SerializeField] private GameObject portraitVisualPrefab;

        public string CharacterId => characterId == null ? string.Empty : characterId.Trim();
        public string DisplayName => displayName == null ? string.Empty : displayName.Trim();
        public Sprite Portrait => portrait;
        public GameObject PortraitVisualPrefab => portraitVisualPrefab;
    }
}
