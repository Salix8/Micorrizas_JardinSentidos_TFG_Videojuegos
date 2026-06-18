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
        [SerializeField] private Image mismatchMemoryOverlay;
        [SerializeField] private GameObject backFaceRoot;
        [SerializeField] private Image backFaceBackground;
        [SerializeField] private TMP_Text backFaceLabel;

        private RectTransform cachedRectTransform;
        private Vector3 restingScale = Vector3.one;
        private bool matchedFeedbackActive;
        private float matchedFeedbackStartedAt;
        private float matchedPulseScale = 1f;
        private float matchedPulseDuration = 0.2f;

        public RectTransform RectTransform => cachedRectTransform != null
            ? cachedRectTransform
            : cachedRectTransform = GetComponent<RectTransform>();

        public void Bind(
            DistributedPairsCardNetworkState state,
            DistributedPairDefinition pairDefinition,
            DistributedPairsCardVisualSettings visualSettings,
            DistributedPairsMatchFeedbackSettings matchFeedbackSettings,
            bool isInteractable,
            bool showMismatchMemoryOverlay,
            bool showMatchedFeedback,
            Action<int> onSelected)
        {
            var isSelected = state.IsSelected;

            if (frameImage != null)
            {
                frameImage.color = showMatchedFeedback && isSelected
                    ? matchFeedbackSettings.MatchedFrameColor
                    : isSelected
                        ? visualSettings.SelectedFrameColor
                        : visualSettings.FrameColor;
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
                var illustrationColor = illustrationImage.color;
                illustrationColor.a = hasIllustration ? 1f : 0f;
                illustrationImage.color = illustrationColor;
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

            EnsureMismatchMemoryOverlay();
            if (mismatchMemoryOverlay != null)
            {
                mismatchMemoryOverlay.gameObject.SetActive(isSelected && showMismatchMemoryOverlay);
            }

            if (backFaceBackground != null)
            {
                backFaceBackground.color = visualSettings.BackColor;
            }

            if (backFaceLabel != null)
            {
                backFaceLabel.text = pairDefinition == null || string.IsNullOrWhiteSpace(pairDefinition.FlavorHint)
                    ? $"Carta {state.HandOrder + 1}"
                    : pairDefinition.FlavorHint.Trim();
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

            UpdateMatchedFeedback(showMatchedFeedback && isSelected, matchFeedbackSettings);
        }

        public void BindEmptySlot(DistributedPairsCardVisualSettings visualSettings, int slotIndex)
        {
            StopMatchedFeedback();

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

            EnsureMismatchMemoryOverlay();
            if (mismatchMemoryOverlay != null)
            {
                mismatchMemoryOverlay.gameObject.SetActive(false);
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

        private void Awake()
        {
            if (RectTransform != null)
            {
                restingScale = RectTransform.localScale;
            }
        }

        private void LateUpdate()
        {
            if (!matchedFeedbackActive || RectTransform == null)
            {
                return;
            }

            var duration = Mathf.Max(0.05f, matchedPulseDuration);
            var cycle = (Time.unscaledTime - matchedFeedbackStartedAt) / duration;
            var pulse = 0.5f - (0.5f * Mathf.Cos(cycle * Mathf.PI * 2f));
            var scale = Mathf.LerpUnclamped(1f, matchedPulseScale, pulse);
            RectTransform.localScale = restingScale * scale;
        }

        private void UpdateMatchedFeedback(bool shouldShowMatchedFeedback, DistributedPairsMatchFeedbackSettings matchFeedbackSettings)
        {
            matchedPulseScale = Mathf.Max(1f, matchFeedbackSettings.MatchedPulseScale);
            matchedPulseDuration = Mathf.Max(0.05f, matchFeedbackSettings.MatchedPulseDuration);

            if (RectTransform != null && !matchedFeedbackActive)
            {
                restingScale = RectTransform.localScale;
            }

            if (!shouldShowMatchedFeedback)
            {
                StopMatchedFeedback();
                return;
            }

            if (!matchedFeedbackActive)
            {
                matchedFeedbackStartedAt = Time.unscaledTime;
            }

            matchedFeedbackActive = true;
        }

        private void StopMatchedFeedback()
        {
            matchedFeedbackActive = false;
            if (RectTransform != null)
            {
                RectTransform.localScale = restingScale;
            }
        }

        private void EnsureMismatchMemoryOverlay()
        {
            if (mismatchMemoryOverlay != null || frontFaceRoot == null)
            {
                return;
            }

            var overlayObject = new GameObject("MismatchMemoryOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.layer = frontFaceRoot.layer;
            overlayObject.transform.SetParent(frontFaceRoot.transform, false);
            overlayObject.transform.SetAsLastSibling();

            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            mismatchMemoryOverlay = overlayObject.GetComponent<Image>();
            mismatchMemoryOverlay.raycastTarget = false;
            mismatchMemoryOverlay.color = new Color(0.96f, 0.98f, 1f, 0.24f);
            mismatchMemoryOverlay.gameObject.SetActive(false);
        }
    }
}
