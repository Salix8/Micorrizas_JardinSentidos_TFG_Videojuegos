using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopFinalResultsUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private CoopSessionProgressSync coopSessionProgressSync;
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private TMP_Text scoreCardTitleLabel;
        [SerializeField] private TMP_Text averageScoreLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartButtonLabel;
        [SerializeField] private TMP_Text waitingHostLabel;

        [Header("Labels")]
        [SerializeField] private string headerText = "Gracias por jugar";
        [SerializeField] [TextArea(2, 4)] private string helperText =
            "Habeis completado el recorrido cooperativo.\nEsta es la nota final del equipo.";
        [SerializeField] private string scoreCardTitleText = "Nota global del equipo";
        [SerializeField] private string averageTextFormat = "{0:0.0}/10";
        [SerializeField] private string restartButtonText = "Reiniciar partida";
        [SerializeField] private string waitingHostText = "Esperando a que el host reinicie la partida.";

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged += HandleProgressChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            RefreshView();
        }

        private void OnDisable()
        {
            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged -= HandleProgressChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        private void HandleProgressChanged()
        {
            ResolveReferences();
            RefreshView();
        }

        private void HandleRestartClicked()
        {
            ResolveReferences();
            coopSessionCoordinator?.RestartSessionToMainMap();
        }

        private void ResolveReferences()
        {
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            coopSessionProgressSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.SessionProgressSync
                : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);

            headerLabel ??= FindTextByName("HeaderLabel");
            helperLabel ??= FindTextByName("HelperLabel");
            scoreCardTitleLabel ??= FindTextByName("ScoreCardTitleLabel");
            averageScoreLabel ??= FindTextByName("AverageScoreLabel");
            restartButton ??= FindButtonByName("RestartButton");
            restartButtonLabel ??= restartButton != null ? restartButton.GetComponentInChildren<TMP_Text>(true) : null;
            waitingHostLabel ??= FindTextByName("WaitingHostLabel");
        }

        private void RefreshView()
        {
            ResolveReferences();

            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            if (helperLabel != null)
            {
                helperLabel.text = helperText;
            }

            if (scoreCardTitleLabel != null)
            {
                scoreCardTitleLabel.text = scoreCardTitleText;
            }

            if (averageScoreLabel != null)
            {
                var averageScore = coopSessionProgressSync == null ? 0f : coopSessionProgressSync.AverageScoreOutOfTen;
                averageScoreLabel.text = string.Format(averageTextFormat, averageScore);
            }

            var canRestart = coopSessionCoordinator != null &&
                             coopSessionCoordinator.IsSpawned &&
                             coopSessionCoordinator.IsServer &&
                             coopSessionProgressSync != null &&
                             coopSessionProgressSync.AreAllMinigamesCompleted;

            if (restartButtonLabel != null)
            {
                restartButtonLabel.text = restartButtonText;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(canRestart);
                restartButton.interactable = canRestart;
            }

            if (waitingHostLabel != null)
            {
                waitingHostLabel.gameObject.SetActive(!canRestart);
                waitingHostLabel.text = waitingHostText;
            }
        }

        private TMP_Text FindTextByName(string objectName)
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.gameObject.name == objectName)
                {
                    return text;
                }
            }

            return null;
        }

        private Button FindButtonByName(string objectName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button != null && button.gameObject.name == objectName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
