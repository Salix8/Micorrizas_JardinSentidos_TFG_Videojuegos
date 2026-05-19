using NUnit.Framework;
using SmartCampus.Dialogue;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueLanguageSettingsServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(DialogueLanguageSettingsService.PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(DialogueLanguageSettingsService.PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void Constructor_WithoutStoredValue_UsesDefaultLanguage()
        {
            var service = new DialogueLanguageSettingsService(DialogueLanguage.English);

            Assert.That(service.CurrentLanguage, Is.EqualTo(DialogueLanguage.English));
        }

        [Test]
        public void SetLanguage_PersistsValueForNextServiceInstance()
        {
            var service = new DialogueLanguageSettingsService(DialogueLanguage.Spanish);
            service.SetLanguage(DialogueLanguage.Valencian);

            var reloadedService = new DialogueLanguageSettingsService(DialogueLanguage.Spanish);

            Assert.That(reloadedService.CurrentLanguage, Is.EqualTo(DialogueLanguage.Valencian));
        }

        [Test]
        public void Constructor_WithInvalidStoredValue_FallsBackToDefaultLanguage()
        {
            PlayerPrefs.SetString(DialogueLanguageSettingsService.PlayerPrefsKey, "unknown-language");
            PlayerPrefs.Save();

            var service = new DialogueLanguageSettingsService(DialogueLanguage.Spanish);

            Assert.That(service.CurrentLanguage, Is.EqualTo(DialogueLanguage.Spanish));
        }
    }
}
