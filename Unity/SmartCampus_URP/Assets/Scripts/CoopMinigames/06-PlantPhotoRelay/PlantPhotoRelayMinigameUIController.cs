using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    [DisallowMultipleComponent]
    public sealed class PlantPhotoRelayMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private PlantPhotoRelayMinigameSession plantPhotoRelayMinigameSession;
        [SerializeField] private CoopSessionProgressSync sessionProgressSync;
        [SerializeField] private CoopMinigameTopPanelView topPanelView;
        [SerializeField] private CoopMinigameBottomPanelView bottomPanelView;

        [Header("Shared Panel Copy")]
        [SerializeField] private string bottomInstructionTitle = "FOTOGRAFIAD Y ADIVINAD";
        [SerializeField] private string bottomInstructionBody = "Un dispositivo fotografia la planta guiado por una pista y otro intenta adivinarla.";
        [SerializeField] private float displayedPenaltySeconds;
        [SerializeField] private string teamName;
        [SerializeField] private string roomCode;

        [Header("Legacy Labels")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text phaseLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text clueLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private TMP_Text roleLabel;
        [SerializeField] private TMP_Text photoStateLabel;
        [SerializeField] private Image photoPreviewImage;
        [SerializeField] private TMP_InputField commonNameInputField;
        [SerializeField] private Button captureButton;
        [SerializeField] private TMP_Text captureButtonLabel;
        [SerializeField] private Button confirmPhotographerSelectionButton;
        [SerializeField] private TMP_Text confirmPhotographerSelectionButtonLabel;
        [SerializeField] private Button submitGuessButton;
        [SerializeField] private TMP_Text submitGuessButtonLabel;
        [SerializeField] private Transform suggestionRoot;
        [SerializeField] private PlantPhotoRelaySuggestionEntryView suggestionTemplate;

        private readonly List<PlantPhotoRelaySuggestionEntryView> suggestionViews = new();

        private PlantPhotoRelayMinigameSession TypedSession => plantPhotoRelayMinigameSession != null
            ? plantPhotoRelayMinigameSession
            : Session as PlantPhotoRelayMinigameSession;

        protected override void Awake()
        {
            plantPhotoRelayMinigameSession ??= FindFirstObjectByType<PlantPhotoRelayMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            plantPhotoRelayMinigameSession ??= FindFirstObjectByType<PlantPhotoRelayMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged += HandleStateChanged;
            }

            if (commonNameInputField != null)
            {
                commonNameInputField.onValueChanged.AddListener(HandleInputChanged);
            }

            if (captureButton != null)
            {
                captureButton.onClick.AddListener(HandleCapturePressed);
            }

            if (confirmPhotographerSelectionButton != null)
            {
                confirmPhotographerSelectionButton.onClick.AddListener(HandlePhotographerSelectionPressed);
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.onClick.AddListener(HandleSubmitGuessPressed);
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

            if (commonNameInputField != null)
            {
                commonNameInputField.onValueChanged.RemoveListener(HandleInputChanged);
            }

            if (captureButton != null)
            {
                captureButton.onClick.RemoveListener(HandleCapturePressed);
            }

            if (confirmPhotographerSelectionButton != null)
            {
                confirmPhotographerSelectionButton.onClick.RemoveListener(HandlePhotographerSelectionPressed);
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.onClick.RemoveListener(HandleSubmitGuessPressed);
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

            var config = TypedSession.MinigameConfig as PlantPhotoRelayMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            BindSharedPanels(config.DisplayName, TypedSession.RemainingPhaseTimeSeconds, GetActivePhaseDurationSeconds(config));

            if (roundLabel != null)
            {
                roundLabel.text = $"Ronda: {TypedSession.CurrentRoundIndex + 1}/{config.RoundCount}";
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = $"Fase: {BuildPhaseLabel(TypedSession.ActivePhase)}";
            }

            if (timerLabel != null)
            {
                var seconds = Mathf.CeilToInt(TypedSession.RemainingPhaseTimeSeconds);
                timerLabel.text = $"Tiempo: {seconds / 60:00}:{seconds % 60:00}";
            }

            if (clueLabel != null)
            {
                clueLabel.text = $"Pista: {TypedSession.ClueText}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = TypedSession.StatusText;
            }

            if (roleLabel != null)
            {
                roleLabel.text = BuildRoleLabel();
            }

            if (helperLabel != null)
            {
                helperLabel.text = BuildHelperMessage(config);
            }

            if (photoStateLabel != null)
            {
                photoStateLabel.text = TypedSession.HasSharedPhoto
                    ? $"Foto compartida: {TypedSession.PhotographerConfirmedCommonName}"
                    : "Todavia no hay foto compartida.";
            }

            if (photoPreviewImage != null)
            {
                photoPreviewImage.enabled = TypedSession.CachedSharedPhotoTexture != null;
                if (TypedSession.CachedSharedPhotoTexture != null)
                {
                    photoPreviewImage.sprite = Sprite.Create(
                        TypedSession.CachedSharedPhotoTexture,
                        new Rect(0f, 0f, TypedSession.CachedSharedPhotoTexture.width, TypedSession.CachedSharedPhotoTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    photoPreviewImage.preserveAspect = true;
                }
            }

            if (captureButtonLabel != null)
            {
                captureButtonLabel.text = TypedSession.IsLocalPhotoCaptureInProgress ? "Abriendo camara..." : "Hacer foto";
            }

            if (confirmPhotographerSelectionButtonLabel != null)
            {
                confirmPhotographerSelectionButtonLabel.text = "Confirmar planta fotografiada";
            }

            if (submitGuessButtonLabel != null)
            {
                submitGuessButtonLabel.text = "Enviar adivinanza";
            }

            if (captureButton != null)
            {
                captureButton.interactable = TypedSession.CanLocalCapturePhoto();
            }

            if (confirmPhotographerSelectionButton != null)
            {
                confirmPhotographerSelectionButton.interactable = TypedSession.CanLocalConfirmPhotographerSelection(GetCurrentInput());
            }

            if (submitGuessButton != null)
            {
                submitGuessButton.interactable = TypedSession.CanLocalSubmitGuess(GetCurrentInput());
            }

            if (commonNameInputField != null)
            {
                commonNameInputField.interactable = TypedSession.ActivePhase == PlantPhotoRelayPhase.Capture || TypedSession.ActivePhase == PlantPhotoRelayPhase.Guess;
            }

            RefreshSuggestionList(config);
        }

        protected override int? GetFailureFeedbackCount()
        {
            return TypedSession == null ? null : TypedSession.FailureCount;
        }

        private string BuildPhaseLabel(PlantPhotoRelayPhase phase)
        {
            switch (phase)
            {
                case PlantPhotoRelayPhase.Clue:
                    return "Pista";
                case PlantPhotoRelayPhase.Capture:
                    return "Captura";
                case PlantPhotoRelayPhase.Guess:
                    return "Adivinanza";
                case PlantPhotoRelayPhase.RoundResults:
                    return "Resultado";
                default:
                    return "Desconocida";
            }
        }

        private string BuildRoleLabel()
        {
            if (TypedSession.IsLocalPhotographer())
            {
                return "Rol local: Fotografo";
            }

            if (TypedSession.IsLocalGuesser())
            {
                return "Rol local: Adivinador";
            }

            return "Rol local: Observador";
        }

        private string BuildHelperMessage(PlantPhotoRelayMinigameConfig config)
        {
            if (!TypedSession.HasLoadedCatalog)
            {
                return "Cargando el catalogo UJI para el autocompletado.";
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return TypedSession.DataLoadError;
            }

            switch (TypedSession.ActivePhase)
            {
                case PlantPhotoRelayPhase.Clue:
                    return "Lee la pista y prepara la busqueda en grupo.";
                case PlantPhotoRelayPhase.Capture:
                    return TypedSession.IsLocalPhotographer()
                        ? "Haz la foto y confirma la planta con el catalogo."
                        : "Espera a que el dispositivo fotografo capture y confirme la planta.";
                case PlantPhotoRelayPhase.Guess:
                    return TypedSession.IsLocalGuesser()
                        ? "Busca por nombre comun y selecciona una planta valida."
                        : "Espera a que el dispositivo adivinador envie su respuesta.";
                case PlantPhotoRelayPhase.RoundResults:
                    return "Revisad el resultado de la ronda antes de pasar a la siguiente.";
                default:
                    return config.InvalidSelectionMessage;
            }
        }

        private void RefreshSuggestionList(PlantPhotoRelayMinigameConfig config)
        {
            if (suggestionRoot == null || suggestionTemplate == null || TypedSession == null || !TypedSession.HasLoadedCatalog)
            {
                return;
            }

            var suggestions = TypedSession.BuildLocalSuggestions(GetCurrentInput());
            for (var index = 0; index < suggestions.Count; index++)
            {
                var view = GetOrCreateSuggestionView(index);
                view.gameObject.SetActive(true);
                var suggestion = suggestions[index];
                view.Bind(suggestion.DisplayCommonName, () =>
                {
                    commonNameInputField.text = suggestion.DisplayCommonName;
                    commonNameInputField.caretPosition = commonNameInputField.text.Length;
                    RefreshUi();
                });
            }

            for (var index = suggestions.Count; index < suggestionViews.Count; index++)
            {
                suggestionViews[index].gameObject.SetActive(false);
            }
        }

        private PlantPhotoRelaySuggestionEntryView GetOrCreateSuggestionView(int index)
        {
            while (suggestionViews.Count <= index)
            {
                var instance = Instantiate(suggestionTemplate, suggestionRoot, false);
                instance.gameObject.SetActive(false);
                suggestionViews.Add(instance);
            }

            return suggestionViews[index];
        }

        private string GetCurrentInput()
        {
            return commonNameInputField == null ? string.Empty : commonNameInputField.text;
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

        private float GetActivePhaseDurationSeconds(PlantPhotoRelayMinigameConfig config)
        {
            switch (TypedSession.ActivePhase)
            {
                case PlantPhotoRelayPhase.Clue:
                    return config.CluePhaseDurationSeconds;
                case PlantPhotoRelayPhase.Capture:
                    return config.CapturePhaseDurationSeconds;
                case PlantPhotoRelayPhase.Guess:
                    return config.GuessPhaseDurationSeconds;
                case PlantPhotoRelayPhase.RoundResults:
                    return config.ResultsRevealDurationSeconds;
                default:
                    return Mathf.Max(1f, TypedSession.RemainingPhaseTimeSeconds);
            }
        }

        private void HandleInputChanged(string _)
        {
            RefreshUi();
        }

        private void HandleCapturePressed()
        {
            TypedSession?.CapturePhotoLocally();
        }

        private void HandlePhotographerSelectionPressed()
        {
            TypedSession?.SubmitLocalPhotographerSelection(GetCurrentInput());
        }

        private void HandleSubmitGuessPressed()
        {
            TypedSession?.SubmitLocalGuess(GetCurrentInput());
        }
    }
}
