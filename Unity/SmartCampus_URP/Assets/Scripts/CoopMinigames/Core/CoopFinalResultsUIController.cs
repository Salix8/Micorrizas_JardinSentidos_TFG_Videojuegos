using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopFinalResultsUIController : MonoBehaviour
    {
        [Header("Runtime Session References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private CoopSessionProgressSync coopSessionProgressSync;
        [SerializeField] private RelayConnectionService relayConnectionService;

        [Header("Scene UI References")]
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private TMP_Text scoreCardTitleLabel;
        [SerializeField] private TMP_Text averageScoreLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartButtonLabel;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text exitButtonLabel;
        [SerializeField] private TMP_Text waitingHostLabel;

        [Header("Labels")]
        [SerializeField] private string headerText = "Gracias por jugar";
        [SerializeField] private bool useSceneAuthoredHelperText = true;
        [SerializeField] [TextArea(2, 4)] private string helperText =
            "Habeis completado el recorrido cooperativo.\nEsta es la nota final del equipo.";
        [SerializeField] private string scoreCardTitleText = "Nota global del equipo";
        [SerializeField] private string averageTextFormat = "{0:0.0}/10";
        [SerializeField] private string restartButtonText = "Reiniciar partida";
        [SerializeField] private string exitButtonText = "Salir del juego";
        [SerializeField] private string waitingHostText = "Esperando a que el host reinicie la partida.";

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();

            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged += HandleProgressChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClicked);
                exitButton.onClick.AddListener(HandleExitClicked);
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

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClicked);
            }
        }

        private void HandleProgressChanged()
        {
            ResolveRuntimeReferences();
            RefreshView();
        }

        private void HandleRestartClicked()
        {
            ResolveRuntimeReferences();
            coopSessionCoordinator?.RestartSessionToMainMap();
        }

        private void HandleExitClicked()
        {
            ResolveRuntimeReferences();
            relayConnectionService?.ShutdownSession();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResolveRuntimeReferences()
        {
            // These systems live in the persistent co-op bootstrap and are not authored inside the summary scene asset.
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            coopSessionProgressSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.SessionProgressSync
                : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);
        }

        private void RefreshView()
        {
            ResolveRuntimeReferences();

            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            if (!useSceneAuthoredHelperText && helperLabel != null)
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

            if (exitButtonLabel != null)
            {
                exitButtonLabel.text = exitButtonText;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(canRestart);
                restartButton.interactable = canRestart;
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
                exitButton.interactable = true;
            }

            if (waitingHostLabel != null)
            {
                waitingHostLabel.gameObject.SetActive(!canRestart);
                waitingHostLabel.text = waitingHostText;
            }
        }
    }
}
