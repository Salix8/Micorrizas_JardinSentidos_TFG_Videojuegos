using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEngine;
using UnityEngine.UI;

public sealed class CollaborativePlantGuessMinigameUiControllerTests
{
    [Test]
    public void EnsureSuggestionScrollHierarchy_KeepsReservedPanelAndCreatesScrollableContent()
    {
        var panel = new GameObject(
            "SuggestionPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement),
            typeof(VerticalLayoutGroup));
        var controllerObject = new GameObject("Controller", typeof(CollaborativePlantGuessMinigameUIController));
        var template = new GameObject(
            "SuggestionTemplate",
            typeof(RectTransform),
            typeof(CollaborativePlantGuessSuggestionEntryView));
        template.transform.SetParent(panel.transform, false);

        var layoutElement = panel.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 150f;

        var controller = controllerObject.GetComponent<CollaborativePlantGuessMinigameUIController>();
        SetField(controller, "suggestionRoot", panel.transform);
        SetField(controller, "suggestionTemplate", template.GetComponent<CollaborativePlantGuessSuggestionEntryView>());

        Invoke(controller, "EnsureSuggestionScrollHierarchy");

        var scrollRect = panel.GetComponent<ScrollRect>();
        Assert.That(scrollRect, Is.Not.Null);
        Assert.That(scrollRect.vertical, Is.True);
        Assert.That(scrollRect.horizontal, Is.False);
        Assert.That(scrollRect.viewport, Is.Not.Null);
        Assert.That(scrollRect.content, Is.Not.Null);
        Assert.That(scrollRect.content.name, Is.EqualTo("SuggestionContent"));
        Assert.That(template.transform.parent, Is.EqualTo(scrollRect.content));
        Assert.That(panel.GetComponent<VerticalLayoutGroup>().enabled, Is.False);
        Assert.That(layoutElement.preferredHeight, Is.EqualTo(150f));

        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void HideSuggestionRows_DoesNotDisableReservedSuggestionPanel()
    {
        var panel = new GameObject("SuggestionPanel", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var controllerObject = new GameObject("Controller", typeof(CollaborativePlantGuessMinigameUIController));
        var template = new GameObject(
            "SuggestionTemplate",
            typeof(RectTransform),
            typeof(CollaborativePlantGuessSuggestionEntryView));
        template.transform.SetParent(panel.transform, false);

        var controller = controllerObject.GetComponent<CollaborativePlantGuessMinigameUIController>();
        SetField(controller, "suggestionRoot", panel.transform);
        SetField(controller, "suggestionTemplate", template.GetComponent<CollaborativePlantGuessSuggestionEntryView>());

        Invoke(controller, "EnsureSuggestionScrollHierarchy");
        panel.SetActive(true);

        Invoke(controller, "HideSuggestionRows");

        Assert.That(panel.activeSelf, Is.True);

        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(controllerObject);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(target, value);
    }

    private static void Invoke(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(target, null);
    }
}
