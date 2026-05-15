using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsCardViewTests
    {
        private GameObject root;
        private DistributedPairsCardView cardView;
        private GameObject frontFaceRoot;
        private Image frontFaceBackground;
        private Image illustrationImage;
        private TMP_Text titleLabel;
        private TMP_Text descriptionLabel;
        private GameObject backFaceRoot;
        private Image backFaceBackground;
        private TMP_Text backFaceLabel;
        private Button selectionButton;
        private Texture2D illustrationTexture;
        private Sprite illustrationSprite;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "DistributedPairsCardView",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(DistributedPairsCardView));

            selectionButton = root.GetComponent<Button>();
            selectionButton.targetGraphic = root.GetComponent<Image>();
            cardView = root.GetComponent<DistributedPairsCardView>();

            frontFaceRoot = new GameObject("FrontFace", typeof(RectTransform), typeof(Image));
            frontFaceRoot.transform.SetParent(root.transform, false);
            frontFaceBackground = frontFaceRoot.GetComponent<Image>();

            var illustrationObject = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
            illustrationObject.transform.SetParent(frontFaceRoot.transform, false);
            illustrationImage = illustrationObject.GetComponent<Image>();
            illustrationImage.color = new Color(1f, 1f, 1f, 0.02f);

            titleLabel = CreateText("Title", frontFaceRoot.transform);
            descriptionLabel = CreateText("Description", frontFaceRoot.transform);

            backFaceRoot = new GameObject("BackFace", typeof(RectTransform), typeof(Image));
            backFaceRoot.transform.SetParent(root.transform, false);
            backFaceBackground = backFaceRoot.GetComponent<Image>();
            backFaceLabel = CreateText("BackLabel", backFaceRoot.transform);

            SetPrivateField("selectionButton", selectionButton);
            SetPrivateField("frameImage", root.GetComponent<Image>());
            SetPrivateField("frontFaceRoot", frontFaceRoot);
            SetPrivateField("frontFaceBackground", frontFaceBackground);
            SetPrivateField("illustrationImage", illustrationImage);
            SetPrivateField("titleLabel", titleLabel);
            SetPrivateField("descriptionLabel", descriptionLabel);
            SetPrivateField("backFaceRoot", backFaceRoot);
            SetPrivateField("backFaceBackground", backFaceBackground);
            SetPrivateField("backFaceLabel", backFaceLabel);

            illustrationTexture = new Texture2D(8, 8);
            illustrationSprite = Sprite.Create(
                illustrationTexture,
                new Rect(0f, 0f, illustrationTexture.width, illustrationTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            if (illustrationSprite != null)
            {
                Object.DestroyImmediate(illustrationSprite);
            }

            if (illustrationTexture != null)
            {
                Object.DestroyImmediate(illustrationTexture);
            }

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_WhenPairHasIllustration_MakesIllustrationVisibleAndOpaque()
        {
            var pairDefinition = CreatePairDefinition("Pareja ilustrada", illustrationSprite);
            var state = CreateState(isSelected: true);

            cardView.Bind(
                state,
                pairDefinition,
                DistributedPairsCardVisualSettings.CreateDefault(),
                isInteractable: true,
                onSelected: null);

            Assert.That(frontFaceRoot.activeSelf, Is.True);
            Assert.That(backFaceRoot.activeSelf, Is.False);
            Assert.That(illustrationImage.gameObject.activeSelf, Is.True);
            Assert.That(illustrationImage.sprite, Is.EqualTo(illustrationSprite));
            Assert.That(illustrationImage.preserveAspect, Is.True);
            Assert.That(illustrationImage.color.a, Is.EqualTo(1f));
        }

        [Test]
        public void Bind_WhenPairHasNoIllustration_HidesIllustrationAndClearsSprite()
        {
            illustrationImage.sprite = illustrationSprite;
            illustrationImage.color = new Color(1f, 1f, 1f, 1f);

            var pairDefinition = CreatePairDefinition("Pareja sin ilustracion", null);
            var state = CreateState(isSelected: true);

            cardView.Bind(
                state,
                pairDefinition,
                DistributedPairsCardVisualSettings.CreateDefault(),
                isInteractable: true,
                onSelected: null);

            Assert.That(illustrationImage.gameObject.activeSelf, Is.False);
            Assert.That(illustrationImage.sprite, Is.Null);
            Assert.That(illustrationImage.color.a, Is.EqualTo(0f));
        }

        private DistributedPairsCardNetworkState CreateState(bool isSelected)
        {
            return new DistributedPairsCardNetworkState
            {
                CardInstanceId = 10,
                PairId = 0,
                OwnerClientId = 1UL,
                IsSelected = isSelected,
                HandOrder = 0,
                Zone = DistributedPairsCardZone.Hand
            };
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private static DistributedPairDefinition CreatePairDefinition(string title, Sprite illustration)
        {
            var pairDefinition = new DistributedPairDefinition();
            SetPairField(pairDefinition, "title", title);
            SetPairField(pairDefinition, "description", "Descripcion");
            SetPairField(pairDefinition, "illustration", illustration);
            SetPairField(pairDefinition, "faceColor", Color.white);
            return pairDefinition;
        }

        private static void SetPairField(DistributedPairDefinition pairDefinition, string fieldName, object value)
        {
            var field = typeof(DistributedPairDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(pairDefinition, value);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            var field = typeof(DistributedPairsCardView).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(cardView, value);
        }
    }
}
