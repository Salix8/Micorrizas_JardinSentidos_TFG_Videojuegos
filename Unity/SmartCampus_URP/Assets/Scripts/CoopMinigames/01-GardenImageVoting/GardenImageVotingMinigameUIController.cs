using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    [DisallowMultipleComponent]
    public sealed class GardenImageVotingMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private GardenImageVotingMinigameSession gardenImageVotingMinigameSession;
        [SerializeField] private GardenImageVotingCardView cardView;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text completionLabel;

        private GardenImageVotingMinigameSession TypedSession => gardenImageVotingMinigameSession != null
            ? gardenImageVotingMinigameSession
            : Session as GardenImageVotingMinigameSession;

        protected override void Awake()
        {
            gardenImageVotingMinigameSession ??= FindFirstObjectByType<GardenImageVotingMinigameSession>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            gardenImageVotingMinigameSession ??= FindFirstObjectByType<GardenImageVotingMinigameSession>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (TypedSession != null)
            {
                TypedSession.StateChanged -= HandleStateChanged;
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
                cardView?.ShowMessage("Secuencia completada", "Ya no te quedan imagenes por revisar en este dispositivo.");
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

        private void HandleStateChanged()
        {
            RefreshGameplay();
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
