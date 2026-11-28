using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using UnityEngine;

public sealed class LocalZoomControlsViewTests
{
    private GameObject owner;
    private LocalZoomControlsView controls;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("ZoomTestOwner", typeof(RectTransform), typeof(Canvas));
    }

    [TearDown]
    public void TearDown()
    {
        if (controls != null) Object.DestroyImmediate(controls.gameObject);
        if (owner != null) Object.DestroyImmediate(owner);
    }

    [Test]
    public void Create_UsesIndependentOverlayCanvas()
    {
        controls = LocalZoomControlsView.Create(owner.transform, null, null, null);
        var canvas = controls.GetComponent<Canvas>();
        Assert.That(controls.transform.parent, Is.Null);
        Assert.That(canvas.isRootCanvas, Is.True);
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
    }

}
