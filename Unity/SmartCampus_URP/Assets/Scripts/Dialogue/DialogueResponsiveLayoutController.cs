using TMPro;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DialogueResponsiveLayoutController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform referenceRectTransform;
        [SerializeField] private RectTransform frameRectTransform;
        [SerializeField] private RectTransform portraitStageRectTransform;
        [SerializeField] private RectTransform speakerBadgeRectTransform;
        [SerializeField] private TMP_Text dialogueTextLabel;

        [Header("Runtime Automation")]
        [SerializeField] private bool enableRuntimeAutomation;
        [SerializeField] private bool autoFrameLayout = true;
        [SerializeField] private bool autoPortraitLayout = true;
        [SerializeField] private bool autoSpeakerBadgeLayout = true;
        [SerializeField] private bool autoTextRectLayout = true;

        [Header("Frame")]
        [SerializeField] [Range(0.15f, 0.65f)] private float portraitFrameHeightRatio = 0.32f;
        [SerializeField] [Range(0.15f, 0.65f)] private float landscapeFrameHeightRatio = 0.42f;
        [SerializeField] [Min(120f)] private float minFrameHeight = 260f;
        [SerializeField] [Min(120f)] private float maxFrameHeight = 520f;
        [SerializeField] private Vector2 outerMargins = new(40f, 32f);
        [SerializeField] private Vector2 textPadding = new(44f, 34f);

        [Header("Speaker And Portrait")]
        [SerializeField] [Min(32f)] private float speakerBadgeHeight = 64f;
        [SerializeField] [Min(32f)] private float minPortraitSize = 120f;
        [SerializeField] [Min(32f)] private float maxPortraitSize = 340f;
        [SerializeField] [Min(0f)] private float portraitGap = 12f;

        [Header("Text")]
        [SerializeField] private bool configureTextAppearance = true;
        [SerializeField] private bool useAutoSize = true;
        [SerializeField] [Min(8f)] private float fixedFontSize = 34f;
        [SerializeField] [Min(8f)] private float minFontSize = 20f;
        [SerializeField] [Min(8f)] private float maxFontSize = 34f;
        [SerializeField] private TextWrappingModes textWrappingMode = TextWrappingModes.Normal;
        [SerializeField] private TextOverflowModes textOverflowMode = TextOverflowModes.Ellipsis;

        private Vector2 lastReferenceSize;

        private void Awake()
        {
            ResolveReferences();
            RefreshRuntimeLayout();
        }

        private void LateUpdate()
        {
            if (!enableRuntimeAutomation)
            {
                return;
            }

            ResolveReferences();
            var referenceSize = GetReferenceSize();
            if (referenceSize != lastReferenceSize)
            {
                RefreshLayout();
            }
        }

        private void OnEnable()
        {
            RefreshRuntimeLayout();
        }

        private void OnValidate()
        {
            minFrameHeight = Mathf.Max(120f, minFrameHeight);
            maxFrameHeight = Mathf.Max(minFrameHeight, maxFrameHeight);
            minPortraitSize = Mathf.Max(32f, minPortraitSize);
            maxPortraitSize = Mathf.Max(minPortraitSize, maxPortraitSize);
            fixedFontSize = Mathf.Max(8f, fixedFontSize);
            minFontSize = Mathf.Max(8f, minFontSize);
            maxFontSize = Mathf.Max(minFontSize, maxFontSize);
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshRuntimeLayout();
        }

        private void RefreshRuntimeLayout()
        {
            if (Application.isPlaying && enableRuntimeAutomation)
            {
                RefreshLayout();
            }
        }

        public void RefreshLayout()
        {
            ResolveReferences();
            if (frameRectTransform == null ||
                portraitStageRectTransform == null ||
                speakerBadgeRectTransform == null)
            {
                return;
            }

            var referenceSize = GetReferenceSize();
            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
            {
                return;
            }

            lastReferenceSize = referenceSize;
            var settings = new DialogueResponsiveLayoutSettings(
                portraitFrameHeightRatio,
                landscapeFrameHeightRatio,
                minFrameHeight,
                maxFrameHeight,
                outerMargins,
                textPadding,
                speakerBadgeHeight,
                minPortraitSize,
                maxPortraitSize,
                portraitGap);
            var layout = DialogueResponsiveLayoutService.Calculate(referenceSize, settings);

            if (autoFrameLayout)
            {
                ApplyBottomLeftRect(frameRectTransform, layout.FrameRect);
            }

            if (autoPortraitLayout)
            {
                ApplyBottomLeftRect(portraitStageRectTransform, layout.PortraitRect);
            }

            if (autoSpeakerBadgeLayout)
            {
                ApplyBottomLeftRect(speakerBadgeRectTransform, layout.SpeakerBadgeRect);
            }

            if (dialogueTextLabel != null && dialogueTextLabel.transform is RectTransform textRectTransform)
            {
                if (autoTextRectLayout)
                {
                    var localTextRect = new Rect(
                        layout.TextRect.position - layout.FrameRect.position,
                        layout.TextRect.size);
                    ApplyBottomLeftRect(textRectTransform, localTextRect);
                }

                if (configureTextAppearance)
                {
                    dialogueTextLabel.enableAutoSizing = useAutoSize;
                    if (useAutoSize)
                    {
                        dialogueTextLabel.fontSizeMin = minFontSize;
                        dialogueTextLabel.fontSizeMax = maxFontSize;
                    }
                    else
                    {
                        dialogueTextLabel.fontSize = fixedFontSize;
                    }

                    dialogueTextLabel.textWrappingMode = textWrappingMode;
                    dialogueTextLabel.overflowMode = textOverflowMode;
                }
            }
        }

        private void ResolveReferences()
        {
            referenceRectTransform ??= transform.parent as RectTransform;
            frameRectTransform ??= transform.Find("Frame") as RectTransform;
            portraitStageRectTransform ??= transform.Find("PortraitStage") as RectTransform;
            speakerBadgeRectTransform ??= transform.Find("SpeakerBadge") as RectTransform;
            dialogueTextLabel ??= transform.Find("Frame/DialogueTextLabel")?.GetComponent<TMP_Text>();
        }

        private Vector2 GetReferenceSize()
        {
            if (referenceRectTransform != null)
            {
                return referenceRectTransform.rect.size;
            }

            return transform is RectTransform rectTransform
                ? rectTransform.rect.size
                : new Vector2(Screen.width, Screen.height);
        }

        private static void ApplyBottomLeftRect(RectTransform target, Rect rect)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = Vector2.zero;
            target.anchoredPosition = rect.position;
            target.sizeDelta = rect.size;
            target.localScale = Vector3.one;
        }
    }
}
