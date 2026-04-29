using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsCardView : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image frameImage;
        [SerializeField] private GameObject frontFaceRoot;
        [SerializeField] private Image frontFaceBackground;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private GameObject backFaceRoot;
        [SerializeField] private Image backFaceBackground;
        [SerializeField] private TMP_Text backFaceLabel;

        private RectTransform cachedRectTransform;

        public RectTransform RectTransform => cachedRectTransform != null
            ? cachedRectTransform
            : cachedRectTransform = GetComponent<RectTransform>();

        public void Bind(
            DistributedPairsCardNetworkState state,
            DistributedPairDefinition pairDefinition,
            DistributedPairsCardVisualSettings visualSettings,
            bool isInteractable,
            Action<int> onSelected)
        {
            var isSelected = state.IsSelected;

            if (frameImage != null)
            {
                frameImage.color = isSelected ? visualSettings.SelectedFrameColor : visualSettings.FrameColor;
            }

            if (frontFaceRoot != null)
            {
                frontFaceRoot.SetActive(isSelected);
            }

            if (backFaceRoot != null)
            {
                backFaceRoot.SetActive(!isSelected);
            }

            if (frontFaceBackground != null)
            {
                frontFaceBackground.color = pairDefinition == null ? Color.white : pairDefinition.FaceColor;
            }

            if (illustrationImage != null)
            {
                var hasIllustration = pairDefinition != null && pairDefinition.Illustration != null;
                illustrationImage.gameObject.SetActive(hasIllustration);
                illustrationImage.sprite = hasIllustration ? pairDefinition.Illustration : null;
                illustrationImage.preserveAspect = true;
            }

            if (titleLabel != null)
            {
                titleLabel.text = pairDefinition == null ? "Carta" : pairDefinition.Title;
                titleLabel.color = visualSettings.FrontTextColor;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = pairDefinition == null ? string.Empty : pairDefinition.Description;
                descriptionLabel.color = visualSettings.FrontTextColor;
            }

            if (backFaceBackground != null)
            {
                backFaceBackground.color = visualSettings.BackColor;
            }

            if (backFaceLabel != null)
            {
                backFaceLabel.text = $"Carta {state.HandOrder + 1}";
                backFaceLabel.color = visualSettings.BackTextColor;
            }

            if (selectionButton != null)
            {
                selectionButton.onClick.RemoveAllListeners();
                selectionButton.interactable = isInteractable;
                if (onSelected != null)
                {
                    selectionButton.onClick.AddListener(() => onSelected(state.CardInstanceId));
                }
            }
        }

        public void BindEmptySlot(DistributedPairsCardVisualSettings visualSettings, int slotIndex)
        {
            if (frameImage != null)
            {
                var emptyFrameColor = visualSettings.FrameColor;
                emptyFrameColor.a = 0.28f;
                frameImage.color = emptyFrameColor;
            }

            if (frontFaceRoot != null)
            {
                frontFaceRoot.SetActive(false);
            }

            if (backFaceRoot != null)
            {
                backFaceRoot.SetActive(true);
            }

            if (backFaceBackground != null)
            {
                var emptyBackColor = visualSettings.BackColor;
                emptyBackColor.a = 0.16f;
                backFaceBackground.color = emptyBackColor;
            }

            if (backFaceLabel != null)
            {
                backFaceLabel.text = string.Empty;
                var emptyTextColor = visualSettings.BackTextColor;
                emptyTextColor.a = 0.45f;
                backFaceLabel.color = emptyTextColor;
            }

            if (selectionButton != null)
            {
                selectionButton.onClick.RemoveAllListeners();
                selectionButton.interactable = false;
            }

            if (titleLabel != null)
            {
                titleLabel.text = string.Empty;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = string.Empty;
            }

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.gameObject.SetActive(false);
            }
        }
    }
}
