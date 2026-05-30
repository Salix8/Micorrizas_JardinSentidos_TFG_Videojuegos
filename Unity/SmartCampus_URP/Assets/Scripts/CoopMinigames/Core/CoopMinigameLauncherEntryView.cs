using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameLauncherEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private Button launchButton;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private bool preserveSceneTitleText;
        [SerializeField] private bool preserveSceneDescriptionText;
        [SerializeField] private bool preserveSceneButtonText;

        public void Bind(
            string displayName,
            string description,
            bool isInteractable,
            UnityEngine.Events.UnityAction onClick,
            string buttonText = "Abrir")
        {
            ResolveReferences();
            var shouldForceRuntimeBinding = Application.isPlaying;

            if (titleLabel != null)
            {
                if (shouldForceRuntimeBinding || !preserveSceneTitleText || string.IsNullOrWhiteSpace(titleLabel.text))
                {
                    titleLabel.text = displayName;
                }
            }

            if (descriptionLabel != null)
            {
                if (shouldForceRuntimeBinding || !preserveSceneDescriptionText || string.IsNullOrWhiteSpace(descriptionLabel.text))
                {
                    descriptionLabel.text = TruncateDescription(description);
                }

                descriptionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
            }

            if (launchButton != null)
            {
                buttonLabel ??= launchButton.GetComponentInChildren<TMP_Text>(true);
                if (buttonLabel != null && (shouldForceRuntimeBinding || !preserveSceneButtonText || string.IsNullOrWhiteSpace(buttonLabel.text)))
                {
                    buttonLabel.text = string.IsNullOrWhiteSpace(buttonText) ? "Abrir" : buttonText;
                }

                launchButton.onClick.RemoveAllListeners();
                launchButton.interactable = isInteractable;
                if (onClick != null)
                {
                    launchButton.onClick.AddListener(onClick);
                }
            }
        }

        private static string TruncateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Length <= 120)
            {
                return description;
            }

            return description.Substring(0, 117).TrimEnd() + "...";
        }

        public void ConfigureForSceneAuthoredPresentation(string displayName, string description, string buttonText)
        {
            ResolveReferences();
            buttonLabel ??= launchButton != null ? launchButton.GetComponentInChildren<TMP_Text>(true) : null;
            preserveSceneTitleText = false;
            preserveSceneDescriptionText = false;
            preserveSceneButtonText = false;

            if (titleLabel != null)
            {
                titleLabel.text = displayName;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = description;
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = buttonText;
            }
        }

        private void ResolveReferences()
        {
            titleLabel ??= transform.Find("TitleLabel")?.GetComponent<TMP_Text>();
            descriptionLabel ??= transform.Find("DescriptionLabel")?.GetComponent<TMP_Text>();
            launchButton ??= transform.Find("LaunchButton")?.GetComponent<Button>();
            buttonLabel ??= launchButton != null ? launchButton.GetComponentInChildren<TMP_Text>(true) : null;
        }
    }
}
