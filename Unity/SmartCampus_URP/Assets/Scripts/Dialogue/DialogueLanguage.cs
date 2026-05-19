using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public enum DialogueLanguage
    {
        Spanish = 0,
        English = 1,
        Valencian = 2
    }

    public static class DialogueLanguageUtility
    {
        private static readonly DialogueLanguage[] SupportedLanguageValues =
        {
            DialogueLanguage.Spanish,
            DialogueLanguage.English,
            DialogueLanguage.Valencian
        };

        public static IReadOnlyList<DialogueLanguage> SupportedLanguages => SupportedLanguageValues;

        public static string GetDisplayName(DialogueLanguage language)
        {
            return language switch
            {
                DialogueLanguage.English => "English",
                DialogueLanguage.Valencian => "Valenciano",
                _ => "Espanol"
            };
        }

        public static string GetPersistentValue(DialogueLanguage language)
        {
            return language switch
            {
                DialogueLanguage.English => "en-US",
                DialogueLanguage.Valencian => "ca-CA",
                _ => "es-ES"
            };
        }

        public static bool TryParsePersistentValue(string rawValue, out DialogueLanguage language)
        {
            language = DialogueLanguage.Spanish;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var sanitizedValue = rawValue.Trim();
            if (Enum.TryParse(sanitizedValue, ignoreCase: true, out language))
            {
                return true;
            }

            if (int.TryParse(sanitizedValue, out var numericValue) &&
                Enum.IsDefined(typeof(DialogueLanguage), numericValue))
            {
                language = (DialogueLanguage)numericValue;
                return true;
            }

            switch (sanitizedValue.ToLowerInvariant())
            {
                case "es":
                case "es-es":
                case "spanish":
                case "espanol":
                    language = DialogueLanguage.Spanish;
                    return true;
                case "en":
                case "en-us":
                case "english":
                    language = DialogueLanguage.English;
                    return true;
                case "ca":
                case "ca-ca":
                case "catalan":
                case "valencian":
                case "valenciano":
                    language = DialogueLanguage.Valencian;
                    return true;
                default:
                    return false;
            }
        }
    }
}
