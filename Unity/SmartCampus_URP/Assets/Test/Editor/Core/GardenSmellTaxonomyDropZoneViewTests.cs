using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.GardenSmellTaxonomy;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class GardenSmellTaxonomyDropZoneViewTests
{
    [Test]
    public void Bind_UsesCategoryIconAndHidesLegacyAccentBadgeLabel()
    {
        var root = new GameObject("DropZone", typeof(RectTransform), typeof(Image), typeof(GardenSmellTaxonomyDropZoneView));
        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        var legacyBadge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        var badgeLabel = new GameObject("BadgeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var iconSprite = CreateSprite();

        accent.transform.SetParent(root.transform, false);
        legacyBadge.transform.SetParent(root.transform, false);
        badgeLabel.transform.SetParent(legacyBadge.transform, false);

        var view = root.GetComponent<GardenSmellTaxonomyDropZoneView>();
        var serializedView = new SerializedObject(view);
        serializedView.FindProperty("zoneTransform").objectReferenceValue = root.GetComponent<RectTransform>();
        serializedView.FindProperty("panelImage").objectReferenceValue = root.GetComponent<Image>();
        serializedView.FindProperty("accentImage").objectReferenceValue = accent.GetComponent<Image>();
        serializedView.FindProperty("categoryIconImage").objectReferenceValue = legacyBadge.GetComponent<Image>();
        serializedView.FindProperty("categoryIconSprite").objectReferenceValue = iconSprite;
        serializedView.FindProperty("badgeLabel").objectReferenceValue = badgeLabel.GetComponent<TMP_Text>();
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        view.Bind(new List<GardenSmellTaxonomyClassificationEntryNetworkState>(), GardenSmellTaxonomyVisualSettings.CreateDefault(), false);

        Assert.That(accent.activeSelf, Is.False);
        Assert.That(badgeLabel.activeSelf, Is.False);
        Assert.That(legacyBadge.activeSelf, Is.True);
        Assert.That(legacyBadge.GetComponent<Image>().sprite, Is.EqualTo(iconSprite));
        Assert.That(legacyBadge.GetComponent<Image>().preserveAspect, Is.True);

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(iconSprite.texture);
        Object.DestroyImmediate(iconSprite);
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(2, 2)
        {
            name = "GardenSmellTaxonomyDropZoneIconTestTexture"
        };
        texture.SetPixel(0, 0, Color.white);
        texture.SetPixel(1, 0, Color.white);
        texture.SetPixel(0, 1, Color.white);
        texture.SetPixel(1, 1, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
    }
}
