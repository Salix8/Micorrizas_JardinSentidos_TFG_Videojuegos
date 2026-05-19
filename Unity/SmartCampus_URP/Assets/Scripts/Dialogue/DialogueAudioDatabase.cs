using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Audio Database", fileName = "DialogueAudioDatabase")]
    public sealed class DialogueAudioDatabase : ScriptableObject
    {
        [SerializeField] private List<DialogueAudioEntry> entries = new();

        public IReadOnlyList<DialogueAudioEntry> Entries => entries;

        public bool TryGetClip(string stringId, out AudioClip clip)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry != null &&
                    string.Equals(entry.StringId, stringId, StringComparison.OrdinalIgnoreCase))
                {
                    clip = entry.Clip;
                    return clip != null;
                }
            }

            clip = null;
            return false;
        }

        public AudioClip GetClipOrNull(string stringId)
        {
            return TryGetClip(stringId, out var clip) ? clip : null;
        }
    }

    [Serializable]
    public sealed class DialogueAudioEntry
    {
        [SerializeField] private string stringId = string.Empty;
        [SerializeField] private AudioClip clip;

        public string StringId => stringId == null ? string.Empty : stringId.Trim();
        public AudioClip Clip => clip;
    }
}
