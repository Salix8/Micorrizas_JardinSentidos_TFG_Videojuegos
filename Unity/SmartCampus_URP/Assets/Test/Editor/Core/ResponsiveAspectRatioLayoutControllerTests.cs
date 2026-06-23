using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class ResponsiveAspectRatioLayoutControllerTests
    {
        private GameObject root;
        private RectTransform rootRectTransform;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ResponsiveAspectRoot", typeof(RectTransform));
            rootRectTransform = root.GetComponent<RectTransform>();
            rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 900f);
            rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1200f);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshLayout_WithLargeReference_ClampsToConfiguredMaximumSize()
        {
            var card = CreateCard(root.transform);
            var controller = card.GetComponent<ResponsiveAspectRatioLayoutController>();

            controller.Configure(
                rootRectTransform,
                0.68f,
                new Vector2(220f, 340f),
                new Vector2(560f, 780f),
                new Vector2(48f, 72f));

            var cardRectTransform = card.GetComponent<RectTransform>();
            Assert.That(cardRectTransform.rect.width, Is.LessThanOrEqualTo(560.1f));
            Assert.That(cardRectTransform.rect.height, Is.LessThanOrEqualTo(780.1f));
        }

        [Test]
        public void RefreshLayout_WithZeroSizedReference_UsesNearestValidAncestor()
        {
            var zeroSizedReference = new GameObject("ZeroSizedReference", typeof(RectTransform));
            zeroSizedReference.transform.SetParent(root.transform, false);
            var zeroSizedReferenceRectTransform = zeroSizedReference.GetComponent<RectTransform>();
            zeroSizedReferenceRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            zeroSizedReferenceRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

            var card = CreateCard(zeroSizedReference.transform);
            var controller = card.GetComponent<ResponsiveAspectRatioLayoutController>();
            var cardRectTransform = card.GetComponent<RectTransform>();
            cardRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 680f);
            cardRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 980f);

            controller.Configure(
                zeroSizedReferenceRectTransform,
                0.68f,
                new Vector2(220f, 340f),
                new Vector2(560f, 780f),
                new Vector2(48f, 72f));

            Assert.That(cardRectTransform.rect.width, Is.EqualTo(560f).Within(0.1f));
            Assert.That(cardRectTransform.rect.height, Is.EqualTo(780f).Within(0.1f));
        }

        private static GameObject CreateCard(Transform parent)
        {
            var card = new GameObject(
                "CardRoot",
                typeof(RectTransform),
                typeof(ResponsiveAspectRatioLayoutController));
            card.transform.SetParent(parent, false);
            return card;
        }
    }
}
