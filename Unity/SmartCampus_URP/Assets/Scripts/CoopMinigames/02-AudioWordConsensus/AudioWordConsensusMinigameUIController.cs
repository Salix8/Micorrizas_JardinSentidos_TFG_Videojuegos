using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    [DisallowMultipleComponent]
    public sealed class AudioWordConsensusMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private AudioWordConsensusMinigameSession audioWordConsensusMinigameSession;
        [SerializeField] private AudioSource localAudioSource;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text roleLabel;
        [SerializeField] private TMP_Text localWordLabel;
        [SerializeField] private Button playSoundButton;
        [SerializeField] private TMP_Text playSoundButtonLabel;
        [SerializeField] private Transform wordOptionsContainer;
        [SerializeField] private Button wordOptionButtonTemplate;
        [SerializeField] private Button submitWordButton;
        [SerializeField] private TMP_Text submitWordButtonLabel;

        private readonly List<Button> activeWordOptionButtons = new();

        private AudioWordConsensusMinigameSession TypedSession => audioWordConsensusMinigameSession != null
            ? audioWordConsensusMinigameSession
            : Session as AudioWordConsensusMinigameSession;

        protected override void Awake()
        {
            audioWordConsensusMinigameSession ??= FindFirstObjectByType<AudioWordConsensusMinigameSession>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            audioWordConsensusMinigameSession ??= FindFirstObjectByType<AudioWordConsensusMinigameSession>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            RegisterLocalButtons();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (TypedSession != null)
            {
                TypedSession.StateChanged -= HandleStateChanged;
            }

            if (playSoundButton != null)
            {
                playSoundButton.onClick.RemoveListener(HandlePlaySoundPressed);
            }

            ClearGeneratedWordOptionButtons();
            base.OnDisable();
        }

        protected override string BuildWaitingMessage()
        {
            if (TypedSession == null || !TypedSession.HasLocalTutorialBeenDismissed)
            {
                return base.BuildWaitingMessage();
            }

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override void RefreshGameplay()
        {
            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as AudioWordConsensusMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            if (roundLabel != null)
            {
                var currentRoundDisplay = Mathf.Max(1, TypedSession.ActiveRoundIndex + 1);
                roundLabel.text = $"Ronda: {currentRoundDisplay}/{Mathf.Max(1, TypedSession.TotalScheduledRounds)}";
            }

            if (timerLabel != null)
            {
                var remainingSeconds = Mathf.CeilToInt(TypedSession.RemainingTimeSeconds);
                timerLabel.text = $"Tiempo restante: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = $"Aciertos: {TypedSession.CorrectRoundCount}   Fallos: {TypedSession.IncorrectRoundCount}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = TypedSession.SharedStatusMessage;
            }

            var isEmitter = TypedSession.IsLocalEmitter;
            var hasAssignedWords = TypedSession.TryGetAssignedWordsForLocalPlayer(out var assignedWords);
            var roundDefinition = TypedSession.GetCurrentRoundDefinition();

            if (roleLabel != null)
            {
                roleLabel.text = isEmitter
                    ? "Tu rol en esta ronda: reproducir el sonido para el grupo."
                    : "Tu rol en esta ronda: mostrar tu palabra y decidir en grupo quien debe pulsar.";
            }

            if (localWordLabel != null)
            {
                localWordLabel.text = isEmitter
                    ? "En este turno no recibes palabra. Tu tarea es reproducir el sonido cuando el grupo lo necesite."
                    : (hasAssignedWords ? "Opciones disponibles en tu dispositivo:" : "No hay opciones disponibles para esta ronda.");
            }

            if (playSoundButton != null)
            {
                playSoundButton.gameObject.SetActive(isEmitter);
                playSoundButton.interactable = isEmitter && roundDefinition != null && roundDefinition.SoundClip != null;
            }

            if (playSoundButtonLabel != null)
            {
                playSoundButtonLabel.text = roundDefinition != null && roundDefinition.SoundClip != null
                    ? "Reproducir sonido"
                    : config.MissingAudioClipLabel;
            }

            RefreshWordOptionButtons(isEmitter, hasAssignedWords ? assignedWords : null);
        }

        private void RegisterLocalButtons()
        {
            if (playSoundButton != null)
            {
                playSoundButton.onClick.RemoveListener(HandlePlaySoundPressed);
                playSoundButton.onClick.AddListener(HandlePlaySoundPressed);
            }

        }

        private void RefreshWordOptionButtons(bool isEmitter, IReadOnlyList<string> assignedWords)
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null)
            {
                return;
            }

            var hasAssignedWords = assignedWords != null && assignedWords.Count > 0;
            if (isEmitter || !hasAssignedWords)
            {
                ClearGeneratedWordOptionButtons();
                templateButton.gameObject.SetActive(false);
                RebuildWordOptionsLayout();
                return;
            }

            EnsureWordOptionButtonPool(assignedWords.Count);
            for (var index = 0; index < activeWordOptionButtons.Count; index++)
            {
                var button = activeWordOptionButtons[index];
                var shouldShow = index < assignedWords.Count;
                button.gameObject.SetActive(shouldShow);

                if (!shouldShow)
                {
                    continue;
                }

                var assignedWord = assignedWords[index];
                var label = GetButtonLabel(button);
                if (label != null)
                {
                    label.text = assignedWord;
                }

                button.interactable = TypedSession.CanLocalSubmitAssignedWord();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleSubmitWordPressed(assignedWord));
            }

            RebuildWordOptionsLayout();
        }

        private void EnsureWordOptionButtonPool(int requiredButtonCount)
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null)
            {
                return;
            }

            if (activeWordOptionButtons.Count == 0)
            {
                activeWordOptionButtons.Add(templateButton);
            }

            while (activeWordOptionButtons.Count < requiredButtonCount)
            {
                var instance = Instantiate(templateButton, templateButton.transform.parent);
                instance.name = $"{templateButton.name}_{activeWordOptionButtons.Count + 1}";
                activeWordOptionButtons.Add(instance);
            }
        }

        private Button ResolveWordOptionTemplate()
        {
            if (wordOptionButtonTemplate == null)
            {
                wordOptionButtonTemplate = submitWordButton;
            }

            if (wordOptionButtonTemplate != null && wordOptionsContainer == null)
            {
                wordOptionsContainer = wordOptionButtonTemplate.transform.parent;
            }

            return wordOptionButtonTemplate;
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            return button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        }

        private void ClearGeneratedWordOptionButtons()
        {
            for (var index = activeWordOptionButtons.Count - 1; index >= 1; index--)
            {
                var button = activeWordOptionButtons[index];
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            if (activeWordOptionButtons.Count > 0)
            {
                var templateButton = activeWordOptionButtons[0];
                if (templateButton != null)
                {
                    templateButton.onClick.RemoveAllListeners();
                    templateButton.gameObject.SetActive(false);
                }

                activeWordOptionButtons.Clear();
            }
        }

        private void HandlePlaySoundPressed()
        {
            var roundDefinition = TypedSession == null ? null : TypedSession.GetCurrentRoundDefinition();
            if (roundDefinition == null || roundDefinition.SoundClip == null || localAudioSource == null)
            {
                return;
            }

            localAudioSource.Stop();
            localAudioSource.clip = roundDefinition.SoundClip;
            localAudioSource.Play();
        }

        private void HandleSubmitWordPressed(string selectedWord)
        {
            TypedSession?.SubmitLocalAssignedWord(selectedWord);
        }

        private void HandleStateChanged()
        {
            RefreshGameplay();
        }

        private void RebuildWordOptionsLayout()
        {
            if (wordOptionsContainer is RectTransform containerRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRectTransform);
            }
        }
    }
}
