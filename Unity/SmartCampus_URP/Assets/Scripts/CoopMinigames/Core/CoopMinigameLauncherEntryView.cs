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
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = description;
                descriptionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
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
    }
}
