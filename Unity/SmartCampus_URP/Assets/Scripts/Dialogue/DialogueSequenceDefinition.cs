using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    [Serializable]
    public sealed class DialogueSequenceDefinition
    {
        public DialogueSequenceDefinition(string key, IReadOnlyList<DialogueLineDefinition> lines)
        {
            Key = key == null ? string.Empty : key.Trim();
            Lines = lines ?? Array.Empty<DialogueLineDefinition>();
        }

        public string Key { get; }
        public IReadOnlyList<DialogueLineDefinition> Lines { get; }
    }
}
