using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public sealed class DialogueLine
    {
        private readonly Dictionary<string, string> localizedTexts;

        public DialogueLine(
            string stringId,
            string character,
            string actOrLocation,
            string contextNotes,
            IReadOnlyDictionary<string, string> localizedTexts)
        {
            StringId = stringId == null ? string.Empty : stringId.Trim();
            Character = character == null ? string.Empty : character.Trim();
            ActOrLocation = actOrLocation == null ? string.Empty : actOrLocation.Trim();
            ContextNotes = contextNotes == null ? string.Empty : contextNotes.Trim();
            this.localizedTexts = localizedTexts == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(localizedTexts, StringComparer.OrdinalIgnoreCase);
        }

        public string StringId { get; }
        public string Character { get; }
        public string ActOrLocation { get; }
        public string ContextNotes { get; }
        public IReadOnlyDictionary<string, string> LocalizedTexts => localizedTexts;

        public bool TryGetText(string locale, string fallbackLocale, out string text)
        {
            if (!string.IsNullOrWhiteSpace(locale) &&
                localizedTexts.TryGetValue(locale.Trim(), out text) &&
                !string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(fallbackLocale) &&
                localizedTexts.TryGetValue(fallbackLocale.Trim(), out text) &&
                !string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            foreach (var localizedText in localizedTexts.Values)
            {
                if (!string.IsNullOrWhiteSpace(localizedText))
                {
                    text = localizedText;
                    return true;
                }
            }

            text = string.Empty;
            return false;
        }
    }
}
