using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    [DisallowMultipleComponent]
    public sealed class AudioWordConsensusMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private AudioWordConsensusMinigameSession audioWordConsensusMinigameSession;
        [SerializeField] private AudioSource localAudioSource;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text roundLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private Text localWordLabel;
        [SerializeField] private Button playSoundButton;
        [SerializeField] private Text playSoundButtonLabel;
        [SerializeField] private Button submitWordButton;
        [SerializeField] private Text submitWordButtonLabel;

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

            if (submitWordButton != null)
            {
                submitWordButton.onClick.RemoveListener(HandleSubmitWordPressed);
            }

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
            var hasAssignedWord = TypedSession.TryGetAssignedWordForLocalPlayer(out var assignedWord);
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
                    : (hasAssignedWord ? $"Tu palabra es: {assignedWord}" : "Esperando a que el host prepare tu palabra.");
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

            if (submitWordButton != null)
            {
                submitWordButton.gameObject.SetActive(!isEmitter && hasAssignedWord);
                submitWordButton.interactable = TypedSession.CanLocalSubmitAssignedWord();
            }

            if (submitWordButtonLabel != null)
            {
                submitWordButtonLabel.text = hasAssignedWord ? $"Pulsar \"{assignedWord}\"" : "Palabra pendiente";
            }
        }

        private void RegisterLocalButtons()
        {
            if (playSoundButton != null)
            {
                playSoundButton.onClick.RemoveListener(HandlePlaySoundPressed);
                playSoundButton.onClick.AddListener(HandlePlaySoundPressed);
            }

            if (submitWordButton != null)
            {
                submitWordButton.onClick.RemoveListener(HandleSubmitWordPressed);
                submitWordButton.onClick.AddListener(HandleSubmitWordPressed);
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

        private void HandleSubmitWordPressed()
        {
            TypedSession?.SubmitLocalAssignedWord();
        }

        private void HandleStateChanged()
        {
            RefreshGameplay();
        }
    }
}
