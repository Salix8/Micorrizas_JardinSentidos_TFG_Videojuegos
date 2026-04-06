using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private CollaborativePlantGuessMinigameSession collaborativePlantGuessMinigameSession;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text attemptsLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text helperLabel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private InputField guessInputField;
        [SerializeField] private Button submitGuessButton;
        [SerializeField] private Text submitGuessButtonLabel;
        [SerializeField] private Transform suggestionRoot;
        [SerializeField] private CollaborativePlantGuessSuggestionEntryView suggestionTemplate;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private CollaborativePlantGuessHistoryRowView historyRowTemplate;
        [SerializeField] private Text emptyHistoryLabel;

        private readonly List<CollaborativePlantGuessSuggestionEntryView> suggestionViews = new();
        private readonly List<CollaborativePlantGuessHistoryRowView> historyRowViews = new();

        private CollaborativePlantGuessMinigameSession TypedSession => collaborativePlantGuessMinigameSession != null
            ? collaborativePlantGuessMinigameSession
            : Session as CollaborativePlantGuessMinigameSession;

        protected override void Awake()
        {
            collaborativePlantGuessMinigameSession ??= FindFirstObjectByType<CollaborativePlantGuessMinigameSession>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            collaborativePlantGuessMinigameSession ??= FindFirstObjectByType<CollaborativePlantGuessMinigameSession>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
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

            if (timerLabel != null)
            {
                var remainingSeconds = Mathf.CeilToInt(TypedSession.RemainingTimeSeconds);
                timerLabel.text = $"Tiempo restante: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
            }

            if (attemptsLabel != null)
            {
                attemptsLabel.text = $"Intentos: {TypedSession.AttemptsUsed}/{config.MaxAttempts}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.IsNullOrWhiteSpace(TypedSession.DataLoadError)
                    ? TypedSession.SharedStatusMessage
                    : TypedSession.DataLoadError;
            }

            if (helperLabel != null)
            {
                helperLabel.text = BuildHelperMessage(config);
            }

            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(true);
                hintLabel.text =
                    $"Pistas: rugosidad y fruto desde el inicio, tipo de hoja en {config.LeafTypeRevealAttempt}, detalle del fruto en {config.FruitDetailRevealAttempt}, tipo de planta en {config.PlantTypeRevealAttempt}.";
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

            return $"Busca por nombre comun, cientifico o sinonimos. El fruto se detalla en el intento {config.FruitDetailRevealAttempt}.";
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
                var historyEntry = historyEntries[index];
                var historyRowView = GetOrCreateHistoryRowView(index);
                historyRowView.gameObject.SetActive(true);

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
            RefreshGameplay();
        }
    }
}
