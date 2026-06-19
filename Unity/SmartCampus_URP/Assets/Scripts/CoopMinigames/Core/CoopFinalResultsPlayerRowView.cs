using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopFinalResultsPlayerRowView : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text playerNameLabel;

        public void Initialize(Image avatar, TMP_Text playerName)
        {
            avatarImage = avatar;
            playerNameLabel = playerName;
        }

        public void Bind(string playerName, Sprite avatarSprite)
        {
            if (playerNameLabel != null)
            {
                playerNameLabel.text = string.IsNullOrWhiteSpace(playerName)
                    ? "Aventurero"
                    : playerName.Trim();
            }

            if (avatarImage == null)
            {
                return;
            }

            avatarImage.sprite = avatarSprite;
            avatarImage.preserveAspect = true;
            avatarImage.enabled = avatarSprite != null;
        }
    }
}
