using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using UnityEngine;

public sealed class LocalZoomServiceTests
{
    [TestCase(0.5f, 2f, 1f)]
    [TestCase(1.5f, 2f, 1.5f)]
    [TestCase(3f, 2f, 2f)]
    [TestCase(3f, 0.5f, 1f)]
    public void ClampZoom_RespectsOriginalViewAndMaximum(float value, float maximum, float expected)
    {
        Assert.That(LocalZoomService.ClampZoom(value, maximum), Is.EqualTo(expected));
    }

    [Test]
    public void ClampPosition_KeepsZoomedContentCoveringViewport()
    {
        var actual = LocalZoomService.ClampPosition(new Vector2(200f, -200f), new Vector2(100f, 80f), 2f);
        Assert.That(actual, Is.EqualTo(new Vector2(50f, -40f)));
    }

    [Test]
    public void ZoomAroundFocus_KeepsPointUnderFingersFixed()
    {
        // Ampliar x2 con los dedos en (100, 50): ese punto del contenido sigue bajo los dedos.
        var position = LocalZoomService.ZoomAroundFocus(Vector2.zero, 1f, 2f, new Vector2(100f, 50f), new Vector2(100f, 50f));
        Assert.That(position, Is.EqualTo(new Vector2(-100f, -50f)));
    }

    [Test]
    public void ZoomAroundFocus_WithSameZoomPansByFingerMovement()
    {
        var position = LocalZoomService.ZoomAroundFocus(new Vector2(10f, 0f), 2f, 2f, new Vector2(0f, 0f), new Vector2(30f, -20f));
        Assert.That(position, Is.EqualTo(new Vector2(40f, -20f)));
    }

    [Test]
    public void ClampPosition_AtOriginalSizeReturnsToCenter()
    {
        Assert.That(LocalZoomService.ClampPosition(new Vector2(20f, -30f), new Vector2(100f, 80f), 1f), Is.EqualTo(Vector2.zero));
    }
}
