using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueWaitingPanelView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private string waitingText = "Esperando";
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float titleFontSize = 30f;
        [SerializeField] private float progressFontSize = 24f;

        [Header("Spinner")]
        [SerializeField] private Color spinnerColor = new(0.78f, 0.58f, 0.3f, 1f);
        [SerializeField] private float spinnerDegreesPerSecond = 180f;

        private RectTransform spinnerRectTransform;
        private TMP_Text titleLabel;
        private TMP_Text progressLabel;
        private bool initialized;

        private void Update()
        {
            if (spinnerRectTransform != null)
            {
                spinnerRectTransform.Rotate(0f, 0f, -spinnerDegreesPerSecond * Time.unscaledDeltaTime);
            }
        }

        public void SetWaiting(bool visible, int confirmedPlayers, int totalPlayers)
        {
            if (!visible && !initialized)
            {
                gameObject.SetActive(false);
                return;
            }

            EnsureInitialized();
            if (progressLabel != null)
            {
                progressLabel.text = $"{Mathf.Max(0, confirmedPlayers)}/{Mathf.Max(0, totalPlayers)}";
            }

            gameObject.SetActive(visible);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            var background = GetComponent<Image>();
            background.raycastTarget = false;

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            spinnerRectTransform = CreateSpinner();
            titleLabel = CreateLabel(
                "WaitingTitle",
                waitingText,
                titleFontSize,
                TextAlignmentOptions.Center,
                new Vector2(0.22f, 0.52f),
                new Vector2(1f, 1f));
            progressLabel = CreateLabel(
                "WaitingProgress",
                "0/0",
                progressFontSize,
                TextAlignmentOptions.Center,
                new Vector2(0.22f, 0f),
                new Vector2(1f, 0.52f));
            initialized = true;
        }

        private RectTransform CreateSpinner()
        {
            var spinner = new GameObject("WaitingSpinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spinner.transform.SetParent(transform, false);
            var spinnerRect = spinner.GetComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.anchoredPosition = new Vector2(42f, 0f);
            spinnerRect.sizeDelta = new Vector2(28f, 28f);
            spinnerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var spinnerImage = spinner.GetComponent<Image>();
            spinnerImage.color = spinnerColor;
            spinnerImage.raycastTarget = false;
            return spinnerRect;
        }

        private TMP_Text CreateLabel(
            string objectName,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            var rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = new Vector2(8f, 6f);
            rectTransform.offsetMax = new Vector2(-12f, -6f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = textColor;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
