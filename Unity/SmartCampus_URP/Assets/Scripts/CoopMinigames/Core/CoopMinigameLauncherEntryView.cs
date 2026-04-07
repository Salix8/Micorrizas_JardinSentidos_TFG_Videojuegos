using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameLauncherEntryView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Button launchButton;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private bool preserveSceneTitleText;
        [SerializeField] private bool preserveSceneDescriptionText;
        [SerializeField] private bool preserveSceneButtonText;

        public void Bind(string displayName, string description, bool isInteractable, UnityEngine.Events.UnityAction onClick)
        {
            if (titleLabel != null)
            {
                if (!preserveSceneTitleText || string.IsNullOrWhiteSpace(titleLabel.text))
                {
                    titleLabel.text = displayName;
                }
            }

            if (descriptionLabel != null)
            {
                if (!preserveSceneDescriptionText || string.IsNullOrWhiteSpace(descriptionLabel.text))
                {
                    descriptionLabel.text = TruncateDescription(description);
                }

                descriptionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
            }

            if (launchButton != null)
            {
                buttonLabel ??= launchButton.GetComponentInChildren<Text>(true);
                if (buttonLabel != null && (!preserveSceneButtonText || string.IsNullOrWhiteSpace(buttonLabel.text)))
                {
                    buttonLabel.text = "Abrir";
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
            buttonLabel ??= launchButton != null ? launchButton.GetComponentInChildren<Text>(true) : null;
            preserveSceneTitleText = true;
            preserveSceneDescriptionText = true;
            preserveSceneButtonText = true;

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
    }
}
