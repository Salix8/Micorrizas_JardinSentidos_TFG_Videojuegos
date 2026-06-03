using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameTopPanelView : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private CoopMinigameThemeConfig themeConfig;

        [Header("Backgrounds")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image titleBandImage;

        [Header("Brand")]
        [SerializeField] private Image logoImage;
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_Text projectSubtitleLabel;

        [Header("Progress")]
        [SerializeField] private TMP_Text progressTitleLabel;
        [SerializeField] private TMP_Text progressPercentLabel;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Image progressBackgroundImage;

        [Header("Team")]
        [SerializeField] private TMP_Text teamTitleLabel;
        [SerializeField] private TMP_Text teamNameLabel;
        [SerializeField] private List<Image> avatarImages = new();

        [Header("Minigame Title")]
        [SerializeField] private TMP_Text minigameTitleLabel;
        [SerializeField] private Image leftLeafImage;
        [SerializeField] private Image rightLeafImage;

        public CoopMinigameThemeConfig ThemeConfig => themeConfig;

        private void Awake()
        {
            ApplyTheme();
        }

        private void OnValidate()
        {
            ApplyTheme();
        }

        public void Bind(string minigameTitle, float globalProgress01, string teamName, string roomCode)
        {
            ApplyTheme();
            SetMinigameTitle(minigameTitle);
            SetProgress(globalProgress01);
            SetTeam(teamName, roomCode);
        }

        public void SetTheme(CoopMinigameThemeConfig theme)
        {
            themeConfig = theme;
            ApplyTheme();
        }

        public void SetMinigameTitle(string minigameTitle)
        {
            if (minigameTitleLabel == null || themeConfig == null)
            {
                return;
            }

            var normalizedTitle = string.IsNullOrWhiteSpace(minigameTitle) ? string.Empty : minigameTitle.Trim();
            minigameTitleLabel.text = string.IsNullOrWhiteSpace(normalizedTitle)
                ? themeConfig.MinigameTitlePrefix
                : $"{themeConfig.MinigameTitlePrefix} {normalizedTitle}";
        }

        public void SetProgress(float progress01)
        {
            var clampedProgress = Mathf.Clamp01(progress01);
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = clampedProgress;
            }

            if (progressPercentLabel != null && themeConfig != null)
            {
                progressPercentLabel.text = string.Format(themeConfig.GlobalProgressPercentFormat, clampedProgress * 100f);
            }
        }

        public void SetTeam(string teamName, string roomCode)
        {
            if (themeConfig == null || teamNameLabel == null)
            {
                return;
            }

            teamNameLabel.text = themeConfig.ResolveTeamDisplayName(teamName, roomCode);
        }

        public void SetAvatarSprites(IReadOnlyList<Sprite> avatarSprites)
        {
            if (avatarImages == null || avatarImages.Count == 0)
            {
                return;
            }

            for (var index = 0; index < avatarImages.Count; index++)
            {
                var image = avatarImages[index];
                if (image == null)
                {
                    continue;
                }

                var hasSprite = avatarSprites != null && index < avatarSprites.Count && avatarSprites[index] != null;
                image.sprite = hasSprite ? avatarSprites[index] : null;
                image.enabled = hasSprite;
                image.gameObject.SetActive(hasSprite);
            }
        }

        public void ApplyTheme()
        {
            if (themeConfig == null)
            {
                return;
            }

            var palette = themeConfig.Palette;
            SetImageColor(backgroundImage, palette.PanelBackground);
            SetImageColor(titleBandImage, palette.PrimaryGreen);
            SetImageColor(progressFillImage, palette.ProgressGreen);
            SetImageColor(progressBackgroundImage, palette.Cream);

            if (logoImage != null)
            {
                logoImage.sprite = themeConfig.LogoSprite;
                logoImage.enabled = themeConfig.LogoSprite != null;
                logoImage.preserveAspect = true;
            }

            SetImageSprite(leftLeafImage, themeConfig.Icons.LeafIcon);
            SetImageSprite(rightLeafImage, themeConfig.Icons.LeafIcon);

            ApplyText(projectTitleLabel, themeConfig.ProjectTitle, themeConfig.Typography.ProjectTitleSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(projectSubtitleLabel, themeConfig.ProjectSubtitle, themeConfig.Typography.BodySize, palette.TextPrimary, FontStyles.Normal);
            ApplyText(progressTitleLabel, themeConfig.GlobalProgressLabel, themeConfig.Typography.CaptionSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(teamTitleLabel, themeConfig.TeamTitle, themeConfig.Typography.CaptionSize, palette.SecondaryGreen, FontStyles.Bold);
            ApplyText(teamNameLabel, themeConfig.ResolveTeamDisplayName(null, null), themeConfig.Typography.CaptionSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(minigameTitleLabel, themeConfig.MinigameTitlePrefix, themeConfig.Typography.MinigameTitleSize, Color.white, FontStyles.Bold);

            if (progressPercentLabel != null)
            {
                ApplyText(progressPercentLabel, progressPercentLabel.text, themeConfig.Typography.BodyLargeSize, palette.SecondaryGreen, FontStyles.Bold);
            }

            if (progressSlider != null)
            {
                progressSlider.wholeNumbers = false;
                progressSlider.interactable = false;
            }

            SetAvatarSprites(themeConfig.DefaultAvatarSprites);
        }

        private void ApplyText(TMP_Text label, string value, int fontSize, Color color, FontStyles style)
        {
            if (label == null)
            {
                return;
            }

            if (themeConfig.PrimaryFont != null)
            {
                label.font = themeConfig.PrimaryFont;
            }

            label.text = value;
            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = style;
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
        }
    }
}
