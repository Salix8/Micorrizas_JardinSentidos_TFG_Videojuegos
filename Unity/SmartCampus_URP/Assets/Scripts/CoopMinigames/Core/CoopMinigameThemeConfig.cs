using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [CreateAssetMenu(
        fileName = "CoopMinigameThemeConfig",
        menuName = "SmartCampus/Coop/Minigame Theme Config")]
    public sealed class CoopMinigameThemeConfig : ScriptableObject
    {
        [Header("Brand")]
        [SerializeField] private string projectTitle = "JARDIN MICORRIZAL";
        [SerializeField] private string projectSubtitle = "Restauramos juntos\nla red de micorrizas";
        [SerializeField] private string globalProgressLabel = "RED DE \nMICORRIZAS";
        [SerializeField] private string teamTitle = "EQUIPO";
        [SerializeField] private string unnamedTeamRoomCodeFormat = "SALA {0}";
        [SerializeField] private string minigameTitlePrefix = "MISIÓN:";
        [SerializeField] private string globalProgressPercentFormat = "{0:0}%";
        [SerializeField] [Min(0f)] private float defaultPenaltySeconds = 10f;
        [SerializeField] private Sprite logoSprite;
        [SerializeField] private List<Sprite> defaultAvatarSprites = new();

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private CoopMinigameTypographySettings typography = CoopMinigameTypographySettings.CreateDefault();

        [Header("Palette")]
        [SerializeField] private CoopMinigamePalette palette = CoopMinigamePalette.CreateDefault();

        [Header("Layout")]
        [SerializeField] private CoopMinigameScreenLayoutSettings screenLayout = CoopMinigameScreenLayoutSettings.CreateDefault();
        [SerializeField] private CoopMinigamePanelStyle cardPanelStyle = CoopMinigamePanelStyle.CreateDefault();

        [Header("Panels")]
        [SerializeField] private CoopMinigameTopPanelStyle topPanelStyle = CoopMinigameTopPanelStyle.CreateDefault();
        [SerializeField] private CoopMinigameBottomPanelStyle bottomPanelStyle = CoopMinigameBottomPanelStyle.CreateDefault();
        [SerializeField] private CoopMinigameInstructionPanelStyle instructionPanelStyle = CoopMinigameInstructionPanelStyle.CreateDefault();

        [Header("Icons")]
        [SerializeField] private CoopMinigameIconSet icons = CoopMinigameIconSet.CreateDefault();

        [Header("Decorative Background")]
        [SerializeField] private CoopMinigameDecorationStyle decorationStyle = CoopMinigameDecorationStyle.CreateDefault();

        public string ProjectTitle => projectTitle;
        public string ProjectSubtitle => projectSubtitle;
        public string GlobalProgressLabel => globalProgressLabel;
        public string TeamTitle => teamTitle;
        public string MinigameTitlePrefix => minigameTitlePrefix;
        public string GlobalProgressPercentFormat => globalProgressPercentFormat;
        public float DefaultPenaltySeconds => defaultPenaltySeconds;
        public Sprite LogoSprite => logoSprite;
        public IReadOnlyList<Sprite> DefaultAvatarSprites => defaultAvatarSprites;
        public TMP_FontAsset PrimaryFont => primaryFont;
        public CoopMinigameTypographySettings Typography => typography;
        public CoopMinigamePalette Palette => palette;
        public CoopMinigameScreenLayoutSettings ScreenLayout => screenLayout;
        public CoopMinigamePanelStyle CardPanelStyle => cardPanelStyle;
        public CoopMinigameTopPanelStyle TopPanelStyle => topPanelStyle;
        public CoopMinigameBottomPanelStyle BottomPanelStyle => bottomPanelStyle;
        public CoopMinigameInstructionPanelStyle InstructionPanelStyle => instructionPanelStyle;
        public CoopMinigameIconSet Icons => icons;
        public CoopMinigameDecorationStyle DecorationStyle => decorationStyle;

        public string ResolveTeamDisplayName(string teamName, string roomCode)
        {
            if (!string.IsNullOrWhiteSpace(teamName))
            {
                return teamName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(roomCode))
            {
                return string.Format(unnamedTeamRoomCodeFormat, roomCode.Trim());
            }

            return teamTitle;
        }

        private void OnValidate()
        {
            projectTitle = NormalizeRequired(projectTitle, "JARDIN MICORRIZAL");
            projectSubtitle = NormalizeRequired(projectSubtitle, "Restauramos juntos\nla red de micorrizas");
            globalProgressLabel = NormalizeRequired(globalProgressLabel, "RED DE \nMICORRIZAS");
            teamTitle = NormalizeRequired(teamTitle, "EQUIPO");
            unnamedTeamRoomCodeFormat = string.IsNullOrWhiteSpace(unnamedTeamRoomCodeFormat)
                ? "SALA {0}"
                : unnamedTeamRoomCodeFormat.Trim();
            minigameTitlePrefix = NormalizeRequired(minigameTitlePrefix, "MISIÓN:");
            globalProgressPercentFormat = NormalizeRequired(globalProgressPercentFormat, "{0:0}%");
            defaultPenaltySeconds = Mathf.Max(0f, defaultPenaltySeconds);

            defaultAvatarSprites ??= new List<Sprite>();
            typography.Clamp();
            palette.Clamp();
            screenLayout.Clamp();
            cardPanelStyle.Clamp();
            topPanelStyle.Clamp();
            bottomPanelStyle.Clamp();
            instructionPanelStyle.Clamp();
            icons.Clamp();
            decorationStyle.Clamp();
        }

        private static string NormalizeRequired(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    [Serializable]
    public struct CoopMinigameTypographySettings
    {
        [SerializeField] [Min(8)] private int captionSize;
        [SerializeField] [Min(10)] private int bodySize;
        [SerializeField] [Min(12)] private int bodyLargeSize;
        [SerializeField] [Min(14)] private int sectionTitleSize;
        [SerializeField] [Min(16)] private int minigameTitleSize;
        [SerializeField] [Min(16)] private int projectTitleSize;
        [SerializeField] [Range(0f, 1f)] private float headingBoldWeight;

        public int CaptionSize => captionSize;
        public int BodySize => bodySize;
        public int BodyLargeSize => bodyLargeSize;
        public int SectionTitleSize => sectionTitleSize;
        public int MinigameTitleSize => minigameTitleSize;
        public int ProjectTitleSize => projectTitleSize;
        public float HeadingBoldWeight => headingBoldWeight;

        public static CoopMinigameTypographySettings CreateDefault()
        {
            return new CoopMinigameTypographySettings
            {
                captionSize = 24,
                bodySize = 30,
                bodyLargeSize = 36,
                sectionTitleSize = 42,
                minigameTitleSize = 48,
                projectTitleSize = 46,
                headingBoldWeight = 0.72f
            };
        }

        public void Clamp()
        {
            captionSize = Mathf.Max(8, captionSize);
            bodySize = Mathf.Max(10, bodySize);
            bodyLargeSize = Mathf.Max(bodySize, bodyLargeSize);
            sectionTitleSize = Mathf.Max(bodyLargeSize, sectionTitleSize);
            minigameTitleSize = Mathf.Max(sectionTitleSize, minigameTitleSize);
            projectTitleSize = Mathf.Max(sectionTitleSize, projectTitleSize);
            headingBoldWeight = Mathf.Clamp01(headingBoldWeight);
        }
    }

    [Serializable]
    public struct CoopMinigamePalette
    {
        [SerializeField] private Color screenBackground;
        [SerializeField] private Color panelBackground;
        [SerializeField] private Color panelBorder;
        [SerializeField] private Color primaryGreen;
        [SerializeField] private Color secondaryGreen;
        [SerializeField] private Color progressGreen;
        [SerializeField] private Color mutedGreen;
        [SerializeField] private Color cream;
        [SerializeField] private Color textPrimary;
        [SerializeField] private Color textMuted;
        [SerializeField] private Color success;
        [SerializeField] private Color warning;
        [SerializeField] private Color danger;
        [SerializeField] private Color dangerSoft;

        public Color ScreenBackground => screenBackground;
        public Color PanelBackground => panelBackground;
        public Color PanelBorder => panelBorder;
        public Color PrimaryGreen => primaryGreen;
        public Color SecondaryGreen => secondaryGreen;
        public Color ProgressGreen => progressGreen;
        public Color MutedGreen => mutedGreen;
        public Color Cream => cream;
        public Color TextPrimary => textPrimary;
        public Color TextMuted => textMuted;
        public Color Success => success;
        public Color Warning => warning;
        public Color Danger => danger;
        public Color DangerSoft => dangerSoft;

        public static CoopMinigamePalette CreateDefault()
        {
            return new CoopMinigamePalette
            {
                screenBackground = new Color(0.982f, 0.965f, 0.925f, 1f),
                panelBackground = new Color(0.996f, 0.988f, 0.956f, 0.94f),
                panelBorder = new Color(0.84f, 0.79f, 0.63f, 1f),
                primaryGreen = new Color(0.075f, 0.29f, 0.095f, 1f),
                secondaryGreen = new Color(0.23f, 0.46f, 0.16f, 1f),
                progressGreen = new Color(0.38f, 0.63f, 0.22f, 1f),
                mutedGreen = new Color(0.68f, 0.78f, 0.49f, 1f),
                cream = new Color(0.93f, 0.90f, 0.78f, 1f),
                textPrimary = new Color(0.08f, 0.11f, 0.09f, 1f),
                textMuted = new Color(0.33f, 0.38f, 0.32f, 1f),
                success = new Color(0.28f, 0.55f, 0.18f, 1f),
                warning = new Color(0.82f, 0.55f, 0.18f, 1f),
                danger = new Color(0.78f, 0.12f, 0.10f, 1f),
                dangerSoft = new Color(0.95f, 0.78f, 0.72f, 1f)
            };
        }

        public void Clamp()
        {
            ForceOpaque(ref screenBackground);
            panelBackground.a = Mathf.Clamp(panelBackground.a, 0.1f, 1f);
            ForceOpaque(ref panelBorder);
            ForceOpaque(ref primaryGreen);
            ForceOpaque(ref secondaryGreen);
            ForceOpaque(ref progressGreen);
            ForceOpaque(ref mutedGreen);
            ForceOpaque(ref cream);
            ForceOpaque(ref textPrimary);
            ForceOpaque(ref textMuted);
            ForceOpaque(ref success);
            ForceOpaque(ref warning);
            ForceOpaque(ref danger);
            ForceOpaque(ref dangerSoft);
        }

        private static void ForceOpaque(ref Color color)
        {
            color.a = 1f;
        }
    }

    [Serializable]
    public struct CoopMinigameScreenLayoutSettings
    {
        [SerializeField] private Vector2 referenceResolution;
        [SerializeField] private Vector2 screenMargin;
        [SerializeField] [Min(80f)] private float topPanelHeight;
        [SerializeField] [Min(80f)] private float bottomPanelHeight;
        [SerializeField] [Min(4f)] private float verticalSpacing;
        [SerializeField] private bool useSafeArea;

        public Vector2 ReferenceResolution => referenceResolution;
        public Vector2 ScreenMargin => screenMargin;
        public float TopPanelHeight => topPanelHeight;
        public float BottomPanelHeight => bottomPanelHeight;
        public float VerticalSpacing => verticalSpacing;
        public bool UseSafeArea => useSafeArea;

        public static CoopMinigameScreenLayoutSettings CreateDefault()
        {
            return new CoopMinigameScreenLayoutSettings
            {
                referenceResolution = new Vector2(1080f, 1920f),
                screenMargin = new Vector2(32f, 28f),
                topPanelHeight = 318f,
                bottomPanelHeight = 278f,
                verticalSpacing = 22f,
                useSafeArea = true
            };
        }

        public void Clamp()
        {
            referenceResolution.x = Mathf.Max(320f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(480f, referenceResolution.y);
            screenMargin.x = Mathf.Max(0f, screenMargin.x);
            screenMargin.y = Mathf.Max(0f, screenMargin.y);
            topPanelHeight = Mathf.Max(80f, topPanelHeight);
            bottomPanelHeight = Mathf.Max(80f, bottomPanelHeight);
            verticalSpacing = Mathf.Max(4f, verticalSpacing);
        }
    }

    [Serializable]
    public struct CoopMinigamePanelStyle
    {
        [SerializeField] [Min(0f)] private float cornerRadius;
        [SerializeField] [Min(0f)] private float borderWidth;
        [SerializeField] private Vector2 shadowOffset;
        [SerializeField] [Range(0f, 1f)] private float shadowAlpha;
        [SerializeField] private Vector2 padding;

        public float CornerRadius => cornerRadius;
        public float BorderWidth => borderWidth;
        public Vector2 ShadowOffset => shadowOffset;
        public float ShadowAlpha => shadowAlpha;
        public Vector2 Padding => padding;

        public static CoopMinigamePanelStyle CreateDefault()
        {
            return new CoopMinigamePanelStyle
            {
                cornerRadius = 32f,
                borderWidth = 2f,
                shadowOffset = new Vector2(0f, -7f),
                shadowAlpha = 0.16f,
                padding = new Vector2(34f, 28f)
            };
        }

        public void Clamp()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            borderWidth = Mathf.Max(0f, borderWidth);
            shadowAlpha = Mathf.Clamp01(shadowAlpha);
            padding.x = Mathf.Max(0f, padding.x);
            padding.y = Mathf.Max(0f, padding.y);
        }
    }

    [Serializable]
    public struct CoopMinigameTopPanelStyle
    {
        [SerializeField] [Min(80f)] private float upperHeight;
        [SerializeField] [Min(40f)] private float titleBandHeight;
        [SerializeField] [Min(32f)] private float logoSize;
        [SerializeField] [Min(0f)] private float logoInnerPadding;
        [SerializeField] [Min(24f)] private float avatarSize;
        [SerializeField] [Min(0f)] private float avatarOverlap;
        [SerializeField] [Min(40f)] private float progressBarWidth;
        [SerializeField] [Min(4f)] private float progressBarHeight;

        public float UpperHeight => upperHeight;
        public float TitleBandHeight => titleBandHeight;
        public float LogoSize => logoSize;
        public float LogoInnerPadding => logoInnerPadding;
        public float AvatarSize => avatarSize;
        public float AvatarOverlap => avatarOverlap;
        public float ProgressBarWidth => progressBarWidth;
        public float ProgressBarHeight => progressBarHeight;

        public static CoopMinigameTopPanelStyle CreateDefault()
        {
            return new CoopMinigameTopPanelStyle
            {
                upperHeight = 222f,
                titleBandHeight = 96f,
                logoSize = 126f,
                logoInnerPadding = 10f,
                avatarSize = 62f,
                avatarOverlap = 8f,
                progressBarWidth = 210f,
                progressBarHeight = 22f
            };
        }

        public void Clamp()
        {
            upperHeight = Mathf.Max(80f, upperHeight);
            titleBandHeight = Mathf.Max(40f, titleBandHeight);
            logoSize = Mathf.Max(32f, logoSize);
            logoInnerPadding = Mathf.Clamp(logoInnerPadding, 0f, logoSize - 8f);
            avatarSize = Mathf.Max(24f, avatarSize);
            avatarOverlap = Mathf.Max(0f, avatarOverlap);
            progressBarWidth = Mathf.Max(40f, progressBarWidth);
            progressBarHeight = Mathf.Max(4f, progressBarHeight);
        }
    }

    [Serializable]
    public struct CoopMinigameBottomPanelStyle
    {
        [SerializeField] [Min(48f)] private float instructionHeight;
        [SerializeField] [Min(64f)] private float timerPanelHeight;
        [SerializeField] [Min(32f)] private float largeIconSize;
        [SerializeField] [Min(8f)] private float timerBarHeight;
        [SerializeField] [Min(40f)] private float timerBarWidth;
        [SerializeField] [Min(0.5f)] private float dividerWidth;
        [SerializeField] private string defaultInstructionTitle;
        [SerializeField] [TextArea(2, 3)] private string defaultInstructionBody;
        [SerializeField] private string timeRemainingLabel;
        [SerializeField] private string penaltyLabel;

        public float InstructionHeight => instructionHeight;
        public float TimerPanelHeight => timerPanelHeight;
        public float LargeIconSize => largeIconSize;
        public float TimerBarHeight => timerBarHeight;
        public float TimerBarWidth => timerBarWidth;
        public float DividerWidth => dividerWidth;
        public string DefaultInstructionTitle => defaultInstructionTitle;
        public string DefaultInstructionBody => defaultInstructionBody;
        public string TimeRemainingLabel => timeRemainingLabel;
        public string PenaltyLabel => penaltyLabel;

        public static CoopMinigameBottomPanelStyle CreateDefault()
        {
            return new CoopMinigameBottomPanelStyle
            {
                instructionHeight = 118f,
                timerPanelHeight = 150f,
                largeIconSize = 86f,
                timerBarHeight = 18f,
                timerBarWidth = 310f,
                dividerWidth = 1f,
                defaultInstructionTitle = "DE ACUERDO EN EQUIPO",
                defaultInstructionBody = "Cuando todos hayais terminado, se contara el resultado.",
                timeRemainingLabel = "TIEMPO RESTANTE",
                penaltyLabel = "PENALIZACION"
            };
        }

        public void Clamp()
        {
            instructionHeight = Mathf.Max(48f, instructionHeight);
            timerPanelHeight = Mathf.Max(64f, timerPanelHeight);
            largeIconSize = Mathf.Max(32f, largeIconSize);
            timerBarHeight = Mathf.Max(8f, timerBarHeight);
            timerBarWidth = Mathf.Max(40f, timerBarWidth);
            dividerWidth = Mathf.Max(0.5f, dividerWidth);
            defaultInstructionTitle = Normalize(defaultInstructionTitle, "DE ACUERDO EN EQUIPO");
            defaultInstructionBody = Normalize(defaultInstructionBody, "Cuando todos hayais terminado, se contara el resultado.");
            timeRemainingLabel = Normalize(timeRemainingLabel, "TIEMPO RESTANTE");
            penaltyLabel = Normalize(penaltyLabel, "PENALIZACION");
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    [Serializable]
    public struct CoopMinigameInstructionPanelStyle
    {
        [SerializeField] [Min(32f)] private float iconCircleSize;
        [SerializeField] [Min(0f)] private float iconCircleBorderWidth;
        [SerializeField] [Min(80f)] private float preferredHeight;

        public float IconCircleSize => iconCircleSize;
        public float IconCircleBorderWidth => iconCircleBorderWidth;
        public float PreferredHeight => preferredHeight;

        public static CoopMinigameInstructionPanelStyle CreateDefault()
        {
            return new CoopMinigameInstructionPanelStyle
            {
                iconCircleSize = 86f,
                iconCircleBorderWidth = 2f,
                preferredHeight = 156f
            };
        }

        public void Clamp()
        {
            iconCircleSize = Mathf.Max(32f, iconCircleSize);
            iconCircleBorderWidth = Mathf.Max(0f, iconCircleBorderWidth);
            preferredHeight = Mathf.Max(80f, preferredHeight);
        }
    }

    [Serializable]
    public struct CoopMinigameIconSet
    {
        [SerializeField] private Sprite teamIcon;
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite chatIcon;
        [SerializeField] private Sprite leafIcon;
        [SerializeField] private Sprite timerIcon;
        [SerializeField] private Sprite penaltyTimerIcon;
        [SerializeField] private Sprite successIcon;
        [SerializeField] private Sprite failureIcon;

        public Sprite TeamIcon => teamIcon;
        public Sprite InfoIcon => infoIcon;
        public Sprite ChatIcon => chatIcon;
        public Sprite LeafIcon => leafIcon;
        public Sprite TimerIcon => timerIcon;
        public Sprite PenaltyTimerIcon => penaltyTimerIcon;
        public Sprite SuccessIcon => successIcon;
        public Sprite FailureIcon => failureIcon;

        public static CoopMinigameIconSet CreateDefault()
        {
            return new CoopMinigameIconSet();
        }

        public void Clamp()
        {
        }
    }

    [Serializable]
    public struct CoopMinigameDecorationStyle
    {
        [SerializeField] private Sprite leftPlantSprite;
        [SerializeField] private Sprite rightPlantSprite;
        [SerializeField] private Sprite mushroomSprite;
        [SerializeField] [Range(0f, 1f)] private float opacity;
        [SerializeField] [Min(0f)] private float bottomHillHeight;

        public Sprite LeftPlantSprite => leftPlantSprite;
        public Sprite RightPlantSprite => rightPlantSprite;
        public Sprite MushroomSprite => mushroomSprite;
        public float Opacity => opacity;
        public float BottomHillHeight => bottomHillHeight;

        public static CoopMinigameDecorationStyle CreateDefault()
        {
            return new CoopMinigameDecorationStyle
            {
                opacity = 0.72f,
                bottomHillHeight = 210f
            };
        }

        public void Clamp()
        {
            opacity = Mathf.Clamp01(opacity);
            bottomHillHeight = Mathf.Max(0f, bottomHillHeight);
        }
    }
}
