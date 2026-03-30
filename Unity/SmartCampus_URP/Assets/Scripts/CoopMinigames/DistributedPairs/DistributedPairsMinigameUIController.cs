using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private DistributedPairsMinigameSession distributedPairsMinigameSession;
        [SerializeField] private DistributedPairsHandView localHandView;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text sharedStatusLabel;
        [SerializeField] private Text localSelectionLabel;

        private DistributedPairsMinigameSession TypedSession => distributedPairsMinigameSession != null
            ? distributedPairsMinigameSession
            : Session as DistributedPairsMinigameSession;

        protected override void Awake()
        {
            distributedPairsMinigameSession ??= FindFirstObjectByType<DistributedPairsMinigameSession>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            distributedPairsMinigameSession ??= FindFirstObjectByType<DistributedPairsMinigameSession>(FindObjectsInactive.Include);
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

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override void RefreshGameplay()
        {
            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as DistributedPairsMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"Parejas: {TypedSession.MatchedPairCount}/{TypedSession.TotalPairCount}   Errores: {TypedSession.FailedAttemptCount}";
            }

            if (sharedStatusLabel != null)
            {
                sharedStatusLabel.text = TypedSession.SharedStatusMessage;
            }

            if (localSelectionLabel != null)
            {
                var localSelectedCard = TypedSession.GetLocalSelectedCard();
                localSelectionLabel.text = localSelectedCard.HasValue
                    ? $"Carta activa: {config.GetPairDefinition(localSelectedCard.Value.PairId)?.Title}"
                    : "Solo puedes tener una carta activa. Si tocas otra, sustituye la seleccion actual.";
            }

            if (localHandView != null)
            {
                localHandView.Render(
                    TypedSession.GetLocalHandStates(),
                    config,
                    TypedSession.Stage == CooperativeMinigameStage.Playing,
                    TypedSession.TryToggleLocalCardSelection);
            }
        }

        private void HandleStateChanged()
        {
            RefreshGameplay();
        }
    }
}
