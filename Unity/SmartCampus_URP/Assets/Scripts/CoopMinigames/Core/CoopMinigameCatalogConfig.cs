using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Catalog", fileName = "CoopMinigameCatalog")]
    public sealed class CoopMinigameCatalogConfig : ScriptableObject
    {
        [SerializeField] private List<CoopMinigameCatalogEntry> entries = new();

        public IReadOnlyList<CoopMinigameCatalogEntry> Entries => entries;
        public int Count => entries.Count;

        public bool TryGetEntryForMinigameIndex(int minigameIndex, out CoopMinigameCatalogEntry entry)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && entries[index].MinigameIndex == minigameIndex)
                {
                    entry = entries[index];
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class CoopMinigameCatalogEntry
    {
        [SerializeField] private int minigameIndex;
        [SerializeField] private string displayName = "Minijuego";
        [SerializeField] [TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private string sceneName = string.Empty;

        public int MinigameIndex => minigameIndex;
        public string DisplayName => displayName;
        public string Description => description;
        public string SceneName => sceneName;
    }
}
