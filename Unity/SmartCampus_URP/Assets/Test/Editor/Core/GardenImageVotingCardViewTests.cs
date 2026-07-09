using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.GardenImageVoting;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class GardenImageVotingCardViewTests
    {
        private GameObject root;
        private GardenImageVotingCardView cardView;
        private RoundedPanelGraphic frameGraphic;
        private TMP_Text titleLabel;
        private TMP_Text bodyLabel;
        private TMP_Text decisionHintLabel;
        private GameObject illustrationPlaceholderRoot;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "GardenImageVotingCardView",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(GardenImageVotingCardView));

            cardView = root.GetComponent<GardenImageVotingCardView>();
            frameGraphic = new GameObject("RoundedCardBackground", typeof(RectTransform), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            frameGraphic.transform.SetParent(root.transform, false);

            titleLabel = CreateText("CardTitleLabel", root.transform);
            bodyLabel = CreateText("BodyLabel", root.transform);
            decisionHintLabel = CreateText("DecisionHintLabel", root.transform);
            illustrationPlaceholderRoot = new GameObject("IllustrationPlaceholder");
            illustrationPlaceholderRoot.transform.SetParent(root.transform, false);

            SetPrivateField("cardTransform", root.GetComponent<RectTransform>());
            SetPrivateField("canvasGroup", root.GetComponent<CanvasGroup>());
            SetPrivateField("frameImage", root.GetComponent<Image>());
            SetPrivateField("frameGraphic", frameGraphic);
            SetPrivateField("titleLabel", titleLabel);
            SetPrivateField("bodyLabel", bodyLabel);
            SetPrivateField("decisionHintLabel", decisionHintLabel);
            SetPrivateField("illustrationPlaceholderRoot", illustrationPlaceholderRoot);
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
        public void Bind_UsesBackgroundColorAsCardFillAndFrameColorAsBorder()
        {
            var visuals = GardenImageVotingCardVisualSettings.CreateDefault();
            var definition = new GardenImageVotingCardDefinition(1, 0, "Roble", "Quercus robur", string.Empty, true);

            cardView.Bind(definition, visuals, true, 120f, 0.2f, _ => { });

            var serializedGraphic = new SerializedObject(frameGraphic);
            Assert.That(serializedGraphic.FindProperty("fillColor").colorValue, Is.EqualTo(visuals.BackgroundColor));
            Assert.That(serializedGraphic.FindProperty("borderColor").colorValue, Is.EqualTo(visuals.FrameColor));
            Assert.That(titleLabel.color, Is.EqualTo(visuals.TitleColor));
            Assert.That(bodyLabel.color, Is.EqualTo(visuals.BodyColor));
        }

        [Test]
        public void ShowMessage_WhenPlaceholderIsDisabled_HidesImagePlaceholder()
        {
            illustrationPlaceholderRoot.SetActive(true);

            cardView.ShowMessage("Secuencia completada", "No quedan imagenes.", showIllustrationPlaceholder: false);

            Assert.That(illustrationPlaceholderRoot.activeSelf, Is.False);
            Assert.That(titleLabel.text, Is.EqualTo("Secuencia completada"));
            Assert.That(bodyLabel.text, Is.EqualTo("No quedan imagenes."));
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TMP_Text>();
        }

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(GardenImageVotingCardView)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(cardView, value);
        }
    }
}
