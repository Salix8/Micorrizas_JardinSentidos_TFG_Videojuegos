using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialoguePanelView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform portraitVisualRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text speakerNameLabel;
        [SerializeField] private TMP_Text dialogueTextLabel;
        [SerializeField] private AudioSource voiceAudioSource;

        [Header("Portrait Layout")]
        [SerializeField] private Vector2 portraitVisualAnchoredPosition = new(-257f, 710f);
        [SerializeField] private Vector2 portraitVisualSize = new(260f, 260f);

        public TMP_Text DialogueTextLabel => dialogueTextLabel;
        public event Action AdvanceRequested;

        private GameObject activePortraitVisual;
        private bool activePortraitVisualIsOwnedInstance;
        private IDialoguePortraitSpeechAnimator activePortraitSpeechAnimator;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }
        }

        public void SetVisible(bool isVisible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isVisible);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelRoot == null || !panelRoot.activeInHierarchy)
            {
                return;
            }

            AdvanceRequested?.Invoke();
        }

        public void SetSpeaker(string speakerName, Sprite portrait, GameObject portraitVisualPrefab)
        {
            if (speakerNameLabel != null)
            {
                speakerNameLabel.text = speakerName ?? string.Empty;
                speakerNameLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portraitVisualPrefab == null && portrait != null;
            }

            SetPortraitVisual(portraitVisualPrefab);
        }

        public void SetDialogueText(string dialogueText)
        {
            if (dialogueTextLabel != null)
            {
                dialogueTextLabel.text = dialogueText ?? string.Empty;
            }
        }

        public void ResetDialogueTextVisibility()
        {
            if (dialogueTextLabel != null)
            {
                dialogueTextLabel.maxVisibleCharacters = int.MaxValue;
            }
        }

        public void SetPortraitVisual(GameObject portraitVisualPrefab)
        {
            if (activePortraitVisual != null)
            {
                if (activePortraitVisualIsOwnedInstance)
                {
                    Destroy(activePortraitVisual);
                }
                else
                {
                    activePortraitVisual.SetActive(false);
                }

                activePortraitVisual = null;
                activePortraitVisualIsOwnedInstance = false;
            }

            if (portraitVisualRoot == null || portraitVisualPrefab == null)
            {
                activePortraitSpeechAnimator = null;
                return;
            }

            if (portraitVisualPrefab.GetComponent<RectTransform>() == null)
            {
                Debug.LogWarning(
                    $"Portrait visual prefab '{portraitVisualPrefab.name}' was skipped because it is not a UI prefab with RectTransform. " +
                    "Use a UI-compatible portrait prefab for dialogue overlays.",
                    this);
                activePortraitSpeechAnimator = null;
                return;
            }

            activePortraitVisual = FindExistingPortraitVisual(portraitVisualPrefab.name);
            if (activePortraitVisual != null)
            {
                activePortraitVisual.SetActive(true);
                activePortraitVisualIsOwnedInstance = false;
            }
            else
            {
                activePortraitVisual = Instantiate(portraitVisualPrefab, portraitVisualRoot);
                activePortraitVisual.name = portraitVisualPrefab.name;
                activePortraitVisualIsOwnedInstance = true;
            }

            activePortraitSpeechAnimator = FindPortraitSpeechAnimator(activePortraitVisual);

            if (activePortraitVisual.transform is RectTransform visualRect)
            {
                visualRect.anchorMin = new Vector2(0.5f, 0f);
                visualRect.anchorMax = new Vector2(0.5f, 0f);
                visualRect.pivot = new Vector2(0.5f, 0f);
                visualRect.sizeDelta = portraitVisualSize;
                visualRect.anchoredPosition = portraitVisualAnchoredPosition;
                visualRect.localScale = Vector3.one;
                visualRect.localRotation = Quaternion.identity;
            }
            else
            {
                activePortraitVisual.transform.localPosition = Vector3.zero;
                activePortraitVisual.transform.localRotation = Quaternion.identity;
                activePortraitVisual.transform.localScale = Vector3.one;
            }
        }

        public void SetPortraitSpeaking(bool isSpeaking)
        {
            activePortraitSpeechAnimator?.SetSpeaking(isSpeaking);
        }

        public void PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource == null)
            {
                return;
            }

            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            if (clip != null)
            {
                voiceAudioSource.Play();
            }
        }

        public void StopVoiceClip()
        {
            if (voiceAudioSource == null)
            {
                return;
            }

            voiceAudioSource.Stop();
            voiceAudioSource.clip = null;
        }

        public void Clear()
        {
            SetSpeaker(string.Empty, null, null);
            SetDialogueText(string.Empty);
            ResetDialogueTextVisibility();
            StopVoiceClip();
            SetPortraitSpeaking(false);
        }

        private static IDialoguePortraitSpeechAnimator FindPortraitSpeechAnimator(GameObject portraitVisual)
        {
            if (portraitVisual == null)
            {
                return null;
            }

            var behaviours = portraitVisual.GetComponentsInChildren<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IDialoguePortraitSpeechAnimator speechAnimator)
                {
                    return speechAnimator;
                }
            }

            return null;
        }

        private GameObject FindExistingPortraitVisual(string prefabName)
        {
            if (portraitVisualRoot == null || string.IsNullOrWhiteSpace(prefabName))
            {
                return null;
            }

            for (var index = 0; index < portraitVisualRoot.childCount; index++)
            {
                var child = portraitVisualRoot.GetChild(index);
                if (child != null && string.Equals(child.name, prefabName, StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
