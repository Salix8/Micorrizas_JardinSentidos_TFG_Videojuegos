using NUnit.Framework;
using SmartCampus.Dialogue;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueFlowResolverTests
    {
        [Test]
        public void TryGetSequenceKey_ConfiguredIntroduction_ReturnsSequence()
        {
            var entries = new List<DialogueFlowMinigameEntry>
            {
                CreateEntry(0, "Garden of Sight", "Garden of Sight Succes"),
                CreateEntry(4, "Garden of Smell", "Garden of Smell Succes")
            };

            var found = DialogueFlowResolver.TryGetSequenceKey(entries, 4, success: false, out var sequenceKey);

            Assert.That(found, Is.True);
            Assert.That(sequenceKey, Is.EqualTo("Garden of Smell"));
        }

        [Test]
        public void TryGetSequenceKey_ConfiguredSuccess_ReturnsSequence()
        {
            var entries = new List<DialogueFlowMinigameEntry>
            {
                CreateEntry(2, "Garden of Touch", "Garden of Touch Succes")
            };

            var found = DialogueFlowResolver.TryGetSequenceKey(entries, 2, success: true, out var sequenceKey);

            Assert.That(found, Is.True);
            Assert.That(sequenceKey, Is.EqualTo("Garden of Touch Succes"));
        }

        [Test]
        public void TryGetSequenceKey_UnconfiguredSixthMinigame_ReturnsFalse()
        {
            var entries = new List<DialogueFlowMinigameEntry>
            {
                CreateEntry(0, "Garden of Sight", "Garden of Sight Succes"),
                CreateEntry(1, "Garden of Sound", "Garden of Sound Succes"),
                CreateEntry(2, "Garden of Touch", "Garden of Touch Succes"),
                CreateEntry(3, "Garden of Taste", "Garden of Taste Succes"),
                CreateEntry(4, "Garden of Smell", "Garden of Smell Succes")
            };

            var found = DialogueFlowResolver.TryGetSequenceKey(entries, 5, success: false, out var sequenceKey);

            Assert.That(found, Is.False);
            Assert.That(sequenceKey, Is.Empty);
        }

        [Test]
        public void DialogueGardenBoundary_ContainsOnlyPointsInsideCollider()
        {
            var boundaryObject = new GameObject("Boundary");
            try
            {
                var boxCollider = boundaryObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(10f, 10f, 10f);
                var boundary = boundaryObject.AddComponent<DialogueGardenBoundary>();

                Assert.That(boundary.Contains(Vector3.zero), Is.True);
                Assert.That(boundary.Contains(new Vector3(20f, 0f, 0f)), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(boundaryObject);
            }
        }

        [Test]
        public void ResolveGpsTimeout_InEditor_UsesEditorFallback()
        {
            var timeout = DialogueOpeningTimingService.ResolveGpsTimeout(
                isEditor: true,
                deviceTimeoutSeconds: 8f,
                editorFallbackSeconds: 1f);

            Assert.That(timeout, Is.EqualTo(1f));
        }

        [Test]
        public void ResolveGpsTimeout_OnDevice_UsesDeviceTimeout()
        {
            var timeout = DialogueOpeningTimingService.ResolveGpsTimeout(
                isEditor: false,
                deviceTimeoutSeconds: 8f,
                editorFallbackSeconds: 1f);

            Assert.That(timeout, Is.EqualTo(8f));
        }

        [Test]
        public void ResolveGpsTimeout_NegativeConfiguration_IsClampedToZero()
        {
            var timeout = DialogueOpeningTimingService.ResolveGpsTimeout(
                isEditor: true,
                deviceTimeoutSeconds: 8f,
                editorFallbackSeconds: -2f);

            Assert.That(timeout, Is.Zero);
        }

        [Test]
        public void OpeningLoadingView_Visible_BlocksInputAndCanBeHidden()
        {
            var overlayObject = new GameObject(
                "OpeningOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(DialogueOpeningLoadingView));
            try
            {
                var view = overlayObject.GetComponent<DialogueOpeningLoadingView>();

                view.Show("Cargando...");

                Assert.That(overlayObject.activeSelf, Is.True);
                Assert.That(overlayObject.GetComponent<Image>().raycastTarget, Is.True);
                Assert.That(overlayObject.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(overlayObject.GetComponent<CanvasGroup>().interactable, Is.True);

                view.Hide();

                Assert.That(overlayObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
            }
        }

        private static DialogueFlowMinigameEntry CreateEntry(int minigameIndex, string introductionKey, string successKey)
        {
            var entry = new DialogueFlowMinigameEntry();
            SetPrivateField(entry, "minigameIndex", minigameIndex);
            SetPrivateField(entry, "introductionSequenceKey", introductionKey);
            SetPrivateField(entry, "successSequenceKey", successKey);
            return entry;
        }

        private static void SetPrivateField<T>(DialogueFlowMinigameEntry entry, string fieldName, T value)
        {
            var field = typeof(DialogueFlowMinigameEntry).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(entry, value);
        }
    }
}
