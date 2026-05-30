using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using SmartCampus.Coop.Minigames.DistributedPairs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsHandViewTests
    {
        private GameObject root;
        private DistributedPairsHandView handView;
        private Transform cardRoot;
        private DistributedPairsMinigameConfig config;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("HandViewRoot", typeof(RectTransform), typeof(DistributedPairsHandView));
            handView = root.GetComponent<DistributedPairsHandView>();

            var cardRootObject = new GameObject(
                "CardRoot",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ResponsiveGridLayoutController));
            cardRootObject.transform.SetParent(root.transform, false);
            cardRoot = cardRootObject.transform;

            SetPrivateField(handView, "cardRoot", cardRoot);
            SetPrivateField(handView, "responsiveGridLayoutController", cardRootObject.GetComponent<ResponsiveGridLayoutController>());

            CreateSceneAuthoredSlot("HandSlot_1");
            CreateSceneAuthoredSlot("HandSlot_2");
            CreateSceneAuthoredSlot("HandSlot_3");
            CreateSceneAuthoredSlot("HandSlot_4");

            config = ScriptableObject.CreateInstance<DistributedPairsMinigameConfig>();
            SetPrivateField(config, "cardsPerDevice", 6);
            SetPrivateField(config, "pairsToUse", 12);
            SetPrivateField(config, "guaranteedVisiblePairsOffset", 1);
            SetPrivateField(config, "pairDefinitions", Enumerable.Range(0, 12).Select(_ => new DistributedPairDefinition()).ToList());
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Render_WithSceneAuthoredCardsAndNoPrefab_CreatesAdditionalSlotsFromExistingTemplate()
        {
            var handStates = Enumerable.Range(0, 6)
                .Select(index => new DistributedPairsCardNetworkState
                {
                    CardInstanceId = index,
                    PairId = index,
                    OwnerClientId = 1UL,
                    IsSelected = false,
                    HandOrder = index,
                    Zone = DistributedPairsCardZone.Hand
                })
                .ToList();

            handView.Render(
                handStates,
                config,
                isInteractable: true,
                showMismatchMemoryState: false,
                showMatchedFeedback: false,
                onCardSelected: null);

            Assert.That(cardRoot.childCount, Is.EqualTo(6));
            Assert.That(cardRoot.Find("HandSlot_5"), Is.Not.Null);
            Assert.That(cardRoot.Find("HandSlot_6"), Is.Not.Null);
        }

        private void CreateSceneAuthoredSlot(string name)
        {
            var slotObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObject.transform.SetParent(cardRoot, false);

            var cardView = CreateCardView(slotObject.transform);
            var cardRectTransform = cardView.RectTransform;
            cardRectTransform.anchorMin = Vector2.zero;
            cardRectTransform.anchorMax = Vector2.one;
            cardRectTransform.offsetMin = Vector2.zero;
            cardRectTransform.offsetMax = Vector2.zero;
        }

        private static DistributedPairsCardView CreateCardView(Transform parent)
        {
            var cardRootObject = new GameObject(
                "CardView",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(DistributedPairsCardView));
            cardRootObject.transform.SetParent(parent, false);

            var selectionButton = cardRootObject.GetComponent<Button>();
            selectionButton.targetGraphic = cardRootObject.GetComponent<Image>();
            var cardView = cardRootObject.GetComponent<DistributedPairsCardView>();

            var frontFaceRoot = new GameObject("FrontFace", typeof(RectTransform), typeof(Image));
            frontFaceRoot.transform.SetParent(cardRootObject.transform, false);
            var frontFaceBackground = frontFaceRoot.GetComponent<Image>();

            var illustrationObject = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
            illustrationObject.transform.SetParent(frontFaceRoot.transform, false);
            var illustrationImage = illustrationObject.GetComponent<Image>();

            var titleLabel = CreateText("Title", frontFaceRoot.transform);
            var descriptionLabel = CreateText("Description", frontFaceRoot.transform);

            var backFaceRoot = new GameObject("BackFace", typeof(RectTransform), typeof(Image));
            backFaceRoot.transform.SetParent(cardRootObject.transform, false);
            var backFaceBackground = backFaceRoot.GetComponent<Image>();
            var backFaceLabel = CreateText("BackLabel", backFaceRoot.transform);

            SetPrivateField(cardView, "selectionButton", selectionButton);
            SetPrivateField(cardView, "frameImage", cardRootObject.GetComponent<Image>());
            SetPrivateField(cardView, "frontFaceRoot", frontFaceRoot);
            SetPrivateField(cardView, "frontFaceBackground", frontFaceBackground);
            SetPrivateField(cardView, "illustrationImage", illustrationImage);
            SetPrivateField(cardView, "titleLabel", titleLabel);
            SetPrivateField(cardView, "descriptionLabel", descriptionLabel);
            SetPrivateField(cardView, "backFaceRoot", backFaceRoot);
            SetPrivateField(cardView, "backFaceBackground", backFaceBackground);
            SetPrivateField(cardView, "backFaceLabel", backFaceLabel);

            return cardView;
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
