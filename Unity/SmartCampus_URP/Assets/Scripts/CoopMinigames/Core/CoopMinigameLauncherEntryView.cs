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

        public void Bind(string displayName, string description, bool isInteractable, UnityEngine.Events.UnityAction onClick)
        {
            if (titleLabel != null)
            {
                titleLabel.text = displayName;
                titleLabel.resizeTextForBestFit = true;
                titleLabel.resizeTextMinSize = 14;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = TruncateDescription(description);
                descriptionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
                descriptionLabel.resizeTextForBestFit = true;
                descriptionLabel.resizeTextMinSize = 12;
            }

            if (launchButton != null)
            {
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
    }
}
