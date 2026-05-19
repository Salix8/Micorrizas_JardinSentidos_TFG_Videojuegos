using System;

namespace SmartCampus.Dialogue
{
    [Serializable]
    public sealed class DialogueLineDefinition
    {
        public DialogueLineDefinition(
            string stringId,
            string characterId,
            string sequenceKey,
            string contextNotes,
            string spanishText,
            string englishText,
            string valencianText)
        {
            StringId = stringId == null ? string.Empty : stringId.Trim();
            CharacterId = characterId == null ? string.Empty : characterId.Trim();
            SequenceKey = sequenceKey == null ? string.Empty : sequenceKey.Trim();
            ContextNotes = contextNotes == null ? string.Empty : contextNotes.Trim();
            SpanishText = spanishText ?? string.Empty;
            EnglishText = englishText ?? string.Empty;
            ValencianText = valencianText ?? string.Empty;
        }

        public string StringId { get; }
        public string CharacterId { get; }
        public string SequenceKey { get; }
        public string ContextNotes { get; }
        public string SpanishText { get; }
        public string EnglishText { get; }
        public string ValencianText { get; }

        public string GetText(DialogueLanguage language)
        {
            return language switch
            {
                DialogueLanguage.English => FirstNonEmpty(EnglishText, SpanishText, ValencianText),
                DialogueLanguage.Valencian => FirstNonEmpty(ValencianText, SpanishText, EnglishText),
                _ => FirstNonEmpty(SpanishText, EnglishText, ValencianText)
            };
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(values[index]))
                {
                    return values[index];
                }
            }

            return string.Empty;
        }
    }
}
