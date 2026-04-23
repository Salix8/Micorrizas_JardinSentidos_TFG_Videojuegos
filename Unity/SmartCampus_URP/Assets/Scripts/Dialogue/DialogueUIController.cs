using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueDatabaseConfig dialogueDatabase;
        [SerializeField] private DialogueSettingsConfig dialogueSettings;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private DialogueTypewriterSoundPlayer soundPlayer;

        [Header("Initial Sequence")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private string localeOverride;
        [SerializeField] private string initialActOrLocation;
        [SerializeField] private string[] initialLineIds = Array.Empty<string>();

        private readonly DialogueSequenceService dialogueService = new();

        public event Action DialogueCompleted;

        private string ActiveLocale => string.IsNullOrWhiteSpace(localeOverride)
            ? (dialogueDatabase == null ? "es-ES" : dialogueDatabase.DefaultLocale)
            : localeOverride.Trim();

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            if (dialogueView != null)
            {
                dialogueView.SetVisible(false);
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayConfiguredInitialSequence();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public bool PlayConfiguredInitialSequence()
        {
            if (initialLineIds != null && initialLineIds.Length > 0)
            {
                return PlayLineIds(initialLineIds);
            }

            return PlayActOrLocation(initialActOrLocation);
        }

        public void PlayConfiguredInitialSequenceFromEvent()
        {
            PlayConfiguredInitialSequence();
        }

        public bool PlayActOrLocation(string actOrLocation)
        {
            if (!CanPlayStoryDialogue() || !CanReadDatabase())
            {
                return false;
            }

            if (!dialogueDatabase.TryGetLinesByActOrLocation(actOrLocation, out var lines, out var errorMessage))
            {
                Debug.LogWarning(errorMessage, this);
                return false;
            }

            return StartSequence(lines);
        }

        public void PlayActOrLocationFromEvent(string actOrLocation)
        {
            PlayActOrLocation(actOrLocation);
        }

        public bool PlayLine(string stringId)
        {
            if (!CanPlayStoryDialogue() || !CanReadDatabase())
            {
                return false;
            }

            if (!dialogueDatabase.TryGetLine(stringId, out var line, out var errorMessage))
            {
                Debug.LogWarning(errorMessage, this);
                return false;
            }

            return StartSequence(new[] { line });
        }

        public void PlayLineFromEvent(string stringId)
        {
            PlayLine(stringId);
        }

        public bool PlayLineIds(IReadOnlyList<string> stringIds)
        {
            if (!CanPlayStoryDialogue() || !CanReadDatabase())
            {
                return false;
            }

            if (!dialogueDatabase.TryGetLinesByIds(stringIds, out var lines, out var errorMessage))
            {
                Debug.LogWarning(errorMessage, this);
                return false;
            }

            return StartSequence(lines);
        }

        public void CloseDialogue()
        {
            dialogueService.Complete();
        }

        private bool StartSequence(IReadOnlyList<DialogueLine> lines)
        {
            if (dialogueView == null)
            {
                Debug.LogWarning("DialogueUIController necesita una DialogueView asignada.", this);
                return false;
            }

            dialogueView.SetVisible(true);
            dialogueService.Start(lines);
            return true;
        }

        private bool CanPlayStoryDialogue()
        {
            return DialogueSettingsConfig.AreStoryDialoguesEnabled(dialogueSettings);
        }

        private bool CanReadDatabase()
        {
            if (dialogueDatabase != null)
            {
                return true;
            }

            Debug.LogWarning("DialogueUIController necesita una DialogueDatabaseConfig asignada.", this);
            return false;
        }

        private void HandleSnapshotChanged(DialogueSequenceSnapshot snapshot)
        {
            if (dialogueView == null)
            {
                return;
            }

            if (!snapshot.HasLine)
            {
                dialogueView.SetVisible(false);
                return;
            }

            var localizedText = dialogueDatabase == null
                ? string.Empty
                : dialogueDatabase.GetLocalizedText(snapshot.CurrentLine, ActiveLocale);

            dialogueView.SetVisible(true);
            dialogueView.Bind(snapshot, localizedText);
        }

        private void HandleSequenceCompleted()
        {
            if (dialogueView != null)
            {
                dialogueView.SetVisible(false);
            }

            DialogueCompleted?.Invoke();
        }

        private void HandleNextRequested()
        {
            dialogueService.MoveNext();
        }

        private void HandlePreviousRequested()
        {
            dialogueService.MovePrevious();
        }

        private void HandleSkipRequested()
        {
            dialogueService.Complete();
        }

        private void HandleCloseRequested()
        {
            dialogueService.Complete();
        }

        private void HandleVisibleCharacterRevealed(string characterName, char visibleCharacter, int visibleCharacterIndex)
        {
            if (soundPlayer != null)
            {
                soundPlayer.PlayForVisibleCharacter(characterName, visibleCharacter, visibleCharacterIndex);
            }
        }

        private void ResolveReferences()
        {
            dialogueView ??= FindFirstObjectByType<DialogueView>(FindObjectsInactive.Include);
            soundPlayer ??= FindFirstObjectByType<DialogueTypewriterSoundPlayer>(FindObjectsInactive.Include);
        }

        private void Subscribe()
        {
            dialogueService.SnapshotChanged += HandleSnapshotChanged;
            dialogueService.SequenceCompleted += HandleSequenceCompleted;

            if (dialogueView == null)
            {
                return;
            }

            dialogueView.NextRequested += HandleNextRequested;
            dialogueView.PreviousRequested += HandlePreviousRequested;
            dialogueView.SkipRequested += HandleSkipRequested;
            dialogueView.CloseRequested += HandleCloseRequested;
            dialogueView.VisibleCharacterRevealed += HandleVisibleCharacterRevealed;
        }

        private void Unsubscribe()
        {
            dialogueService.SnapshotChanged -= HandleSnapshotChanged;
            dialogueService.SequenceCompleted -= HandleSequenceCompleted;

            if (dialogueView == null)
            {
                return;
            }

            dialogueView.NextRequested -= HandleNextRequested;
            dialogueView.PreviousRequested -= HandlePreviousRequested;
            dialogueView.SkipRequested -= HandleSkipRequested;
            dialogueView.CloseRequested -= HandleCloseRequested;
            dialogueView.VisibleCharacterRevealed -= HandleVisibleCharacterRevealed;
        }
    }
}
