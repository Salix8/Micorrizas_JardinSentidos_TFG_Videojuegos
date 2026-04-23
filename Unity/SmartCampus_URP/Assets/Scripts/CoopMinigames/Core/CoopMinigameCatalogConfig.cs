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
    }

    [Serializable]
    public sealed class CoopMinigameCatalogEntry
    {
        [SerializeField] private int minigameIndex;
        [SerializeField] private string displayName = "Minijuego";
        [SerializeField] [TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private string sceneName = string.Empty;
        [SerializeField] private string storyDialogueActOrLocation = string.Empty;
        [SerializeField] private string[] storyDialogueLineIds = Array.Empty<string>();

        public int MinigameIndex => minigameIndex;
        public string DisplayName => displayName;
        public string Description => description;
        public string SceneName => sceneName;
        public string StoryDialogueActOrLocation => storyDialogueActOrLocation;
        public IReadOnlyList<string> StoryDialogueLineIds => storyDialogueLineIds;

        public bool HasStoryDialogue => HasStoryDialogueLineIds || !string.IsNullOrWhiteSpace(storyDialogueActOrLocation);
        public bool HasStoryDialogueLineIds
        {
            get
            {
                if (storyDialogueLineIds == null)
                {
                    return false;
                }

                for (var index = 0; index < storyDialogueLineIds.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(storyDialogueLineIds[index]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
