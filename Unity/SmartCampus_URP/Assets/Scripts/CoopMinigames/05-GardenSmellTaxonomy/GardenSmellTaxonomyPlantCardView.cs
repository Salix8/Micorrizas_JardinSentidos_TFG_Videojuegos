using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    [DisallowMultipleComponent]
    public sealed class GardenSmellTaxonomyPlantCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private GameObject illustrationPlaceholderRoot;
        [SerializeField] private TMP_Text scientificNameLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private LayoutElement layoutElement;

        [Header("Internal Layout")]
        [SerializeField] private bool arrangeContentHorizontally = true;
        [SerializeField] private Vector2 contentPadding = new(26f, 24f);
        [SerializeField] [Range(0.35f, 0.7f)] private float illustrationWidthRatio = 0.56f;
        [SerializeField] [Min(0f)] private float horizontalGap = 22f;
        [SerializeField] [Range(0.35f, 0.75f)] private float scientificNameHeightRatio = 0.55f;

        [Header("Drag Feel")]
        [SerializeField] [Min(0f)] private float dragRotationSensitivity = 0.04f;
        [SerializeField] [Min(0f)] private float maxDragRotationDegrees = 10f;

        [Header("Image Loading")]
        [SerializeField] [Min(1)] private int maxImageLoadAttempts = 4;
        [SerializeField] [Min(0f)] private float imageRetryDelaySeconds = 0.35f;

        private Func<Vector2, Camera, GardenSmellTaxonomyCategory?> resolveDropCategory;
        private Action<GardenSmellTaxonomyCategory?> onHoverCategoryChanged;
        private Action<GardenSmellTaxonomyCategory> onClassificationCommitted;
        private Coroutine animationCoroutine;
        private Coroutine imageLoadingCoroutine;
        private Sprite runtimeSprite;
        private Vector2 initialAnchoredPosition;
        private Vector2 dragStartPointerLocalPosition;
        private Vector2 dragStartAnchoredPosition;
        private float transitionDuration;
        private bool isInteractable;
        private bool isDragging;
        private bool hasActiveDragPointer;
        private bool hasCapturedLayoutPosition;
        private bool hasCapturedEditorTextColors;
        private bool hasCapturedLayoutElementState;
        private bool originalIgnoreLayout;
        private bool hasBinding;
        private Color scientificNameEditorColor = Color.white;
        private Color helperEditorColor = Color.white;
        private string currentBindingKey = string.Empty;
        private string currentCommonName = string.Empty;
        private GardenSmellTaxonomyCategory? hoveredCategory;
        private GardenSmellTaxonomyVisualSettings visualSettings = GardenSmellTaxonomyVisualSettings.CreateDefault();

        private void Awake()
        {
            cardTransform ??= GetComponent<RectTransform>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            layoutElement ??= GetComponent<LayoutElement>();
            CaptureEditorTextColors();
            ApplyInternalLayout();
            CaptureCurrentLayoutPosition();
        }

        private void Start()
        {
            RefreshLayoutBaseline();
        }

        private void OnEnable()
        {
            RefreshLayoutBaseline();
        }

        private void OnValidate()
        {
            ApplyInternalLayout();
        }

        private void OnDisable()
        {
            RestoreLayoutParticipation();
            ReleaseRuntimeSprite();
        }

        public void Bind(
            GardenSmellTaxonomyPlantDefinition definition,
            GardenSmellTaxonomyVisualSettings visuals,
            bool canInteract,
            float configuredTransitionDuration,
            Func<Vector2, Camera, GardenSmellTaxonomyCategory?> dropResolver,
            Action<GardenSmellTaxonomyCategory?> hoverChanged,
            Action<GardenSmellTaxonomyCategory> onClassification)
        {
            var bindingKey = definition == null
                ? "<null>"
                : $"{definition.PlantId}|{definition.ScientificName}|{definition.ImagePath}";

            visualSettings = visuals;
            transitionDuration = configuredTransitionDuration;
            resolveDropCategory = dropResolver;
            onHoverCategoryChanged = hoverChanged;
            onClassificationCommitted = onClassification;
            isInteractable = canInteract;

            if (hasBinding && string.Equals(currentBindingKey, bindingKey, StringComparison.Ordinal))
            {
                if (definition != null && !HasVisibleIllustration() && imageLoadingCoroutine == null)
                {
                    LoadIllustration(definition);
                }

                UpdateHelperLabel(hoveredCategory);
                return;
            }

            StopActiveAnimation();
            hasBinding = true;
            currentBindingKey = bindingKey;
            currentCommonName = definition == null ? string.Empty : definition.CommonName;
            hoveredCategory = null;
            ApplyInternalLayout();
            RefreshLayoutBaseline();

            if (frameImage != null)
            {
                frameImage.color = visuals.CardFrameColor;
            }

            if (scientificNameLabel != null)
            {
                scientificNameLabel.text = definition == null ? "Sin planta activa" : definition.ScientificName;
                scientificNameLabel.color = scientificNameEditorColor;
            }

            ResetTransform();
            UpdateHelperLabel(null);
            LoadIllustration(definition);
        }

        public void ShowMessage(string title, string helperText)
        {
            isInteractable = false;
            isDragging = false;
            hasBinding = false;
            currentBindingKey = string.Empty;
            currentCommonName = helperText;
            hoveredCategory = null;
            onHoverCategoryChanged?.Invoke(null);
            onClassificationCommitted = null;
            resolveDropCategory = null;
            StopActiveAnimation();
            ReleaseRuntimeSprite();
            RefreshLayoutBaseline();
            ResetTransform();

            if (scientificNameLabel != null)
            {
                scientificNameLabel.text = title;
                scientificNameLabel.color = scientificNameEditorColor;
            }

            if (helperLabel != null)
            {
                helperLabel.text = currentCommonName;
                helperLabel.color = helperEditorColor;
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

            RefreshLayoutBaseline();
            SuspendLayoutParticipation();
            CaptureDragStart(eventData);
            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isInteractable || animationCoroutine != null || cardTransform == null)
            {
                return;
            }

            UpdateDragPosition(eventData);
            ApplyDragRotation();

            hoveredCategory = resolveDropCategory == null
                ? null
                : resolveDropCategory(eventData.position, eventData.pressEventCamera);

            onHoverCategoryChanged?.Invoke(hoveredCategory);
            UpdateHelperLabel(hoveredCategory);
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

            var resolvedCategory = resolveDropCategory == null
                ? null
                : resolveDropCategory(eventData.position, eventData.pressEventCamera);

            hoveredCategory = null;
            onHoverCategoryChanged?.Invoke(null);

            if (!resolvedCategory.HasValue)
            {
                isDragging = false;
                hasActiveDragPointer = false;
                animationCoroutine = StartCoroutine(AnimateReturnCoroutine());
                return;
            }

            isInteractable = false;
            isDragging = false;
            hasActiveDragPointer = false;
            animationCoroutine = StartCoroutine(AnimateCommitCoroutine(resolvedCategory.Value));
            onClassificationCommitted?.Invoke(resolvedCategory.Value);
        }

        private void LoadIllustration(GardenSmellTaxonomyPlantDefinition definition)
        {
            ReleaseRuntimeSprite();

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
                UpdateHelperLabel(null);
                return;
            }

            imageLoadingCoroutine = StartCoroutine(LoadIllustrationWithRetriesCoroutine(definition, currentBindingKey));
        }

        private IEnumerator LoadIllustrationWithRetriesCoroutine(GardenSmellTaxonomyPlantDefinition definition, string requestedBindingKey)
        {
            var attempts = Mathf.Max(1, maxImageLoadAttempts);
            var retryDelay = Mathf.Max(0f, imageRetryDelaySeconds);
            string lastError = string.Empty;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                Sprite loadedSprite = null;
                string loadError = string.Empty;

                yield return CoopMinigameExternalContentService.LoadSpriteAsync(
                    definition.ImagePath,
                    (sprite, error) =>
                    {
                        loadedSprite = sprite;
                        loadError = error;
                    });

                if (!string.Equals(currentBindingKey, requestedBindingKey, StringComparison.Ordinal))
                {
                    if (loadedSprite != null)
                    {
                        DestroyRuntimeSprite(loadedSprite);
                    }

                    imageLoadingCoroutine = null;
                    yield break;
                }

                if (loadedSprite != null)
                {
                    runtimeSprite = loadedSprite;
                    if (illustrationImage != null)
                    {
                        illustrationImage.sprite = runtimeSprite;
                        illustrationImage.enabled = true;
                    }

                    if (illustrationPlaceholderRoot != null)
                    {
                        illustrationPlaceholderRoot.SetActive(false);
                    }

                    imageLoadingCoroutine = null;
                    yield break;
                }

                lastError = loadError;
                Debug.LogWarning(
                    $"[GardenSmellTaxonomy] No se pudo cargar la imagen '{definition.ImagePath}' para '{definition.ScientificName}' en el intento {attempt}/{attempts}: {loadError}",
                    this);

                if (attempt < attempts && retryDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(retryDelay);
                }
            }

            if (helperLabel != null && !string.IsNullOrWhiteSpace(lastError))
            {
                helperLabel.text = $"No se ha podido cargar la imagen.\n{lastError}";
                helperLabel.color = visualSettings.IncorrectColor;
            }

            imageLoadingCoroutine = null;
        }

        private bool HasVisibleIllustration()
        {
            return illustrationImage != null &&
                illustrationImage.enabled &&
                illustrationImage.sprite != null;
        }

        private void ApplyInternalLayout()
        {
            if (!arrangeContentHorizontally)
            {
                return;
            }

            var illustrationRect = illustrationImage == null ? null : illustrationImage.rectTransform;
            var scientificRect = scientificNameLabel == null ? null : scientificNameLabel.rectTransform;
            var commonRect = helperLabel == null ? null : helperLabel.rectTransform;
            if (illustrationRect == null || scientificRect == null || commonRect == null)
            {
                return;
            }

            var leftPadding = contentPadding.x;
            var rightPadding = contentPadding.x;
            var topPadding = contentPadding.y;
            var bottomPadding = contentPadding.y;
            var gapHalf = horizontalGap * 0.5f;
            var illustrationRightAnchor = Mathf.Clamp(illustrationWidthRatio, 0.35f, 0.7f);
            var titleBottomAnchor = Mathf.Clamp01(1f - scientificNameHeightRatio);

            StretchToAnchors(
                illustrationRect,
                Vector2.zero,
                new Vector2(illustrationRightAnchor, 1f),
                new Vector2(leftPadding, bottomPadding),
                new Vector2(-gapHalf, -topPadding));

            StretchToAnchors(
                scientificRect,
                new Vector2(illustrationRightAnchor, titleBottomAnchor),
                Vector2.one,
                new Vector2(gapHalf, 0f),
                new Vector2(-rightPadding, -topPadding));

            StretchToAnchors(
                commonRect,
                new Vector2(illustrationRightAnchor, 0f),
                new Vector2(1f, titleBottomAnchor),
                new Vector2(gapHalf, bottomPadding),
                new Vector2(-rightPadding, -horizontalGap * 0.35f));

            if (illustrationPlaceholderRoot != null &&
                illustrationPlaceholderRoot.transform is RectTransform placeholderRect)
            {
                StretchToAnchors(placeholderRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private static void StretchToAnchors(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
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
            RestoreLayoutParticipation();
            animationCoroutine = null;
        }

        private IEnumerator AnimateCommitCoroutine(GardenSmellTaxonomyCategory category)
        {
            var targetOffset = GetCommitOffset(category);
            var startPosition = cardTransform.anchoredPosition;
            var startRotationZ = NormalizeAngle(cardTransform.rotation.eulerAngles.z);
            var targetRotationZ = category == GardenSmellTaxonomyCategory.Food ? 0f : (category == GardenSmellTaxonomyCategory.Decoration ? 12f : -12f);
            var elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / transitionDuration);
                cardTransform.anchoredPosition = Vector2.Lerp(startPosition, targetOffset, progress);
                cardTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRotationZ, targetRotationZ, progress));
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0.25f, progress);
                }

                yield return null;
            }

            ResetTransform();
            RestoreLayoutParticipation();
            animationCoroutine = null;
        }

        private void UpdateHelperLabel(GardenSmellTaxonomyCategory? currentCategory)
        {
            if (helperLabel == null)
            {
                return;
            }

            if (currentCategory.HasValue)
            {
                helperLabel.text = $"Suelta en {GardenSmellTaxonomyCategoryLabels.GetDisplayName(currentCategory.Value)}";
                helperLabel.color = visualSettings.GetCategoryColor(currentCategory.Value);
                return;
            }

            helperLabel.text = string.IsNullOrWhiteSpace(currentCommonName)
                ? (isInteractable ? "Nombre comun no disponible" : "Esperando...")
                : currentCommonName;
            helperLabel.color = helperEditorColor;
        }

        private Vector2 GetCommitOffset(GardenSmellTaxonomyCategory category)
        {
            switch (category)
            {
                case GardenSmellTaxonomyCategory.Decoration:
                    return initialAnchoredPosition + new Vector2(-540f, -220f);
                case GardenSmellTaxonomyCategory.Food:
                    return initialAnchoredPosition + new Vector2(0f, -420f);
                case GardenSmellTaxonomyCategory.Healing:
                    return initialAnchoredPosition + new Vector2(540f, -220f);
                default:
                    return initialAnchoredPosition;
            }
        }

        private void CaptureDragStart(PointerEventData eventData)
        {
            if (cardTransform == null || !TryGetPointerLocalPosition(eventData, out dragStartPointerLocalPosition))
            {
                hasActiveDragPointer = false;
                dragStartAnchoredPosition = cardTransform == null ? Vector2.zero : cardTransform.anchoredPosition;
                return;
            }

            dragStartAnchoredPosition = cardTransform.anchoredPosition;
            hasActiveDragPointer = true;
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (cardTransform == null)
            {
                return;
            }

            if (!hasActiveDragPointer || !TryGetPointerLocalPosition(eventData, out var currentPointerLocalPosition))
            {
                cardTransform.anchoredPosition += eventData.delta;
                return;
            }

            cardTransform.anchoredPosition = dragStartAnchoredPosition + (currentPointerLocalPosition - dragStartPointerLocalPosition);
        }

        private bool TryGetPointerLocalPosition(PointerEventData eventData, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            if (eventData == null || cardTransform == null || cardTransform.parent is not RectTransform parentRect)
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPosition);
        }

        private void ApplyDragRotation()
        {
            if (cardTransform == null)
            {
                return;
            }

            var horizontalDragOffset = cardTransform.anchoredPosition.x - initialAnchoredPosition.x;
            var rotationZ = Mathf.Clamp(
                horizontalDragOffset * -dragRotationSensitivity,
                -maxDragRotationDegrees,
                maxDragRotationDegrees);

            cardTransform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
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
            }
        }

        private void SuspendLayoutParticipation()
        {
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            if (!hasCapturedLayoutElementState)
            {
                originalIgnoreLayout = layoutElement.ignoreLayout;
                hasCapturedLayoutElementState = true;
            }

            layoutElement.ignoreLayout = true;
        }

        private void RestoreLayoutParticipation()
        {
            if (layoutElement == null || !hasCapturedLayoutElementState)
            {
                return;
            }

            layoutElement.ignoreLayout = originalIgnoreLayout;
            hasCapturedLayoutElementState = false;

            if (cardTransform != null && cardTransform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            }
        }

        private void RefreshLayoutBaseline()
        {
            if (cardTransform == null || isDragging)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            if (cardTransform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            Canvas.ForceUpdateCanvases();
            CaptureCurrentLayoutPosition();
        }

        private void CaptureCurrentLayoutPosition()
        {
            if (cardTransform == null)
            {
                initialAnchoredPosition = Vector2.zero;
                hasCapturedLayoutPosition = false;
                return;
            }

            initialAnchoredPosition = cardTransform.anchoredPosition;
            hasCapturedLayoutPosition = true;
        }

        private void CaptureEditorTextColors()
        {
            if (hasCapturedEditorTextColors)
            {
                return;
            }

            if (scientificNameLabel != null)
            {
                scientificNameEditorColor = scientificNameLabel.color;
            }

            if (helperLabel != null)
            {
                helperEditorColor = helperLabel.color;
            }

            hasCapturedEditorTextColors = true;
        }

        private void StopActiveAnimation()
        {
            if (animationCoroutine == null)
            {
                return;
            }

            StopCoroutine(animationCoroutine);
            animationCoroutine = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            RestoreLayoutParticipation();

            if (cardTransform != null && hasCapturedLayoutPosition)
            {
                cardTransform.rotation = Quaternion.identity;
            }
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        private void ReleaseRuntimeSprite()
        {
            if (imageLoadingCoroutine != null)
            {
                StopCoroutine(imageLoadingCoroutine);
                imageLoadingCoroutine = null;
            }

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
            }

            if (runtimeSprite != null)
            {
                DestroyRuntimeSprite(runtimeSprite);
                runtimeSprite = null;
            }
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var texture = sprite.texture;
            Destroy(sprite);
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
