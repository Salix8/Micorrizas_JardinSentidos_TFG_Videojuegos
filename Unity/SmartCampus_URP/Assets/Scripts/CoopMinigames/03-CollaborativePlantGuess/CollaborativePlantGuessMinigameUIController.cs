using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private CollaborativePlantGuessMinigameSession collaborativePlantGuessMinigameSession;
        [SerializeField] private CoopSessionProgressSync sessionProgressSync;
        [SerializeField] private CoopMinigameTopPanelView topPanelView;
        [SerializeField] private CoopMinigameBottomPanelView bottomPanelView;

        [Header("Shared Panel Copy")]
        [SerializeField] private string bottomInstructionTitle = "ENCONTRAD EL ARBOL";
        [SerializeField] private string bottomInstructionBody = "Uno escribe el nombre y el juego da pistas. Adivinad cual es.";
        [SerializeField] private float displayedPenaltySeconds;
        [SerializeField] private string teamName;
        [SerializeField] private string roomCode;

        [Header("Legacy Labels")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text attemptsLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_InputField guessInputField;
        [SerializeField] private Button submitGuessButton;
        [SerializeField] private TMP_Text submitGuessButtonLabel;
        [SerializeField] private Transform suggestionRoot;
        [SerializeField] private CollaborativePlantGuessSuggestionEntryView suggestionTemplate;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private CollaborativePlantGuessHistoryRowView historyRowTemplate;
        [SerializeField] private TMP_Text emptyHistoryLabel;

        private readonly List<CollaborativePlantGuessSuggestionEntryView> suggestionViews = new();
        private readonly List<CollaborativePlantGuessHistoryRowView> historyRowViews = new();

        private CollaborativePlantGuessMinigameSession TypedSession => collaborativePlantGuessMinigameSession != null
            ? collaborativePlantGuessMinigameSession
            : Session as CollaborativePlantGuessMinigameSession;

        protected override void Awake()
        {
            collaborativePlantGuessMinigameSession ??= FindFirstObjectByType<CollaborativePlantGuessMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            collaborativePlantGuessMinigameSession ??= FindFirstObjectByType<CollaborativePlantGuessMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged += HandleStateChanged;
            }

            if (guessInputField != null)
            {
                guessInputField.onValueChanged.AddListener(HandleInputValueChanged);
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.onClick.AddListener(HandleSubmitPressed);
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

            if (guessInputField != null)
            {
                guessInputField.onValueChanged.RemoveListener(HandleInputValueChanged);
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.onClick.RemoveListener(HandleSubmitPressed);
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

            var config = TypedSession.MinigameConfig as CollaborativePlantGuessMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            BindSharedPanels(config.DisplayName, TypedSession.RemainingTimeSeconds, config.TimeLimitSeconds);

            if (timerLabel != null)
            {
                timerLabel.text = $"Tiempo restante: {FormatTime(TypedSession.RemainingTimeSeconds)}";
            }

            if (attemptsLabel != null)
            {
                attemptsLabel.text = $"Intentos: {TypedSession.AttemptsUsed}/{config.MaxAttempts}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = BuildStatusMessage(config);
            }

            if (helperLabel != null)
            {
                helperLabel.text = BuildHelperMessage(config);
            }

            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(true);
                hintLabel.text =
                    $"Orden de pistas: rugosidad, tipo de hoja, categoria del fruto, tipo de fruto, hoja perenne o caduca y tipo de planta. " +
                    $"Se desbloquean en los intentos 2, 4 y 6 para tipo de fruto, persistencia y tipo de planta.";
            }

            if (guessInputField != null)
            {
                guessInputField.interactable =
                    TypedSession.Stage == CooperativeMinigameStage.Playing &&
                    string.IsNullOrWhiteSpace(TypedSession.DataLoadError);
            }

            if (submitGuessButtonLabel != null)
            {
                submitGuessButtonLabel.text = "Enviar planta";
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.interactable = TypedSession.CanLocalSubmitGuess(guessInputField == null ? string.Empty : guessInputField.text);
            }

            RefreshSuggestionList(config);
            RefreshHistoryList(config);
        }

        protected override int? GetFailureFeedbackCount()
        {
            if (TypedSession == null)
            {
                return null;
            }

            var historyEntries = TypedSession.GetGuessHistory();
            var incorrectGuessCount = 0;
            for (var index = 0; index < historyEntries.Count; index++)
            {
                if (!historyEntries[index].IsExactPlantMatch)
                {
                    incorrectGuessCount++;
                }
            }

            return incorrectGuessCount;
        }

        private string BuildHelperMessage(CollaborativePlantGuessMinigameConfig config)
        {
            if (!TypedSession.HasLoadedPlantDefinitions)
            {
                return "Cargando listado de plantas para el autocompletado.";
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return TypedSession.DataLoadError;
            }

            var inputText = guessInputField == null ? string.Empty : guessInputField.text;
            var blockReason = TypedSession.GetLocalSubmissionBlockReason(inputText);
            if (blockReason == CollaborativePlantGuessSubmissionBlockReason.WaitingForAnotherPlayer)
            {
                return "Tu dispositivo ya hizo el ultimo intento. Espera a que otro envie una planta.";
            }

            if (blockReason == CollaborativePlantGuessSubmissionBlockReason.InvalidPlantSelection && !string.IsNullOrWhiteSpace(inputText))
            {
                return "Selecciona una planta valida. Puedes buscar por nombre comun, cientifico o sinonimos.";
            }

            return "Busca por nombre comun, cientifico o sinonimos. Cada intento se comparte con 6 pistas para comparar la planta elegida.";
        }

        private string BuildStatusMessage(CollaborativePlantGuessMinigameConfig config)
        {
            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return TypedSession.DataLoadError;
            }

            var sharedStatusMessage = TypedSession.SharedStatusMessage;
            if (TypedSession.Stage != CooperativeMinigameStage.Playing || config == null)
            {
                return sharedStatusMessage;
            }

            if (!sharedStatusMessage.StartsWith("Intento ", System.StringComparison.Ordinal))
            {
                return sharedStatusMessage;
            }

            return TypedSession.HasLocalSubmittedMostRecentGuess()
                ? $"Intento {TypedSession.AttemptsUsed}/{config.MaxAttempts}. Tu dispositivo hizo el ultimo intento; ahora debe responder otro."
                : $"Intento {TypedSession.AttemptsUsed}/{config.MaxAttempts}. Tu dispositivo ya puede volver a responder.";
        }

        private void RefreshSuggestionList(CollaborativePlantGuessMinigameConfig config)
        {
            if (suggestionRoot == null || suggestionTemplate == null || guessInputField == null || !TypedSession.HasLoadedPlantDefinitions)
            {
                return;
            }

            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(
                TypedSession.GetLoadedPlantDefinitions(),
                guessInputField.text,
                config.AutocompleteSuggestionCount);

            for (var index = 0; index < suggestions.Count; index++)
            {
                var suggestionView = GetOrCreateSuggestionView(index);
                suggestionView.gameObject.SetActive(true);
                var suggestion = suggestions[index];
                suggestionView.Bind(suggestion.FullDisplayName, () =>
                {
                    guessInputField.text = suggestion.FullDisplayName;
                    guessInputField.caretPosition = guessInputField.text.Length;
                    RefreshGameplay();
                });
            }

            for (var index = suggestions.Count; index < suggestionViews.Count; index++)
            {
                suggestionViews[index].gameObject.SetActive(false);
            }
        }

        private void RefreshHistoryList(CollaborativePlantGuessMinigameConfig config)
        {
            if (historyRoot == null || historyRowTemplate == null)
            {
                return;
            }

            var historyEntries = TypedSession.GetGuessHistory();
            if (emptyHistoryLabel != null)
            {
                emptyHistoryLabel.gameObject.SetActive(historyEntries.Count == 0);
                emptyHistoryLabel.text = "Todavia no hay intentos compartidos.";
            }

            for (var index = 0; index < historyEntries.Count; index++)
            {
                var historyEntry = historyEntries[historyEntries.Count - 1 - index];
                var historyRowView = GetOrCreateHistoryRowView(index);
                historyRowView.gameObject.SetActive(true);
                historyRowView.transform.SetSiblingIndex(index);

                TypedSession.TryGetPlantDefinition(historyEntry.PlantId.ToString(), out var plantDefinition);
                historyRowView.Bind(
                    plantDefinition,
                    TypedSession.GetPlayerDisplaySlot(historyEntry.GuessingClientId),
                    historyEntry,
                    config);
            }

            for (var index = historyEntries.Count; index < historyRowViews.Count; index++)
            {
                historyRowViews[index].gameObject.SetActive(false);
            }
        }

        private CollaborativePlantGuessSuggestionEntryView GetOrCreateSuggestionView(int index)
        {
            while (suggestionViews.Count <= index)
            {
                var suggestionView = Instantiate(suggestionTemplate, suggestionRoot);
                suggestionView.gameObject.SetActive(true);
                suggestionViews.Add(suggestionView);
            }

            return suggestionViews[index];
        }

        private CollaborativePlantGuessHistoryRowView GetOrCreateHistoryRowView(int index)
        {
            while (historyRowViews.Count <= index)
            {
                var historyRowView = Instantiate(historyRowTemplate, historyRoot);
                historyRowView.gameObject.SetActive(true);
                historyRowViews.Add(historyRowView);
            }

            return historyRowViews[index];
        }

        private void HandleInputValueChanged(string _)
        {
            RefreshGameplay();
        }

        private void HandleSubmitPressed()
        {
            if (guessInputField == null)
            {
                return;
            }

            TypedSession?.SubmitLocalGuess(guessInputField.text);
            guessInputField.text = string.Empty;
            RefreshGameplay();
        }

        private void HandleStateChanged()
        {
            RefreshUi();
        }

        private void BindSharedPanels(string minigameTitle, float remainingSeconds, float totalSeconds)
        {
            if (topPanelView != null)
            {
                topPanelView.Bind(minigameTitle, CalculateGlobalProgress01(), teamName, roomCode);
            }

            if (bottomPanelView != null)
            {
                bottomPanelView.Bind(
                    bottomInstructionTitle,
                    bottomInstructionBody,
                    remainingSeconds,
                    totalSeconds,
                    displayedPenaltySeconds);
            }
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
            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
