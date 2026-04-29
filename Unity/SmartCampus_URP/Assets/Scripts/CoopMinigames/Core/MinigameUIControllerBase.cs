using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    public abstract class MinigameUIControllerBase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CooperativeMinigameBase minigameSession;
        [SerializeField] private TutorialPopupController tutorialPopupController;
        [SerializeField] private MinigameResultView minigameResultView;

        [Header("Panels")]
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private GameObject gameplayPanel;

        [Header("Labels")]
        [SerializeField] private TMP_Text waitingStatusLabel;

        protected CooperativeMinigameBase Session => minigameSession;

        protected virtual void Awake()
        {
            ResolveReferences();
        }

        protected virtual void OnEnable()
        {
            ResolveReferences();

            if (minigameSession == null)
            {
                enabled = false;
                return;
            }

            minigameSession.StageChanged += HandleStageChanged;
            minigameSession.TutorialProgressChanged += HandleTutorialProgressChanged;
            minigameSession.ResultPublished += HandleResultPublished;

            if (tutorialPopupController != null)
            {
                tutorialPopupController.Closed += HandleTutorialClosed;
            }

            RefreshView();
        }

        protected virtual void OnDisable()
        {
            if (minigameSession != null)
            {
                minigameSession.StageChanged -= HandleStageChanged;
                minigameSession.TutorialProgressChanged -= HandleTutorialProgressChanged;
                minigameSession.ResultPublished -= HandleResultPublished;
            }

            if (tutorialPopupController != null)
            {
                tutorialPopupController.Closed -= HandleTutorialClosed;
            }
        }

        protected virtual string BuildWaitingMessage()
        {
            return $"Esperando al resto del grupo: {Session.TutorialDismissedCount}/{Session.ParticipantCount}";
        }

        protected virtual void RefreshGameplay()
        {
        }

        protected virtual void RefreshResults()
        {
        }

        private void ResolveReferences()
        {
            minigameSession ??= FindFirstObjectByType<CooperativeMinigameBase>(FindObjectsInactive.Include);
            tutorialPopupController ??= FindFirstObjectByType<TutorialPopupController>(FindObjectsInactive.Include);
            minigameResultView ??= FindFirstObjectByType<MinigameResultView>(FindObjectsInactive.Include);
        }

        private void HandleTutorialClosed()
        {
            Session.DismissLocalTutorial();
            RefreshView();
        }

        private void HandleStageChanged(CooperativeMinigameStage _)
        {
            RefreshView();
        }

        private void HandleTutorialProgressChanged()
        {
            RefreshView();
        }

        private void HandleResultPublished(MinigameResultData _)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            if (Session == null)
            {
                return;
            }

            var config = Session.MinigameConfig;
            var showTutorialPopup = !Session.HasLocalTutorialBeenDismissed && config != null && config.TutorialContent != null;
            if (tutorialPopupController != null)
            {
                tutorialPopupController.gameObject.SetActive(showTutorialPopup);
                if (showTutorialPopup)
                {
                    tutorialPopupController.Bind(config.TutorialContent);
                    tutorialPopupController.SetDismissButtonsInteractable(!Session.IsLocalTutorialDismissalPending);
                }
            }

            var showGameplay = Session.Stage == CooperativeMinigameStage.Playing;
            var showResults = Session.Stage == CooperativeMinigameStage.Results;
            var showWaiting = !showTutorialPopup && !showGameplay && !showResults;

            if (waitingPanel != null)
            {
                waitingPanel.SetActive(showWaiting);
            }

            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(showGameplay);
            }

            if (waitingStatusLabel != null)
            {
                waitingStatusLabel.text = BuildWaitingMessage();
            }

            if (minigameResultView != null)
            {
                minigameResultView.gameObject.SetActive(showResults);
                if (showResults)
                {
                    minigameResultView.Bind(
                        Session.CurrentResult,
                        config == null ? "Volver al mapa" : config.ReturnToMapButtonLabel,
                        Session.CanLocalPlayerReturnToMainMap,
                        Session.RequestReturnToMainMap);
                }
            }

            RefreshGameplay();
            RefreshResults();
        }
    }
}
