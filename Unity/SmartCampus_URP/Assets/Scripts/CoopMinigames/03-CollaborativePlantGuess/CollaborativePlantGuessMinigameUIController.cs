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
        [SerializeField] private bool overrideBottomInstructionText;
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
        [SerializeField] private ScrollRect suggestionScrollRect;
        [SerializeField] private Transform suggestionRoot;
        [SerializeField] private CollaborativePlantGuessSuggestionEntryView suggestionTemplate;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private CollaborativePlantGuessHistoryRowView historyRowTemplate;
        [SerializeField] private TMP_Text emptyHistoryLabel;

        [Header("Editor Authored Text")]
        [SerializeField] private bool overrideHintLabelText;
        [SerializeField] private bool forceHintLabelVisible;

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

            if (hintLabel != null && overrideHintLabelText)
            {
                if (forceHintLabelVisible)
                {
                    hintLabel.gameObject.SetActive(true);
                }

                hintLabel.text =
                    $"Orden de pistas: rugosidad, tipo de hoja, categoria del fruto, tipo de fruto y tipo de planta. " +
                    $"El tipo de fruto se desbloquea en el intento 2 y el tipo de planta en el intento 4.";
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

            return "Busca por nombre comun, cientifico o sinonimos. Cada intento se comparte con 5 pistas para comparar la planta elegida.";
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
            EnsureSuggestionScrollHierarchy();

            if (suggestionRoot == null || suggestionTemplate == null || guessInputField == null || !TypedSession.HasLoadedPlantDefinitions)
            {
                HideSuggestionRows();
                return;
            }

            var suggestionLimit = config == null
                ? CollaborativePlantGuessMinigameConfig.MaxAutocompleteSuggestions
                : Mathf.Min(config.AutocompleteSuggestionCount, CollaborativePlantGuessMinigameConfig.MaxAutocompleteSuggestions);
            var suggestions = CollaborativePlantGuessAutocompleteService.BuildSuggestions(
                TypedSession.GetLoadedPlantDefinitions(),
                guessInputField.text,
                suggestionLimit);

            suggestionRoot.gameObject.SetActive(true);

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

            RebuildSuggestionScrollLayout();
        }

        private void EnsureSuggestionScrollHierarchy()
        {
            if (suggestionRoot == null)
            {
                return;
            }

            var existingScrollRect = suggestionScrollRect != null
                ? suggestionScrollRect
                : suggestionRoot.GetComponentInParent<ScrollRect>();
            if (existingScrollRect != null && existingScrollRect.content != null)
            {
                suggestionScrollRect = existingScrollRect;
                suggestionRoot = existingScrollRect.content;
                EnsureSuggestionContentLayout(suggestionRoot.gameObject);
                MoveSuggestionViewsToContent(suggestionRoot);
                return;
            }

            if (suggestionRoot is not RectTransform panelRect)
            {
                return;
            }

            var outerLayout = panelRect.GetComponent<VerticalLayoutGroup>();
            if (outerLayout != null)
            {
                outerLayout.enabled = false;
            }

            var panelImage = panelRect.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.raycastTarget = true;
            }

            suggestionScrollRect = panelRect.GetComponent<ScrollRect>();
            if (suggestionScrollRect == null)
            {
                suggestionScrollRect = panelRect.gameObject.AddComponent<ScrollRect>();
            }

            var viewportRect = panelRect.Find("SuggestionViewport") as RectTransform;
            if (viewportRect == null)
            {
                var viewportObject = new GameObject("SuggestionViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportObject.layer = panelRect.gameObject.layer;
                viewportObject.transform.SetParent(panelRect, false);
                viewportRect = viewportObject.GetComponent<RectTransform>();
                var viewportImage = viewportObject.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;
            }

            Stretch(viewportRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));

            var contentRect = viewportRect.Find("SuggestionContent") as RectTransform;
            if (contentRect == null)
            {
                var contentObject = new GameObject("SuggestionContent", typeof(RectTransform));
                contentObject.layer = panelRect.gameObject.layer;
                contentObject.transform.SetParent(viewportRect, false);
                contentRect = contentObject.GetComponent<RectTransform>();
            }

            ConfigureSuggestionContentRect(contentRect);
            EnsureSuggestionContentLayout(contentRect.gameObject);
            MoveSuggestionViewsToContent(contentRect);

            suggestionScrollRect.viewport = viewportRect;
            suggestionScrollRect.content = contentRect;
            suggestionScrollRect.horizontal = false;
            suggestionScrollRect.vertical = true;
            suggestionScrollRect.movementType = ScrollRect.MovementType.Clamped;
            suggestionScrollRect.scrollSensitivity = 24f;
            suggestionRoot = contentRect;
        }

        private void MoveSuggestionViewsToContent(Transform contentRoot)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (suggestionTemplate != null && suggestionTemplate.transform.parent != contentRoot)
            {
                suggestionTemplate.transform.SetParent(contentRoot, false);
                suggestionTemplate.transform.SetAsLastSibling();
            }

            for (var index = 0; index < suggestionViews.Count; index++)
            {
                var suggestionView = suggestionViews[index];
                if (suggestionView != null && suggestionView.transform.parent != contentRoot)
                {
                    suggestionView.transform.SetParent(contentRoot, false);
                }
            }
        }

        private static void EnsureSuggestionContentLayout(GameObject contentObject)
        {
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = contentObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = contentObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureSuggestionContentRect(RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
        }

        private void HideSuggestionRows()
        {
            if (suggestionRoot != null)
            {
                suggestionRoot.gameObject.SetActive(true);
            }

            for (var index = 0; index < suggestionViews.Count; index++)
            {
                if (suggestionViews[index] != null)
                {
                    suggestionViews[index].gameObject.SetActive(false);
                }
            }

            RebuildSuggestionScrollLayout();
        }

        private void RebuildSuggestionScrollLayout()
        {
            if (suggestionRoot is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            if (suggestionScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                suggestionScrollRect.verticalNormalizedPosition = 1f;
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
                EnsureSuggestionScrollHierarchy();
                var suggestionView = Instantiate(suggestionTemplate, suggestionRoot, false);
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
                if (overrideBottomInstructionText)
                {
                    bottomPanelView.Bind(
                        bottomInstructionTitle,
                        bottomInstructionBody,
                        remainingSeconds,
                        totalSeconds,
                        displayedPenaltySeconds);
                }
                else
                {
                    bottomPanelView.BindTimerAndPenalty(remainingSeconds, totalSeconds, displayedPenaltySeconds);
                }
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

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
