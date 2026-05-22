using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsHandView : MonoBehaviour
    {
        [SerializeField] private Transform cardRoot;
        [SerializeField] private DistributedPairsCardView cardPrefab;
        [SerializeField] private ResponsiveGridLayoutController responsiveGridLayoutController;
        [SerializeField] private RectTransform drawPileAnchor;
        [SerializeField] [Min(0.05f)] private float drawAnimationDuration = 0.28f;
        [SerializeField] [Range(0.2f, 1f)] private float drawAnimationStartScale = 0.84f;
        [SerializeField] private Color emptySlotBackgroundColor = new(1f, 1f, 1f, 0.08f);

        private readonly List<HandSlotView> handSlotViews = new();

        private sealed class HandSlotView
        {
            public RectTransform SlotRectTransform;
            public Image SlotBackgroundImage;
            public DistributedPairsCardView CardView;
            public int LastCardInstanceId = -1;
            public Coroutine ActiveAnimation;
        }

        public void SetDrawPileAnchor(RectTransform anchor)
        {
            drawPileAnchor = anchor;
        }

        public void Render(
            IReadOnlyList<DistributedPairsCardNetworkState> handStates,
            DistributedPairsMinigameConfig config,
            bool isInteractable,
            bool showMismatchMemoryState,
            bool showMatchedFeedback,
            Action<int> onCardSelected)
        {
            if (cardRoot == null || config == null)
            {
                return;
            }

            EnsureSlots(config.CardsPerDevice);
            if (handSlotViews.Count == 0)
            {
                return;
            }

            if (responsiveGridLayoutController != null)
            {
                var visuals = config.CardVisualSettings;
                responsiveGridLayoutController.Configure(
                    visuals.MaxColumns,
                    visuals.MinCardSize,
                    visuals.MaxCardSize,
                    visuals.CardAspectRatio);
            }

            var slotModels = DistributedPairsHandSlotService.BuildSlots(handStates, config.CardsPerDevice);
            for (var slotIndex = 0; slotIndex < handSlotViews.Count; slotIndex++)
            {
                var slotView = handSlotViews[slotIndex];
                ApplySlotBackground(slotView, hasCard: slotModels[slotIndex].HasCard);

                if (slotModels[slotIndex].HasCard)
                {
                    var state = slotModels[slotIndex].CardState;
                    slotView.CardView.gameObject.SetActive(true);
                    slotView.CardView.Bind(
                        state,
                        config.GetPairDefinition(state.PairId),
                        config.CardVisualSettings,
                        config.MatchFeedbackSettings,
                        isInteractable,
                        showMismatchMemoryState,
                        showMatchedFeedback,
                        onCardSelected);

                    var shouldAnimateDraw = slotView.LastCardInstanceId != state.CardInstanceId;
                    slotView.LastCardInstanceId = state.CardInstanceId;

                    if (shouldAnimateDraw)
                    {
                        RestartDrawAnimation(slotView);
                    }
                }
                else
                {
                    slotView.LastCardInstanceId = -1;
                    slotView.CardView.gameObject.SetActive(true);
                    slotView.CardView.BindEmptySlot(config.CardVisualSettings, slotIndex);
                }
            }

            if (responsiveGridLayoutController != null)
            {
                responsiveGridLayoutController.RefreshLayout();
            }
        }

        private void EnsureSlots(int slotCount)
        {
            RegisterSceneAuthoredSlots();

            for (var index = handSlotViews.Count; index < slotCount; index++)
            {
                if (cardPrefab == null)
                {
                    break;
                }

                var slotObject = new GameObject($"HandSlot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                slotObject.transform.SetParent(cardRoot, false);
                slotObject.layer = cardRoot.gameObject.layer;

                var slotRectTransform = slotObject.GetComponent<RectTransform>();
                slotRectTransform.anchorMin = Vector2.zero;
                slotRectTransform.anchorMax = Vector2.one;
                slotRectTransform.offsetMin = Vector2.zero;
                slotRectTransform.offsetMax = Vector2.zero;

                var slotBackgroundImage = slotObject.GetComponent<Image>();
                slotBackgroundImage.color = emptySlotBackgroundColor;
                slotBackgroundImage.raycastTarget = false;

                var layoutElement = slotObject.GetComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;
                layoutElement.flexibleHeight = 1f;

                var cardView = Instantiate(cardPrefab, slotObject.transform, false);
                var cardRectTransform = cardView.RectTransform;
                cardRectTransform.anchorMin = Vector2.zero;
                cardRectTransform.anchorMax = Vector2.one;
                cardRectTransform.offsetMin = Vector2.zero;
                cardRectTransform.offsetMax = Vector2.zero;

                handSlotViews.Add(new HandSlotView
                {
                    SlotRectTransform = slotRectTransform,
                    SlotBackgroundImage = slotBackgroundImage,
                    CardView = cardView
                });
            }

            for (var index = 0; index < handSlotViews.Count; index++)
            {
                handSlotViews[index].SlotRectTransform.SetSiblingIndex(index);
            }
        }

        private void RegisterSceneAuthoredSlots()
        {
            if (cardRoot == null || handSlotViews.Count > 0)
            {
                return;
            }

            for (var index = 0; index < cardRoot.childCount; index++)
            {
                var child = cardRoot.GetChild(index);
                var cardView = child.GetComponentInChildren<DistributedPairsCardView>(true);
                if (cardView == null || child is not RectTransform slotRectTransform)
                {
                    continue;
                }

                var slotBackgroundImage = child.GetComponent<Image>();
                if (slotBackgroundImage == null)
                {
                    slotBackgroundImage = child.gameObject.AddComponent<Image>();
                    slotBackgroundImage.raycastTarget = false;
                }

                var layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = child.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.flexibleWidth = 1f;
                layoutElement.flexibleHeight = 1f;

                var cardRectTransform = cardView.RectTransform;
                cardRectTransform.anchorMin = Vector2.zero;
                cardRectTransform.anchorMax = Vector2.one;
                cardRectTransform.offsetMin = Vector2.zero;
                cardRectTransform.offsetMax = Vector2.zero;

                handSlotViews.Add(new HandSlotView
                {
                    SlotRectTransform = slotRectTransform,
                    SlotBackgroundImage = slotBackgroundImage,
                    CardView = cardView
                });
            }
        }

        private void ApplySlotBackground(HandSlotView slotView, bool hasCard)
        {
            if (slotView.SlotBackgroundImage == null)
            {
                return;
            }

            slotView.SlotBackgroundImage.color = hasCard ? Color.clear : emptySlotBackgroundColor;
        }

        private void RestartDrawAnimation(HandSlotView slotView)
        {
            if (slotView.ActiveAnimation != null)
            {
                StopCoroutine(slotView.ActiveAnimation);
            }

            if (drawPileAnchor == null || drawAnimationDuration <= 0f || !isActiveAndEnabled)
            {
                return;
            }

            slotView.ActiveAnimation = StartCoroutine(AnimateDrawFromDeck(slotView));
        }

        private IEnumerator AnimateDrawFromDeck(HandSlotView slotView)
        {
            yield return null;

            if (slotView.CardView == null || drawPileAnchor == null)
            {
                yield break;
            }

            var cardRectTransform = slotView.CardView.RectTransform;
            var targetPosition = cardRectTransform.position;
            var targetScale = cardRectTransform.localScale;
            var startScale = targetScale * drawAnimationStartScale;
            var elapsed = 0f;

            cardRectTransform.position = drawPileAnchor.position;
            cardRectTransform.localScale = startScale;

            while (elapsed < drawAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / drawAnimationDuration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                cardRectTransform.position = Vector3.LerpUnclamped(drawPileAnchor.position, targetPosition, easedProgress);
                cardRectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress);
                yield return null;
            }

            cardRectTransform.position = targetPosition;
            cardRectTransform.localScale = targetScale;
            slotView.ActiveAnimation = null;
        }
    }
}
