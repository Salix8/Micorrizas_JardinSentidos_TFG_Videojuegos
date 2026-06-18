using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameBottomPanelView : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private CoopMinigameThemeConfig themeConfig;

        [Header("Instruction")]
        [SerializeField] private RoundedPanelGraphic instructionPanelGraphic;
        [SerializeField] private Image instructionPanelImage;
        [SerializeField] private RoundedPanelGraphic instructionIconCircleGraphic;
        [SerializeField] private Image instructionIconImage;
        [SerializeField] private TMP_Text instructionTitleLabel;
        [SerializeField] private TMP_Text instructionBodyLabel;

        [Header("Timer")]
        [SerializeField] private RoundedPanelGraphic timerPanelGraphic;
        [SerializeField] private Image timerPanelImage;
        [SerializeField] private RoundedPanelGraphic timerIconCircleGraphic;
        [SerializeField] private Image timerIconImage;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text timeValueLabel;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private Image timeSliderFillImage;
        [SerializeField] private Image timeSliderBackgroundImage;

        [Header("Penalty")]
        [SerializeField] private Image dividerImage;
        [SerializeField] private RoundedPanelGraphic penaltyIconCircleGraphic;
        [SerializeField] private Image penaltyIconImage;
        [SerializeField] private TMP_Text penaltyLabel;
        [SerializeField] private TMP_Text penaltyValueLabel;

        public CoopMinigameThemeConfig ThemeConfig => themeConfig;

        private void Awake()
        {
            ApplyTheme();
        }

        private void OnValidate()
        {
            ApplyTheme();
        }

        public void Bind(string instructionTitle, string instructionBody, float remainingSeconds, float totalSeconds, float penaltySeconds)
        {
            ApplyTheme();
            SetInstruction(instructionTitle, instructionBody);
            SetTimer(remainingSeconds, totalSeconds);
            SetPenaltySeconds(penaltySeconds);
        }

        public void SetTheme(CoopMinigameThemeConfig theme)
        {
            themeConfig = theme;
            ApplyTheme();
        }

        public void SetInstruction(string title, string body)
        {
            if (themeConfig == null)
            {
                return;
            }

            if (instructionTitleLabel != null)
            {
                instructionTitleLabel.text = string.IsNullOrWhiteSpace(title)
                    ? themeConfig.BottomPanelStyle.DefaultInstructionTitle
                    : title.Trim();
            }

            if (instructionBodyLabel != null)
            {
                instructionBodyLabel.text = string.IsNullOrWhiteSpace(body)
                    ? themeConfig.BottomPanelStyle.DefaultInstructionBody
                    : body.Trim();
            }
        }

        public void SetTimer(float remainingSeconds, float totalSeconds)
        {
            var safeTotal = Mathf.Max(0.01f, totalSeconds);
            var safeRemaining = Mathf.Clamp(remainingSeconds, 0f, safeTotal);
            var normalizedRemaining = safeRemaining / safeTotal;

            if (timeValueLabel != null)
            {
                timeValueLabel.text = FormatTime(safeRemaining);
            }

            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = 1f;
                timeSlider.value = normalizedRemaining;
            }
        }

        public void SetPenaltySeconds(float penaltySeconds)
        {
            if (penaltyValueLabel == null)
            {
                return;
            }

            var roundedPenalty = Mathf.RoundToInt(Mathf.Abs(penaltySeconds));
            penaltyValueLabel.text = roundedPenalty <= 0 ? "0s" : $"-{roundedPenalty}s";
        }

        public void ApplyTheme()
        {
            if (themeConfig == null)
            {
                return;
            }

            var palette = themeConfig.Palette;
            var typography = themeConfig.Typography;
            var bottomStyle = themeConfig.BottomPanelStyle;
            var panelStyle = themeConfig.CardPanelStyle;
            var iconRadius = bottomStyle.LargeIconSize * 0.5f;

            SetRoundedPanel(instructionPanelGraphic, palette.PanelBackground, palette.PanelBorder, panelStyle.CornerRadius + 8f, panelStyle.BorderWidth);
            SetRoundedPanel(timerPanelGraphic, palette.PanelBackground, palette.PanelBorder, panelStyle.CornerRadius + 8f, panelStyle.BorderWidth);
            SetRoundedPanel(instructionIconCircleGraphic, palette.PanelBackground, palette.PanelBorder, themeConfig.InstructionPanelStyle.IconCircleSize * 0.5f, themeConfig.InstructionPanelStyle.IconCircleBorderWidth);
            SetRoundedPanel(timerIconCircleGraphic, palette.PanelBackground, palette.ProgressGreen, iconRadius, 2f);
            SetRoundedPanel(penaltyIconCircleGraphic, palette.PanelBackground, palette.DangerSoft, iconRadius, 2f);

            SetImageColor(instructionPanelImage, palette.PanelBackground);
            SetImageColor(timerPanelImage, palette.PanelBackground);
            SetImageColor(timeSliderFillImage, palette.ProgressGreen);
            SetImageColor(timeSliderBackgroundImage, palette.Cream);
            SetImageColor(dividerImage, palette.PanelBorder);

            SetImageSprite(instructionIconImage, themeConfig.Icons.TeamIcon);
            SetImageSprite(timerIconImage, themeConfig.Icons.TimerIcon);
            SetImageSprite(penaltyIconImage, themeConfig.Icons.PenaltyTimerIcon);

            ApplyTextStyle(instructionTitleLabel, typography.SectionTitleSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyTextStyle(instructionBodyLabel, typography.BodySize, palette.TextPrimary, FontStyles.Normal);
            ApplyText(timeLabel, bottomStyle.TimeRemainingLabel, typography.CaptionSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(timeValueLabel, "00:00", typography.ProjectTitleSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(penaltyLabel, bottomStyle.PenaltyLabel, typography.CaptionSize, palette.Danger, FontStyles.Bold);
            ApplyText(penaltyValueLabel, "-0s", typography.ProjectTitleSize, palette.Danger, FontStyles.Bold);

            if (timeSlider != null)
            {
                timeSlider.wholeNumbers = false;
                timeSlider.interactable = false;
            }
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

        private void ApplyTextStyle(TMP_Text label, int fontSize, Color color, FontStyles style)
        {
            if (label == null)
            {
                return;
            }

            if (themeConfig.PrimaryFont != null)
            {
                label.font = themeConfig.PrimaryFont;
            }

            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = style;
        }

        private static string FormatTime(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            var minutes = totalSeconds / 60;
            var remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetRoundedPanel(RoundedPanelGraphic graphic, Color fill, Color border, float radius, float width)
        {
            if (graphic != null)
            {
                graphic.Configure(fill, border, radius, width);
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
