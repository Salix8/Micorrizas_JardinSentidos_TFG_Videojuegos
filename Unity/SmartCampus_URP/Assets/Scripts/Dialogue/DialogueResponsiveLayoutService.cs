using UnityEngine;

namespace SmartCampus.Dialogue
{
    public readonly struct DialogueResponsiveLayoutSettings
    {
        public DialogueResponsiveLayoutSettings(
            float portraitFrameHeightRatio,
            float landscapeFrameHeightRatio,
            float minFrameHeight,
            float maxFrameHeight,
            Vector2 outerMargins,
            Vector2 textPadding,
            float speakerBadgeHeight,
            float minPortraitSize,
            float maxPortraitSize,
            float portraitGap)
        {
            PortraitFrameHeightRatio = portraitFrameHeightRatio;
            LandscapeFrameHeightRatio = landscapeFrameHeightRatio;
            MinFrameHeight = minFrameHeight;
            MaxFrameHeight = maxFrameHeight;
            OuterMargins = outerMargins;
            TextPadding = textPadding;
            SpeakerBadgeHeight = speakerBadgeHeight;
            MinPortraitSize = minPortraitSize;
            MaxPortraitSize = maxPortraitSize;
            PortraitGap = portraitGap;
        }

        public float PortraitFrameHeightRatio { get; }
        public float LandscapeFrameHeightRatio { get; }
        public float MinFrameHeight { get; }
        public float MaxFrameHeight { get; }
        public Vector2 OuterMargins { get; }
        public Vector2 TextPadding { get; }
        public float SpeakerBadgeHeight { get; }
        public float MinPortraitSize { get; }
        public float MaxPortraitSize { get; }
        public float PortraitGap { get; }
    }

    public readonly struct DialogueResponsiveLayout
    {
        public DialogueResponsiveLayout(
            bool isPortrait,
            Rect frameRect,
            Rect portraitRect,
            Rect speakerBadgeRect,
            Rect textRect)
        {
            IsPortrait = isPortrait;
            FrameRect = frameRect;
            PortraitRect = portraitRect;
            SpeakerBadgeRect = speakerBadgeRect;
            TextRect = textRect;
        }

        public bool IsPortrait { get; }
        public Rect FrameRect { get; }
        public Rect PortraitRect { get; }
        public Rect SpeakerBadgeRect { get; }
        public Rect TextRect { get; }
    }

    public static class DialogueResponsiveLayoutService
    {
        public static DialogueResponsiveLayout Calculate(
            Vector2 availableSize,
            DialogueResponsiveLayoutSettings settings)
        {
            var width = Mathf.Max(1f, availableSize.x);
            var height = Mathf.Max(1f, availableSize.y);
            var isPortrait = height >= width;
            var horizontalMargin = Mathf.Clamp(settings.OuterMargins.x, 0f, width * 0.2f);
            var verticalMargin = Mathf.Clamp(settings.OuterMargins.y, 0f, height * 0.2f);
            var usableWidth = Mathf.Max(1f, width - horizontalMargin * 2f);
            var usableHeight = Mathf.Max(1f, height - verticalMargin * 2f);

            var frameRatio = isPortrait
                ? settings.PortraitFrameHeightRatio
                : settings.LandscapeFrameHeightRatio;
            var requestedFrameHeight = usableHeight * Mathf.Clamp(frameRatio, 0.1f, 0.8f);
            var frameHeight = Mathf.Clamp(
                requestedFrameHeight,
                Mathf.Min(settings.MinFrameHeight, usableHeight),
                Mathf.Min(settings.MaxFrameHeight, usableHeight));

            var frameRect = new Rect(horizontalMargin, verticalMargin, usableWidth, frameHeight);
            var availablePortraitHeight = Mathf.Max(
                1f,
                height - frameRect.yMax - verticalMargin - settings.PortraitGap);
            var portraitWidthLimit = usableWidth * (isPortrait ? 0.42f : 0.24f);
            var requestedPortraitSize = Mathf.Min(portraitWidthLimit, availablePortraitHeight);
            var profileMaxPortraitSize = isPortrait
                ? settings.MaxPortraitSize
                : settings.MaxPortraitSize * 0.72f;
            var portraitSize = Mathf.Clamp(
                requestedPortraitSize,
                Mathf.Min(settings.MinPortraitSize, requestedPortraitSize),
                Mathf.Min(profileMaxPortraitSize, requestedPortraitSize));
            var portraitRect = new Rect(
                frameRect.x + Mathf.Min(24f, usableWidth * 0.03f),
                frameRect.yMax + settings.PortraitGap,
                portraitSize,
                portraitSize);

            var badgeWidth = Mathf.Clamp(
                isPortrait ? usableWidth * 0.42f : usableWidth * 0.28f,
                Mathf.Min(220f, usableWidth),
                Mathf.Min(360f, usableWidth));
            var badgeHeight = Mathf.Min(settings.SpeakerBadgeHeight, frameHeight * 0.3f);
            var speakerBadgeRect = new Rect(
                frameRect.x + Mathf.Min(32f, usableWidth * 0.04f),
                frameRect.yMax - badgeHeight * 0.5f,
                badgeWidth,
                badgeHeight);

            var textLeft = frameRect.x + settings.TextPadding.x;
            var textBottom = frameRect.y + settings.TextPadding.y;
            var textRight = frameRect.xMax - settings.TextPadding.x;
            var textTop = speakerBadgeRect.yMin - Mathf.Max(12f, settings.TextPadding.y * 0.4f);
            if (textTop <= textBottom)
            {
                textTop = frameRect.yMax - settings.TextPadding.y;
            }

            var textRect = Rect.MinMaxRect(
                textLeft,
                textBottom,
                Mathf.Max(textLeft, textRight),
                Mathf.Max(textBottom, textTop));
            return new DialogueResponsiveLayout(
                isPortrait,
                frameRect,
                portraitRect,
                speakerBadgeRect,
                textRect);
        }

        public static bool IsInside(Rect child, Vector2 parentSize, float tolerance = 0.01f)
        {
            return child.xMin >= -tolerance &&
                   child.yMin >= -tolerance &&
                   child.xMax <= parentSize.x + tolerance &&
                   child.yMax <= parentSize.y + tolerance;
        }
    }
}
