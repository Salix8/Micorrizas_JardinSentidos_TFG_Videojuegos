using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    [DisallowMultipleComponent]
    public sealed class GardenSmellTaxonomyMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private GardenSmellTaxonomyMinigameSession gardenSmellTaxonomyMinigameSession;
        [SerializeField] private CoopSessionProgressSync sessionProgressSync;
        [SerializeField] private CoopMinigameTopPanelView topPanelView;
        [SerializeField] private CoopMinigameBottomPanelView bottomPanelView;

        [Header("Shared Panel Copy")]
        [SerializeField] private string bottomInstructionTitle = "CLASIFICAD POR USO";
        [SerializeField] private string bottomInstructionBody = "Arrastrad cada planta hacia decoracion, alimentacion o curacion segun su uso principal.";
        [SerializeField] private float displayedPenaltySeconds;
        [SerializeField] private string teamName;
        [SerializeField] private string roomCode;

        [Header("Legacy Labels")]
        [SerializeField] private GardenSmellTaxonomyPlantCardView plantCardView;
        [SerializeField] private GardenSmellTaxonomyDropZoneView decorationDropZone;
        [SerializeField] private GardenSmellTaxonomyDropZoneView foodDropZone;
        [SerializeField] private GardenSmellTaxonomyDropZoneView healingDropZone;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text sharedScoreLabel;
        [SerializeField] private TMP_Text statusLabel;

        private readonly List<GardenSmellTaxonomyClassificationEntryNetworkState> decorationEntries = new();
        private readonly List<GardenSmellTaxonomyClassificationEntryNetworkState> foodEntries = new();
        private readonly List<GardenSmellTaxonomyClassificationEntryNetworkState> healingEntries = new();
        private GardenSmellTaxonomyCategory? hoveredCategory;

        private GardenSmellTaxonomyMinigameSession TypedSession => gardenSmellTaxonomyMinigameSession != null
            ? gardenSmellTaxonomyMinigameSession
            : Session as GardenSmellTaxonomyMinigameSession;

        protected override void Awake()
        {
            gardenSmellTaxonomyMinigameSession ??= FindFirstObjectByType<GardenSmellTaxonomyMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            base.Awake();
        }

        protected override void OnEnable()
        {
            gardenSmellTaxonomyMinigameSession ??= FindFirstObjectByType<GardenSmellTaxonomyMinigameSession>(FindObjectsInactive.Include);
            sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged += HandleStateChanged;
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

            base.OnDisable();
        }

        protected override string BuildWaitingMessage()
        {
            if (TypedSession == null || !TypedSession.HasLocalTutorialBeenDismissed)
            {
                return base.BuildWaitingMessage();
            }

            if (!TypedSession.HasLoadedDefinitions && string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return "Preparando la secuencia compartida de plantas...";
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                return TypedSession.DataLoadError;
            }

            if (TypedSession.ContentReadyCount < TypedSession.ParticipantCount)
            {
                return $"Esperando imagenes locales: {TypedSession.ContentReadyCount}/{TypedSession.ParticipantCount}";
            }

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override void RefreshGameplay()
        {
            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as GardenSmellTaxonomyMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
                titleLabel.color = config.VisualSettings.TitleColor;
            }

            BindSharedPanels(config.DisplayName, TypedSession.RemainingTimeSeconds, config.TimeLimitSeconds);

            if (timerLabel != null)
            {
                timerLabel.text = $"Tiempo restante: {FormatTime(TypedSession.RemainingTimeSeconds)}";
                timerLabel.color = config.VisualSettings.BodyColor;
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"Plantas clasificadas: {TypedSession.AnsweredPlantCount}/{TypedSession.TotalScheduledPlants}";
                progressLabel.color = config.VisualSettings.BodyColor;
            }

            if (sharedScoreLabel != null)
            {
                sharedScoreLabel.text = $"Aciertos compartidos: {TypedSession.CorrectAnswerCount}   Fallos: {TypedSession.IncorrectAnswerCount}";
                sharedScoreLabel.color = config.VisualSettings.BodyColor;
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.IsNullOrWhiteSpace(TypedSession.DataLoadError)
                    ? TypedSession.SharedStatusMessage
                    : TypedSession.DataLoadError;
                statusLabel.color = config.VisualSettings.SubtitleColor;
            }

            RefreshDropZones(config.VisualSettings);

            if (!TypedSession.HasLoadedDefinitions && string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                plantCardView?.ShowMessage("Cargando plantas", "Se esta preparando la secuencia compartida.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(TypedSession.DataLoadError))
            {
                plantCardView?.ShowMessage("CSV no disponible", TypedSession.DataLoadError);
                return;
            }

            var currentPlant = TypedSession.GetCurrentPlantDefinition();
            if (currentPlant == null)
            {
                plantCardView?.ShowMessage("Secuencia completada", "Espera al resto del grupo o revisa las categorias centrales.");
                return;
            }

            plantCardView?.Bind(
                currentPlant,
                config.VisualSettings,
                TypedSession.CanLocalSubmitClassification(),
                config.TransitionDuration,
                ResolveDropCategory,
                HandleHoveredCategoryChanged,
                HandleClassificationSubmitted);
        }

        protected override int? GetFailureFeedbackCount()
        {
            return TypedSession?.IncorrectAnswerCount;
        }

        private void HandleStateChanged()
        {
            hoveredCategory = null;
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

        private void HandleHoveredCategoryChanged(GardenSmellTaxonomyCategory? category)
        {
            hoveredCategory = category;

            var config = TypedSession != null ? TypedSession.MinigameConfig as GardenSmellTaxonomyMinigameConfig : null;
            if (config != null)
            {
                RefreshDropZones(config.VisualSettings);
            }
        }

        private void HandleClassificationSubmitted(GardenSmellTaxonomyCategory category)
        {
            hoveredCategory = null;
            TypedSession?.SubmitLocalClassification(category);
        }

        private GardenSmellTaxonomyCategory? ResolveDropCategory(Vector2 screenPoint, Camera eventCamera)
        {
            if (decorationDropZone != null && decorationDropZone.ContainsScreenPoint(screenPoint, eventCamera))
            {
                return GardenSmellTaxonomyCategory.Decoration;
            }

            if (foodDropZone != null && foodDropZone.ContainsScreenPoint(screenPoint, eventCamera))
            {
                return GardenSmellTaxonomyCategory.Food;
            }

            if (healingDropZone != null && healingDropZone.ContainsScreenPoint(screenPoint, eventCamera))
            {
                return GardenSmellTaxonomyCategory.Healing;
            }

            return null;
        }

        private void RefreshDropZones(GardenSmellTaxonomyVisualSettings visuals)
        {
            decorationEntries.Clear();
            foodEntries.Clear();
            healingEntries.Clear();

            var historyEntries = TypedSession == null ? null : TypedSession.GetClassificationHistory();
            if (historyEntries != null)
            {
                for (var index = 0; index < historyEntries.Count; index++)
                {
                    var currentEntry = historyEntries[index];
                    switch (currentEntry.CorrectCategory)
                    {
                        case GardenSmellTaxonomyCategory.Decoration:
                            decorationEntries.Add(currentEntry);
                            break;
                        case GardenSmellTaxonomyCategory.Food:
                            foodEntries.Add(currentEntry);
                            break;
                        case GardenSmellTaxonomyCategory.Healing:
                            healingEntries.Add(currentEntry);
                            break;
                    }
                }
            }

            decorationDropZone?.Bind(decorationEntries, visuals, hoveredCategory == GardenSmellTaxonomyCategory.Decoration);
            foodDropZone?.Bind(foodEntries, visuals, hoveredCategory == GardenSmellTaxonomyCategory.Food);
            healingDropZone?.Bind(healingEntries, visuals, hoveredCategory == GardenSmellTaxonomyCategory.Healing);
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
