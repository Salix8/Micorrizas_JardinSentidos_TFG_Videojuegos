using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueOpeningLoadingView : MonoBehaviour
    {
        private const float SpinnerDegreesPerSecond = 180f;

        private RectTransform spinnerRectTransform;
        private TMP_Text loadingLabel;
        private bool initialized;

        private void Update()
        {
            if (spinnerRectTransform != null)
            {
                spinnerRectTransform.Rotate(0f, 0f, -SpinnerDegreesPerSecond * Time.unscaledDeltaTime);
            }
        }

        public void Show(string loadingText)
        {
            EnsureInitialized();
            loadingLabel.text = string.IsNullOrWhiteSpace(loadingText) ? "Cargando..." : loadingText;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            var background = GetComponent<Image>();
            background.color = new Color(0.035f, 0.025f, 0.02f, 0.82f);
            background.raycastTarget = true;

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            spinnerRectTransform = CreateSpinner();
            loadingLabel = CreateLoadingLabel();
            initialized = true;
        }

        private RectTransform CreateSpinner()
        {
            var spinner = new GameObject(
                "OpeningLoadingSpinner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            spinner.transform.SetParent(transform, false);

            var spinnerRect = spinner.GetComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.anchoredPosition = new Vector2(0f, 42f);
            spinnerRect.sizeDelta = new Vector2(64f, 64f);
            spinnerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var spinnerImage = spinner.GetComponent<Image>();
            spinnerImage.color = new Color(0.78f, 0.58f, 0.3f, 1f);
            spinnerImage.raycastTarget = false;
            return spinnerRect;
        }

        private TMP_Text CreateLoadingLabel()
        {
            var labelObject = new GameObject(
                "OpeningLoadingLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.15f, 0.5f);
            labelRect.anchorMax = new Vector2(0.85f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, -48f);
            labelRect.sizeDelta = new Vector2(0f, 80f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 34f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
