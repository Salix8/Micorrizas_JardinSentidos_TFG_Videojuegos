using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    [DisallowMultipleComponent]
    public sealed class GardenImageVotingCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image frameImage;
        [SerializeField] private RoundedPanelGraphic frameGraphic;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private GameObject illustrationPlaceholderRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text decisionHintLabel;

        private Action<bool> onDecisionCommitted;
        private Coroutine animationCoroutine;
        private Coroutine imageLoadingCoroutine;
        private Sprite runtimeSprite;
        private Vector2 initialAnchoredPosition;
        private float swipeThreshold;
        private float transitionDuration;
        private bool isInteractable;
        private GardenImageVotingCardVisualSettings visualSettings = GardenImageVotingCardVisualSettings.CreateDefault();
        private string currentBindingKey = string.Empty;
        private bool hasBinding;
        private bool currentBindingInteractable;

        private void Awake()
        {
            cardTransform ??= GetComponent<RectTransform>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            initialAnchoredPosition = cardTransform == null ? Vector2.zero : cardTransform.anchoredPosition;
        }

        public void Bind(
            GardenImageVotingCardDefinition definition,
            GardenImageVotingCardVisualSettings visuals,
            bool canInteract,
            float configuredSwipeThreshold,
            float configuredTransitionDuration,
            Action<bool> onDecision)
        {
            var bindingKey = definition == null
                ? "<null>"
                : $"{definition.RoundIndex}|{definition.DeviceSlot}|{definition.Title}|{definition.CommonName}|{definition.ImagePath}";

            visualSettings = visuals;
            swipeThreshold = configuredSwipeThreshold;
            transitionDuration = configuredTransitionDuration;
            onDecisionCommitted = onDecision;
            isInteractable = canInteract;

            if (hasBinding && string.Equals(currentBindingKey, bindingKey, StringComparison.Ordinal) && currentBindingInteractable == canInteract)
            {
                UpdateDecisionHint(0f);
                return;
            }

            hasBinding = true;
            currentBindingKey = bindingKey;
            currentBindingInteractable = canInteract;

            if (frameImage != null)
            {
                frameImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (frameGraphic != null)
            {
                frameGraphic.Configure(visualSettings.FrameColor, new Color(0.84f, 0.79f, 0.63f, 1f), 34f, 3f);
            }

            if (titleLabel != null)
            {
                titleLabel.color = visualSettings.TitleColor;
                titleLabel.text = definition == null ? "Imagen pendiente" : definition.Title;
            }

            if (bodyLabel != null)
            {
                bodyLabel.color = visualSettings.BodyColor;
                bodyLabel.text = definition == null
                    ? "Todavia no hay una planta disponible para este dispositivo."
                    : definition.CommonName;
            }

            if (decisionHintLabel != null)
            {
                decisionHintLabel.text = canInteract ? "Arrastra para responder" : "Esperando...";
                decisionHintLabel.color = visualSettings.BodyColor;
            }

            ResetTransform();
            LoadIllustration(definition);
        }

        public void ShowMessage(string title, string body)
        {
            isInteractable = false;
            onDecisionCommitted = null;
            hasBinding = false;
            currentBindingKey = string.Empty;
            ResetTransform();
            ReleaseRuntimeSprite();

            if (titleLabel != null)
            {
                titleLabel.text = title;
                titleLabel.color = visualSettings.TitleColor;
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = body;
                bodyLabel.color = visualSettings.BodyColor;
            }

            if (decisionHintLabel != null)
            {
                decisionHintLabel.text = string.Empty;
            }

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.enabled = false;
            }

            if (illustrationPlaceholderRoot != null)
            {
                illustrationPlaceholderRoot.SetActive(true);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isInteractable || animationCoroutine != null)
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isInteractable || animationCoroutine != null || cardTransform == null)
            {
                return;
            }

            cardTransform.anchoredPosition += eventData.delta;
            cardTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(cardTransform.anchoredPosition.x * -0.05f, -12f, 12f));
            UpdateDecisionHint(cardTransform.anchoredPosition.x);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isInteractable || animationCoroutine != null || cardTransform == null)
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            var horizontalOffset = cardTransform.anchoredPosition.x;
            if (Mathf.Abs(horizontalOffset) < swipeThreshold)
            {
                animationCoroutine = StartCoroutine(AnimateReturnCoroutine());
                return;
            }

            var answeredYes = horizontalOffset > 0f;
            isInteractable = false;
            onDecisionCommitted?.Invoke(answeredYes);
            animationCoroutine = StartCoroutine(AnimateDecisionCoroutine(answeredYes));
        }

        private void LoadIllustration(GardenImageVotingCardDefinition definition)
        {
            ReleaseRuntimeSprite();

            if (imageLoadingCoroutine != null)
            {
                StopCoroutine(imageLoadingCoroutine);
                imageLoadingCoroutine = null;
            }

            if (illustrationImage != null)
            {
                illustrationImage.color = Color.white;
                illustrationImage.enabled = false;
            }

            if (illustrationPlaceholderRoot != null)
            {
                illustrationPlaceholderRoot.SetActive(true);
            }

            if (definition == null || string.IsNullOrWhiteSpace(definition.ImagePath))
            {
                if (bodyLabel != null)
                {
                    bodyLabel.text = "Imagen pendiente de configurar. De momento esta carta funciona como placeholder editable desde el CSV.";
                }

                return;
            }

            imageLoadingCoroutine = StartCoroutine(GardenImageVotingExternalContentService.LoadSpriteAsync(
                definition.ImagePath,
                (sprite, error) =>
                {
                    imageLoadingCoroutine = null;

                    if (sprite == null)
                    {
                        Debug.LogWarning($"[GardenImageVoting] No se ha podido cargar la imagen '{definition.ImagePath}'. Error: {error}", this);
                        if (bodyLabel != null && !string.IsNullOrWhiteSpace(error))
                        {
                            bodyLabel.text = $"No se ha podido cargar la imagen.\n{error}";
                        }

                        return;
                    }

                    runtimeSprite = sprite;
                    if (illustrationImage != null)
                    {
                        illustrationImage.sprite = runtimeSprite;
                        illustrationImage.enabled = true;
                    }

                    if (illustrationPlaceholderRoot != null)
                    {
                        illustrationPlaceholderRoot.SetActive(false);
                    }
                }));
        }

        private IEnumerator AnimateReturnCoroutine()
        {
            var startPosition = cardTransform.anchoredPosition;
            var startRotation = cardTransform.rotation;
            var elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / transitionDuration);
                cardTransform.anchoredPosition = Vector2.Lerp(startPosition, initialAnchoredPosition, progress);
                cardTransform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, progress);
                yield return null;
            }

            ResetTransform();
            animationCoroutine = null;
        }

        private IEnumerator AnimateDecisionCoroutine(bool answeredYes)
        {
            var targetOffset = new Vector2(GetExitOffset(answeredYes), 0f);
            var startPosition = cardTransform.anchoredPosition;
            var startRotationZ = NormalizeAngle(cardTransform.rotation.eulerAngles.z);
            var targetRotationZ = answeredYes ? -18f : 18f;
            var elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / transitionDuration);
                cardTransform.anchoredPosition = Vector2.Lerp(startPosition, targetOffset, progress);
                cardTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRotationZ, targetRotationZ, progress));
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0.2f, progress);
                }

                yield return null;
            }

            ResetTransform();
            animationCoroutine = null;
        }

        private void UpdateDecisionHint(float horizontalOffset)
        {
            if (decisionHintLabel == null)
            {
                return;
            }

            if (horizontalOffset >= swipeThreshold * 0.4f)
            {
                decisionHintLabel.text = "Si la he visto";
                decisionHintLabel.color = visualSettings.SwipeRightColor;
                return;
            }

            if (horizontalOffset <= -swipeThreshold * 0.4f)
            {
                decisionHintLabel.text = "No la he visto";
                decisionHintLabel.color = visualSettings.SwipeLeftColor;
                return;
            }

            decisionHintLabel.text = isInteractable ? "Arrastra para responder" : "Esperando...";
            decisionHintLabel.color = visualSettings.BodyColor;
        }

        private void ResetTransform()
        {
            if (cardTransform != null)
            {
                cardTransform.anchoredPosition = initialAnchoredPosition;
                cardTransform.rotation = Quaternion.identity;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private float GetExitOffset(bool answeredYes)
        {
            if (cardTransform == null)
            {
                return answeredYes ? 900f : -900f;
            }

            var direction = answeredYes ? 1f : -1f;
            var parentRectTransform = cardTransform.parent as RectTransform;
            if (parentRectTransform == null)
            {
                return direction * 900f;
            }

            var travelDistance = parentRectTransform.rect.width * 0.5f + cardTransform.rect.width;
            return direction * Mathf.Max(900f, travelDistance);
        }

        private void ReleaseRuntimeSprite()
        {
            if (runtimeSprite == null)
            {
                return;
            }

            var runtimeTexture = runtimeSprite.texture;
            Destroy(runtimeSprite);
            runtimeSprite = null;

            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
            }
        }

        private static float NormalizeAngle(float rawAngle)
        {
            return rawAngle > 180f ? rawAngle - 360f : rawAngle;
        }
    }
}
