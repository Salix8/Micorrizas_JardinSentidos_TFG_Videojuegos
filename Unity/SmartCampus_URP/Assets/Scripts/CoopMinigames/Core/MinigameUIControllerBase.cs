using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    public abstract class MinigameUIControllerBase : MonoBehaviour
    {
        protected readonly struct MinigameUIViewState
        {
            public MinigameUIViewState(bool showTutorialPopup, bool showWaiting, bool showGameplay, bool showResults, string waitingMessage = null)
            {
                ShowTutorialPopup = showTutorialPopup;
                ShowWaiting = showWaiting;
                ShowGameplay = showGameplay;
                ShowResults = showResults;
                WaitingMessage = waitingMessage;
            }

            public bool ShowTutorialPopup { get; }
            public bool ShowWaiting { get; }
            public bool ShowGameplay { get; }
            public bool ShowResults { get; }
            public string WaitingMessage { get; }
        }

        [Header("References")]
        [SerializeField] private CooperativeMinigameBase minigameSession;
        [SerializeField] private TutorialPopupController tutorialPopupController;
        [SerializeField] private MinigameResultView minigameResultView;
        [SerializeField] private MinigameFailureFeedbackController failureFeedbackController;

        [Header("Panels")]
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private GameObject gameplayPanel;

        [Header("Labels")]
        [SerializeField] private TMP_Text waitingStatusLabel;

        private bool hasFailureFeedbackBaseline;
        private int lastFailureFeedbackCount;

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

        protected virtual int? GetFailureFeedbackCount()
        {
            return null;
        }

        protected virtual bool TryResolveViewStateOverride(CooperativeMinigameConfigBase config, out MinigameUIViewState viewState)
        {
            viewState = default;
            return false;
        }

        protected void RefreshUi()
        {
            RefreshView();
        }

        private void ResolveReferences()
        {
            minigameSession ??= FindFirstObjectByType<CooperativeMinigameBase>(FindObjectsInactive.Include);
            tutorialPopupController ??= FindFirstObjectByType<TutorialPopupController>(FindObjectsInactive.Include);
            minigameResultView ??= FindFirstObjectByType<MinigameResultView>(FindObjectsInactive.Include);
            failureFeedbackController ??= GetComponentInChildren<MinigameFailureFeedbackController>(true);
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
            var showGameplay = Session.Stage == CooperativeMinigameStage.Playing;
            var showResults = Session.Stage == CooperativeMinigameStage.Results;
            var showWaiting = !showTutorialPopup && !showGameplay && !showResults;
            var waitingMessage = BuildWaitingMessage();

            if (TryResolveViewStateOverride(config, out var overriddenViewState))
            {
                showTutorialPopup = overriddenViewState.ShowTutorialPopup;
                showWaiting = overriddenViewState.ShowWaiting;
                showGameplay = overriddenViewState.ShowGameplay;
                showResults = overriddenViewState.ShowResults;
                if (!string.IsNullOrWhiteSpace(overriddenViewState.WaitingMessage))
                {
                    waitingMessage = overriddenViewState.WaitingMessage;
                }
            }

            if (tutorialPopupController != null)
            {
                tutorialPopupController.gameObject.SetActive(showTutorialPopup);
                if (showTutorialPopup)
                {
                    tutorialPopupController.Bind(config.TutorialContent);
                    tutorialPopupController.SetDismissButtonsInteractable(!Session.IsLocalTutorialDismissalPending);
                }
            }

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
                waitingStatusLabel.text = waitingMessage;
            }

            if (minigameResultView != null)
            {
                minigameResultView.gameObject.SetActive(showResults);
                if (showResults)
                {
                    minigameResultView.Bind(
                        Session.CurrentResult,
                        Session.GetContinueAfterResultsButtonLabel(),
                        Session.CanLocalPlayerAdvanceAfterResults,
                        Session.RequestAdvanceAfterResults);
                }
            }

            RefreshGameplay();
            RefreshResults();
            RefreshFailureFeedback(showGameplay);
        }

        private void RefreshFailureFeedback(bool showGameplay)
        {
            var currentFailureCount = GetFailureFeedbackCount();
            if (!showGameplay || !currentFailureCount.HasValue)
            {
                hasFailureFeedbackBaseline = false;
                return;
            }

            var normalizedFailureCount = Mathf.Max(0, currentFailureCount.Value);
            if (!hasFailureFeedbackBaseline)
            {
                lastFailureFeedbackCount = normalizedFailureCount;
                hasFailureFeedbackBaseline = true;
                return;
            }

            if (normalizedFailureCount > lastFailureFeedbackCount)
            {
                failureFeedbackController?.PlayFeedback();
            }

            lastFailureFeedbackCount = normalizedFailureCount;
        }
    }
}
