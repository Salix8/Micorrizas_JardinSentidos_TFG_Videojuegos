using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public sealed class DialogueCatalogService
    {
        private readonly Dictionary<string, DialogueSequenceDefinition> sequencesByKey;
        private readonly Dictionary<string, DialogueLineDefinition> linesById;
        private readonly List<string> orderedSequenceKeys;

        private DialogueCatalogService(
            Dictionary<string, DialogueSequenceDefinition> sequencesByKey,
            Dictionary<string, DialogueLineDefinition> linesById,
            List<string> orderedSequenceKeys)
        {
            this.sequencesByKey = sequencesByKey;
            this.linesById = linesById;
            this.orderedSequenceKeys = orderedSequenceKeys;
        }

        public IReadOnlyList<string> SequenceKeys => orderedSequenceKeys;

        public static bool TryCreate(string csvContent, out DialogueCatalogService catalog, out string errorMessage)
        {
            catalog = null;
            if (!DialogueCsvService.TryParse(csvContent, out var lines, out errorMessage))
            {
                return false;
            }

            var sequencesByKey = new Dictionary<string, DialogueSequenceDefinition>(StringComparer.OrdinalIgnoreCase);
            var linesById = new Dictionary<string, DialogueLineDefinition>(StringComparer.OrdinalIgnoreCase);
            var groupedLines = new Dictionary<string, List<DialogueLineDefinition>>(StringComparer.OrdinalIgnoreCase);
            var orderedSequenceKeys = new List<string>();

            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                linesById.Add(line.StringId, line);

                if (!groupedLines.TryGetValue(line.SequenceKey, out var sequenceLines))
                {
                    sequenceLines = new List<DialogueLineDefinition>();
                    groupedLines.Add(line.SequenceKey, sequenceLines);
                    orderedSequenceKeys.Add(line.SequenceKey);
                }

                sequenceLines.Add(line);
            }

            for (var index = 0; index < orderedSequenceKeys.Count; index++)
            {
                var key = orderedSequenceKeys[index];
                sequencesByKey.Add(key, new DialogueSequenceDefinition(key, groupedLines[key]));
            }

            catalog = new DialogueCatalogService(sequencesByKey, linesById, orderedSequenceKeys);
            errorMessage = string.Empty;
            return true;
        }

        public bool TryGetSequence(string sequenceKey, out DialogueSequenceDefinition sequence)
        {
            return sequencesByKey.TryGetValue(sequenceKey ?? string.Empty, out sequence);
        }

        public bool TryGetLine(string stringId, out DialogueLineDefinition line)
        {
            return linesById.TryGetValue(stringId ?? string.Empty, out line);
        }

        public bool TryBuildSingleLineSequence(string stringId, out DialogueSequenceDefinition sequence)
        {
            sequence = null;
            if (!TryGetLine(stringId, out var line))
            {
                return false;
            }

            sequence = new DialogueSequenceDefinition(line.StringId, new[] { line });
            return true;
        }
    }
}
