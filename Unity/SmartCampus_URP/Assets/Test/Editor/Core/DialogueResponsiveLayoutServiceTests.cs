using NUnit.Framework;
using SmartCampus.Dialogue;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueResponsiveLayoutServiceTests
    {
        private static readonly DialogueResponsiveLayoutSettings Settings = new(
            portraitFrameHeightRatio: 0.32f,
            landscapeFrameHeightRatio: 0.42f,
            minFrameHeight: 260f,
            maxFrameHeight: 520f,
            outerMargins: new Vector2(40f, 32f),
            textPadding: new Vector2(44f, 34f),
            speakerBadgeHeight: 64f,
            minPortraitSize: 120f,
            maxPortraitSize: 340f,
            portraitGap: 12f);

        [TestCase(1080, 1920)]
        [TestCase(1920, 1080)]
        [TestCase(720, 1280)]
        [TestCase(1280, 720)]
        [TestCase(2560, 1080)]
        public void Calculate_CommonResolutions_KeepsVisualsInsideAvailableArea(int width, int height)
        {
            var availableSize = new Vector2(width, height);

            var layout = DialogueResponsiveLayoutService.Calculate(availableSize, Settings);

            Assert.That(layout.FrameRect.height, Is.GreaterThan(0f));
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.FrameRect, availableSize), Is.True);
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.PortraitRect, availableSize), Is.True);
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.SpeakerBadgeRect, availableSize), Is.True);
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.TextRect, availableSize), Is.True);
            AssertRectContains(layout.FrameRect, layout.TextRect);
        }

        [TestCase(1080, 1740)]
        [TestCase(1800, 980)]
        [TestCase(1700, 1020)]
        public void Calculate_NotchedSafeAreaSizes_KeepsContentInsideSafeArea(int width, int height)
        {
            var safeAreaSize = new Vector2(width, height);

            var layout = DialogueResponsiveLayoutService.Calculate(safeAreaSize, Settings);

            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.FrameRect, safeAreaSize), Is.True);
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.PortraitRect, safeAreaSize), Is.True);
            Assert.That(DialogueResponsiveLayoutService.IsInside(layout.SpeakerBadgeRect, safeAreaSize), Is.True);
            AssertRectContains(layout.FrameRect, layout.TextRect);
        }

        [Test]
        public void Calculate_OrientationChange_UsesExpectedProfile()
        {
            var portrait = DialogueResponsiveLayoutService.Calculate(new Vector2(1080f, 1920f), Settings);
            var landscape = DialogueResponsiveLayoutService.Calculate(new Vector2(1920f, 1080f), Settings);

            Assert.That(portrait.IsPortrait, Is.True);
            Assert.That(landscape.IsPortrait, Is.False);
            Assert.That(landscape.PortraitRect.width, Is.LessThan(portrait.PortraitRect.width));
        }

        private static void AssertRectContains(Rect parent, Rect child)
        {
            Assert.That(child.xMin, Is.GreaterThanOrEqualTo(parent.xMin));
            Assert.That(child.yMin, Is.GreaterThanOrEqualTo(parent.yMin));
            Assert.That(child.xMax, Is.LessThanOrEqualTo(parent.xMax));
            Assert.That(child.yMax, Is.LessThanOrEqualTo(parent.yMax));
        }
    }
}
