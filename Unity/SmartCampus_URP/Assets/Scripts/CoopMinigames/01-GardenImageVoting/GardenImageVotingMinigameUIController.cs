using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    [DisallowMultipleComponent]
    public sealed class GardenImageVotingMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private GardenImageVotingMinigameSession gardenImageVotingMinigameSession;
        [SerializeField] private CoopSessionProgressSync sessionProgressSync;
        [SerializeField] private CoopMinigameTopPanelView topPanelView;
        [SerializeField] private CoopMinigameBottomPanelView bottomPanelView;
        [SerializeField] private GardenImageVotingCardView cardView;
        [SerializeField] private ResponsiveAspectRatioLayoutController cardLayoutController;

        [SerializeField] private float displayedPenaltySeconds;
        [SerializeField] private string teamName;
        [SerializeField] private string roomCode;

        [Header("Legacy Labels")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text completionLabel;

        private GardenImageVotingMinigameSession TypedSession => gardenImageVotingMinigameSession != null
            ? gardenImageVotingMinigameSession
            : Session as GardenImageVotingMinigameSession;

        protected override void Awake()
        {
            gardenImageVotingMinigameSession ??= FindFirstObjectByType<GardenImageVotingMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            gardenImageVotingMinigameSession ??= FindFirstObjectByType<GardenImageVotingMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged += HandleStateChanged;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (TypedSession != null)
            {
                TypedSession.StateChanged -= HandleStateChanged;
            }

            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged -= HandleStateChanged;
            }

            base.OnDisable();
        }

        protected override string BuildWaitingMessage()
        {
            if (TypedSession == null || !TypedSession.HasLocalTutorialBeenDismissed)
            {
                return base.BuildWaitingMessage();
            }

            if (!TypedSession.HasLoadedCardDefinitions && string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return "Preparando el conjunto de imagenes compartidas...";
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return TypedSession.DataLoadError;
            }

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override void RefreshGameplay()
        {
            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as GardenImageVotingMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            ApplyCardLayout(config.CardLayoutSettings);

            if (topPanelView != null)
            {
                topPanelView.Bind(config.DisplayName, CalculateGlobalProgress01(), teamName, roomCode);
            }

            if (bottomPanelView != null)
            {
                var penaltySeconds = displayedPenaltySeconds > 0f
                    ? displayedPenaltySeconds
                    : config.IncorrectAnswerPenaltySeconds;
                bottomPanelView.SetTimer(TypedSession.RemainingTimeSeconds, config.TimeLimitSeconds);
                bottomPanelView.SetPenaltySeconds(penaltySeconds);
            }

            if (timerLabel != null)
            {
                timerLabel.text = $"Tiempo restante: {FormatTime(TypedSession.RemainingTimeSeconds)}";
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = $"Puntos compartidos: {TypedSession.SharedCorrectAnswers}";
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"Respondidas: {TypedSession.SharedAnsweredCount}/{TypedSession.TotalScheduledCards}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.IsNullOrWhiteSpace(TypedSession.DataLoadError)
                    ? TypedSession.SharedStatusMessage
                    : TypedSession.DataLoadError;
            }

            if (completionLabel != null)
            {
                completionLabel.gameObject.SetActive(false);
            }

            if (!TypedSession.HasLoadedCardDefinitions && string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                cardView?.ShowMessage("Cargando datos", "Se esta leyendo el CSV y preparando la secuencia local.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                cardView?.ShowMessage("CSV no disponible", TypedSession.DataLoadError);
                return;
            }

            var localCard = TypedSession.GetLocalCurrentCard();
            if (localCard == null)
            {
                cardView?.ShowMessage(
                    "Secuencia completada",
                    "Ya no te quedan imagenes por revisar en este dispositivo.",
                    showIllustrationPlaceholder: false);
                if (completionLabel != null)
                {
                    completionLabel.gameObject.SetActive(true);
                    completionLabel.text = "Has terminado tus imagenes. Puedes seguir mirando la puntuacion comun mientras termina el resto.";
                }

                return;
            }

            cardView?.Bind(
                localCard,
                config.CardVisualSettings,
                TypedSession.CanLocalPlayerSubmitDecision(),
                config.SwipeThreshold,
                config.TransitionDuration,
                TypedSession.SubmitLocalDecision);
        }

        private void ApplyCardLayout(GardenImageVotingCardLayoutSettings layoutSettings)
        {
            if (cardLayoutController == null && cardView != null)
            {
                cardLayoutController = cardView.GetComponent<ResponsiveAspectRatioLayoutController>();
            }

            if (cardLayoutController == null)
            {
                return;
            }

            cardLayoutController.ConfigureSizing(
                layoutSettings.WidthToHeightRatio,
                layoutSettings.MinSize,
                layoutSettings.MaxSize,
                layoutSettings.OuterMargin);
        }

        protected override int? GetFailureFeedbackCount()
        {
            return TypedSession?.SharedIncorrectAnswers;
        }

        private void HandleStateChanged()
        {
            RefreshUi();
        }

        private float CalculateGlobalProgress01()
        {
            if (sessionProgressSync == null || sessionProgressSync.ConfiguredMinigameCount <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)sessionProgressSync.CompletedCount / sessionProgressSync.ConfiguredMinigameCount);
        }

        private static string FormatTime(float remainingSeconds)
        {
            var clampedSeconds = Mathf.Max(0f, remainingSeconds);
            var totalSeconds = Mathf.CeilToInt(clampedSeconds);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
