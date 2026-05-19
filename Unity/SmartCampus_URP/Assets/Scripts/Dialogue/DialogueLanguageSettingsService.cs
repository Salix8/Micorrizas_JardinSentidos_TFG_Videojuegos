using System;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    public sealed class DialogueLanguageSettingsService
    {
        public const string PlayerPrefsKey = "SmartCampus.Dialogue.Language";

        private readonly DialogueLanguage defaultLanguage;
        private DialogueLanguage currentLanguage;

        public DialogueLanguageSettingsService(DialogueLanguage defaultLanguage)
        {
            this.defaultLanguage = defaultLanguage;
            currentLanguage = LoadStoredOrDefault();
        }

        public DialogueLanguage CurrentLanguage => currentLanguage;

        public event Action<DialogueLanguage> LanguageChanged;

        public void SetLanguage(DialogueLanguage language)
        {
            if (currentLanguage == language)
            {
                return;
            }

            currentLanguage = language;
            PlayerPrefs.SetString(PlayerPrefsKey, DialogueLanguageUtility.GetPersistentValue(language));
            PlayerPrefs.Save();
            LanguageChanged?.Invoke(currentLanguage);
        }

        public void Reload()
        {
            var loadedLanguage = LoadStoredOrDefault();
            if (loadedLanguage == currentLanguage)
            {
                return;
            }

            currentLanguage = loadedLanguage;
            LanguageChanged?.Invoke(currentLanguage);
        }

        private DialogueLanguage LoadStoredOrDefault()
        {
            var storedValue = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            return DialogueLanguageUtility.TryParsePersistentValue(storedValue, out var parsedLanguage)
                ? parsedLanguage
                : defaultLanguage;
        }
    }
}
