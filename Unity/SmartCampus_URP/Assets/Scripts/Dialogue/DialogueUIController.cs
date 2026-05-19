using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueSystemConfig dialogueSystemConfig;
        [SerializeField] private DialoguePanelView dialoguePanelView;

        private DialogueCatalogService dialogueCatalog;
        private DialogueSequencePlayer dialogueSequencePlayer;
        private DialogueLanguageSettingsService languageSettingsService;
        private Coroutine typewriterCoroutine;
        private bool initialized;
        private bool isTypewriterRunning;

        public IReadOnlyList<string> AvailableSequenceKeys => dialogueCatalog == null
            ? Array.Empty<string>()
            : dialogueCatalog.SequenceKeys;

        public DialogueLanguage CurrentLanguage => languageSettingsService == null
            ? dialogueSystemConfig == null ? DialogueLanguage.Spanish : dialogueSystemConfig.DefaultLanguage
            : languageSettingsService.CurrentLanguage;

        public event Action<string> SequenceCompleted;

        private void Awake()
        {
            ResolveReferences();
            if (!TryInitialize())
            {
                enabled = false;
                return;
            }

            dialoguePanelView.Clear();
            dialoguePanelView.SetVisible(false);
        }

        private void OnEnable()
        {
            RegisterPanelCallbacks();
        }

        private void OnDisable()
        {
            UnregisterPanelCallbacks();
            StopTypewriter();
            if (dialoguePanelView != null)
            {
                dialoguePanelView.StopVoiceClip();
            }
        }

        private void OnDestroy()
        {
            if (dialogueSequencePlayer != null)
            {
                dialogueSequencePlayer.SequenceStarted -= HandleSequenceStarted;
                dialogueSequencePlayer.LineChanged -= HandleLineChanged;
                dialogueSequencePlayer.SequenceCompleted -= HandleSequenceCompleted;
                dialogueSequencePlayer.Closed -= HandleDialogueClosed;
            }

            if (languageSettingsService != null)
            {
                languageSettingsService.LanguageChanged -= HandleLanguageChanged;
            }
        }

        public bool PlaySequence(string actLocationKey)
        {
            if (!TryInitialize())
            {
                return false;
            }

            var success = dialogueSequencePlayer.PlaySequence(actLocationKey);
            if (!success)
            {
                Debug.LogWarning($"Dialogue sequence '{actLocationKey}' could not be resolved.", this);
            }

            return success;
        }

        public bool PlayLine(string stringId)
        {
            if (!TryInitialize())
            {
                return false;
            }

            var success = dialogueSequencePlayer.PlayLine(stringId);
            if (!success)
            {
                Debug.LogWarning($"Dialogue line '{stringId}' could not be resolved.", this);
            }

            return success;
        }

        public void Advance()
        {
            if (!TryInitialize())
            {
                return;
            }

            if (isTypewriterRunning && dialogueSystemConfig.RevealFullLineOnAdvanceDuringTypewriter)
            {
                RevealCurrentLineInstantly();
                return;
            }

            if (dialogueSequencePlayer.IsCompleted)
            {
                if (dialogueSystemConfig.ClosePanelWhenSequenceCompletes)
                {
                    Close();
                }

                return;
            }

            dialogueSequencePlayer.Advance();
        }

        public void Close()
        {
            if (!TryInitialize())
            {
                return;
            }

            dialogueSequencePlayer.Close();
        }

        public void SetLanguage(DialogueLanguage language)
        {
            if (!TryInitialize())
            {
                return;
            }

            languageSettingsService.SetLanguage(language);
        }

        private void ResolveReferences()
        {
            dialoguePanelView ??= GetComponentInChildren<DialoguePanelView>(includeInactive: true);
        }

        private bool TryInitialize()
        {
            if (initialized)
            {
                return true;
            }

            if (dialogueSystemConfig == null)
            {
                Debug.LogError($"{nameof(DialogueUIController)} requires a {nameof(DialogueSystemConfig)} reference.", this);
                return false;
            }

            if (dialoguePanelView == null)
            {
                Debug.LogError($"{nameof(DialogueUIController)} requires a {nameof(DialoguePanelView)} reference.", this);
                return false;
            }

            if (dialogueSystemConfig.DialogueCsvAsset == null)
            {
                Debug.LogError($"{nameof(DialogueSystemConfig)} requires a dialogue CSV asset.", this);
                return false;
            }

            if (!DialogueCatalogService.TryCreate(dialogueSystemConfig.DialogueCsvAsset.text, out dialogueCatalog, out var errorMessage))
            {
                Debug.LogError($"Dialogue catalog could not be built. {errorMessage}", this);
                return false;
            }

            languageSettingsService = new DialogueLanguageSettingsService(dialogueSystemConfig.DefaultLanguage);
            languageSettingsService.LanguageChanged += HandleLanguageChanged;

            dialogueSequencePlayer = new DialogueSequencePlayer(dialogueCatalog);
            dialogueSequencePlayer.SequenceStarted += HandleSequenceStarted;
            dialogueSequencePlayer.LineChanged += HandleLineChanged;
            dialogueSequencePlayer.SequenceCompleted += HandleSequenceCompleted;
            dialogueSequencePlayer.Closed += HandleDialogueClosed;

            initialized = true;
            return true;
        }

        private void RegisterPanelCallbacks()
        {
            if (dialoguePanelView == null)
            {
                return;
            }

            dialoguePanelView.AdvanceRequested += Advance;
        }

        private void UnregisterPanelCallbacks()
        {
            if (dialoguePanelView == null)
            {
                return;
            }

            dialoguePanelView.AdvanceRequested -= Advance;
        }

        private void HandleSequenceStarted(DialogueSequenceDefinition _)
        {
            dialoguePanelView.SetVisible(true);
        }

        private void HandleLineChanged(DialogueLineDefinition line)
        {
            ApplyLine(line);
        }

        private void HandleSequenceCompleted(DialogueSequenceDefinition sequence)
        {
            SequenceCompleted?.Invoke(sequence.Key);
            if (dialogueSystemConfig.ClosePanelWhenSequenceCompletes)
            {
                Close();
            }
        }

        private void HandleDialogueClosed()
        {
            StopTypewriter();
            dialoguePanelView.Clear();
            dialoguePanelView.SetVisible(false);
        }

        private void HandleLanguageChanged(DialogueLanguage _)
        {
            if (dialogueSequencePlayer == null || dialogueSequencePlayer.CurrentLine == null)
            {
                return;
            }

            ApplyLine(dialogueSequencePlayer.CurrentLine);
        }

        private void ApplyLine(DialogueLineDefinition line)
        {
            if (line == null)
            {
                return;
            }

            var portrait = dialogueSystemConfig.CharacterPortraitDatabase == null
                ? null
                : dialogueSystemConfig.CharacterPortraitDatabase.GetPortraitOrFallback(line.CharacterId);
            var portraitVisualPrefab = dialogueSystemConfig.CharacterPortraitDatabase == null
                ? null
                : dialogueSystemConfig.CharacterPortraitDatabase.GetPortraitVisualPrefabOrNull(line.CharacterId);
            var speakerName = dialogueSystemConfig.CharacterPortraitDatabase == null
                ? (string.IsNullOrWhiteSpace(line.CharacterId) ? "Narrador" : line.CharacterId)
                : dialogueSystemConfig.CharacterPortraitDatabase.GetDisplayNameOrFallback(line.CharacterId);

            dialoguePanelView.SetSpeaker(speakerName, portrait, portraitVisualPrefab);
            dialoguePanelView.PlayVoiceClip(dialogueSystemConfig.PlayAudioOnLineChanged
                ? dialogueSystemConfig.DialogueAudioDatabase == null
                    ? null
                    : dialogueSystemConfig.DialogueAudioDatabase.GetClipOrNull(line.StringId)
                : null);

            var localizedText = line.GetText(CurrentLanguage);
            StartTypewriter(localizedText);
        }

        private void StartTypewriter(string text)
        {
            StopTypewriter();
            dialoguePanelView.SetDialogueText(text);

            var textLabel = dialoguePanelView.DialogueTextLabel;
            if (textLabel == null)
            {
                return;
            }

            if (!dialogueSystemConfig.UseTypewriterEffect || string.IsNullOrEmpty(text))
            {
                textLabel.maxVisibleCharacters = int.MaxValue;
                isTypewriterRunning = false;
                dialoguePanelView.SetPortraitSpeaking(false);
                return;
            }

            dialoguePanelView.SetPortraitSpeaking(true);
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(textLabel, text));
        }

        private IEnumerator TypewriterRoutine(TMP_Text textLabel, string text)
        {
            isTypewriterRunning = true;
            textLabel.maxVisibleCharacters = 0;

            var visibleCharacters = 0f;
            var charactersPerSecond = Mathf.Max(1f, dialogueSystemConfig.TypewriterCharactersPerSecond);
            while (visibleCharacters < text.Length)
            {
                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                textLabel.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, text.Length);
                yield return null;
            }

            textLabel.maxVisibleCharacters = int.MaxValue;
            isTypewriterRunning = false;
            typewriterCoroutine = null;
            dialoguePanelView.SetPortraitSpeaking(false);
        }

        private void RevealCurrentLineInstantly()
        {
            StopTypewriter();
            if (dialoguePanelView != null && dialoguePanelView.DialogueTextLabel != null)
            {
                dialoguePanelView.DialogueTextLabel.maxVisibleCharacters = int.MaxValue;
            }
        }

        private void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            isTypewriterRunning = false;
            if (dialoguePanelView != null)
            {
                dialoguePanelView.ResetDialogueTextVisibility();
                dialoguePanelView.SetPortraitSpeaking(false);
            }
        }
    }
}
