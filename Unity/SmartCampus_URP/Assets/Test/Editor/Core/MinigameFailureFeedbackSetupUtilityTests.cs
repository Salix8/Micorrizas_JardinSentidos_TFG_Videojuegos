using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

public sealed class MinigameFailureFeedbackSetupUtilityTests
{
    [Test]
    public void CreateOrUpdateFailureFeedback_CreatesHierarchyAndAssignsReferences()
    {
        var uiRoot = new GameObject("UiRoot", typeof(RectTransform));
        var gameplayPanel = new GameObject("GameplayPanel", typeof(RectTransform), typeof(Image));
        gameplayPanel.transform.SetParent(uiRoot.transform, false);

        try
        {
            var controller = MinigameFailureFeedbackSetupUtility.CreateOrUpdateFailureFeedback(uiRoot, gameplayPanel);

            Assert.That(controller, Is.Not.Null);
            Assert.That(uiRoot.transform.Find("FailureFeedback"), Is.Not.Null);
            Assert.That(uiRoot.transform.Find("FailureFeedback/FailureFlashOverlay"), Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            var sharedConfig = serializedController.FindProperty("sharedConfig").objectReferenceValue as MinigameFailureFeedbackConfig;
            var shakeTarget = serializedController.FindProperty("shakeTarget").objectReferenceValue as RectTransform;
            var flashOverlay = serializedController.FindProperty("flashOverlay").objectReferenceValue as Image;
            var feedbackAudioSource = serializedController.FindProperty("feedbackAudioSource").objectReferenceValue as AudioSource;

            Assert.That(sharedConfig, Is.Not.Null);
            Assert.That(shakeTarget, Is.EqualTo(gameplayPanel.GetComponent<RectTransform>()));
            Assert.That(flashOverlay, Is.Not.Null);
            Assert.That(feedbackAudioSource, Is.Not.Null);
            Assert.That(flashOverlay.gameObject.name, Is.EqualTo("FailureFlashOverlay"));
            Assert.That(flashOverlay.gameObject.activeSelf, Is.False);
            Assert.That(flashOverlay.color.a, Is.EqualTo(0f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(uiRoot);
        }
    }
}
