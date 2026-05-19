using TMPro;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueLanguageSelectorUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueSystemConfig dialogueSystemConfig;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private DialogueUIController targetDialogueUIController;

        private DialogueLanguageSettingsService languageSettingsService;
        private bool suppressDropdownCallback;

        private void Awake()
        {
            targetDialogueUIController ??= FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            InitializeService();
            PopulateDropdown();

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(HandleDropdownValueChanged);
            }

            if (languageSettingsService != null)
            {
                languageSettingsService.LanguageChanged += HandleStoredLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveListener(HandleDropdownValueChanged);
            }

            if (languageSettingsService != null)
            {
                languageSettingsService.LanguageChanged -= HandleStoredLanguageChanged;
            }
        }

        private void InitializeService()
        {
            if (languageSettingsService != null)
            {
                return;
            }

            var defaultLanguage = dialogueSystemConfig == null
                ? DialogueLanguage.Spanish
                : dialogueSystemConfig.DefaultLanguage;
            languageSettingsService = new DialogueLanguageSettingsService(defaultLanguage);
        }

        private void PopulateDropdown()
        {
            if (languageDropdown == null)
            {
                return;
            }

            suppressDropdownCallback = true;
            languageDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<string>();
            var supportedLanguages = DialogueLanguageUtility.SupportedLanguages;
            for (var index = 0; index < supportedLanguages.Count; index++)
            {
                options.Add(DialogueLanguageUtility.GetDisplayName(supportedLanguages[index]));
            }

            languageDropdown.AddOptions(options);
            languageDropdown.value = GetCurrentLanguageIndex();
            languageDropdown.RefreshShownValue();
            suppressDropdownCallback = false;
        }

        private void HandleDropdownValueChanged(int selectedIndex)
        {
            if (suppressDropdownCallback)
            {
                return;
            }

            var supportedLanguages = DialogueLanguageUtility.SupportedLanguages;
            var safeIndex = Mathf.Clamp(selectedIndex, 0, supportedLanguages.Count - 1);
            var selectedLanguage = supportedLanguages[safeIndex];
            languageSettingsService.SetLanguage(selectedLanguage);

            if (targetDialogueUIController != null)
            {
                targetDialogueUIController.SetLanguage(selectedLanguage);
            }
        }

        private void HandleStoredLanguageChanged(DialogueLanguage language)
        {
            if (languageDropdown != null)
            {
                suppressDropdownCallback = true;
                languageDropdown.value = GetLanguageIndex(language);
                languageDropdown.RefreshShownValue();
                suppressDropdownCallback = false;
            }

            if (targetDialogueUIController != null)
            {
                targetDialogueUIController.SetLanguage(language);
            }
        }

        private int GetCurrentLanguageIndex()
        {
            return GetLanguageIndex(languageSettingsService == null
                ? dialogueSystemConfig == null ? DialogueLanguage.Spanish : dialogueSystemConfig.DefaultLanguage
                : languageSettingsService.CurrentLanguage);
        }

        private static int GetLanguageIndex(DialogueLanguage language)
        {
            var supportedLanguages = DialogueLanguageUtility.SupportedLanguages;
            for (var index = 0; index < supportedLanguages.Count; index++)
            {
                if (supportedLanguages[index] == language)
                {
                    return index;
                }
            }

            return 0;
        }
    }
}
