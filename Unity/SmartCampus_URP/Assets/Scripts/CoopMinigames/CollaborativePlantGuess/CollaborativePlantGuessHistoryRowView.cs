using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessHistoryRowView : MonoBehaviour
    {
        [SerializeField] private Text guessedByLabel;
        [SerializeField] private Text plantNameLabel;
        [SerializeField] private Image plantImage;
        [SerializeField] private GameObject plantImagePlaceholder;
        [SerializeField] [FormerlySerializedAs("leafPersistenceCell")] private Image plantTypeCell;
        [SerializeField] [FormerlySerializedAs("leafPersistenceLabel")] private Text plantTypeLabel;
        [SerializeField] [FormerlySerializedAs("leafSizeCell")] private Image surfaceRoughnessCell;
        [SerializeField] [FormerlySerializedAs("leafSizeLabel")] private Text surfaceRoughnessLabel;
        [SerializeField] [FormerlySerializedAs("leafTextureCell")] private Image leafTypeCell;
        [SerializeField] [FormerlySerializedAs("leafTextureLabel")] private Text leafTypeLabel;
        [SerializeField] [FormerlySerializedAs("fruitTypeCell")] private Image fruitCell;
        [SerializeField] [FormerlySerializedAs("fruitTypeLabel")] private Text fruitLabel;

        private bool responsiveLayoutConfigured;

        public void Bind(
            CollaborativePlantGuessPlantDefinition plantDefinition,
            int guessingPlayerSlot,
            CollaborativePlantGuessHistoryEntryNetworkState historyEntry,
            CollaborativePlantGuessMinigameConfig config)
        {
            if (guessedByLabel != null)
            {
                guessedByLabel.text = guessingPlayerSlot > 0
                    ? $"Dispositivo {guessingPlayerSlot}"
                    : "Dispositivo compartido";
            }

            if (plantNameLabel != null)
            {
                plantNameLabel.text = $"Intento {historyEntry.AttemptIndex}";
            }

            if (plantImage != null)
            {
                plantImage.gameObject.SetActive(false);
            }

            if (plantImagePlaceholder != null)
            {
                plantImagePlaceholder.SetActive(false);
            }

            EnsureResponsiveLayout();

            BindCell(
                plantTypeCell,
                plantTypeLabel,
                "Tipo de planta",
                CollaborativePlantGuessHintProgressionService.GetPlantTypeDisplayValue(plantDefinition, historyEntry.AttemptIndex, config),
                historyEntry.PlantTypeOutcome,
                config.VisualSettings,
                CollaborativePlantGuessHintProgressionService.ShouldRevealPlantType(historyEntry.AttemptIndex, config));
            BindCell(
                surfaceRoughnessCell,
                surfaceRoughnessLabel,
                "Rugosidad",
                plantDefinition == null ? CollaborativePlantGuessHintProgressionService.LockedValue : plantDefinition.SurfaceRoughness,
                historyEntry.SurfaceRoughnessOutcome,
                config.VisualSettings,
                isRevealed: true);
            BindCell(
                leafTypeCell,
                leafTypeLabel,
                "Tipo de hoja",
                CollaborativePlantGuessHintProgressionService.GetLeafTypeDisplayValue(plantDefinition, historyEntry.AttemptIndex, config),
                historyEntry.LeafTypeOutcome,
                config.VisualSettings,
                CollaborativePlantGuessHintProgressionService.ShouldRevealLeafType(historyEntry.AttemptIndex, config));
            BindCell(
                fruitCell,
                fruitLabel,
                "Fruto",
                CollaborativePlantGuessHintProgressionService.GetFruitDisplayValue(plantDefinition, historyEntry.AttemptIndex, config),
                historyEntry.FruitOutcome,
                config.VisualSettings,
                isRevealed: true);

            RefreshLayout();
        }

        private void BindCell(
            Image cellImage,
            Text label,
            string header,
            string value,
            CollaborativePlantGuessComparisonOutcome outcome,
            CollaborativePlantGuessVisualSettings visualSettings,
            bool isRevealed)
        {
            if (cellImage == null)
            {
                return;
            }

            var headerLabel = ResolveHeaderLabel(cellImage);
            if (headerLabel != null)
            {
                headerLabel.text = header;
            }

            if (label != null)
            {
                label.text = isRevealed ? value : CollaborativePlantGuessHintProgressionService.LockedValue;
            }

            cellImage.color = !isRevealed
                ? visualSettings.NeutralCellColor
                : outcome switch
                {
                    CollaborativePlantGuessComparisonOutcome.Exact => visualSettings.ExactMatchColor,
                    CollaborativePlantGuessComparisonOutcome.Close => visualSettings.CloseMatchColor,
                    _ => visualSettings.IncorrectMatchColor
                };
        }

        private void EnsureResponsiveLayout()
        {
            if (responsiveLayoutConfigured)
            {
                return;
            }

            ConfigureRowRoot();
            ConfigureInfoColumn();
            ConfigureComparisonRow();
            ConfigureComparisonCell(plantTypeCell);
            ConfigureComparisonCell(surfaceRoughnessCell);
            ConfigureComparisonCell(leafTypeCell);
            ConfigureComparisonCell(fruitCell);
            ConfigureInfoText(guessedByLabel, TextAnchor.MiddleLeft, 1);
            ConfigureInfoText(plantNameLabel, TextAnchor.UpperLeft, 2);
            responsiveLayoutConfigured = true;
        }

        private void ConfigureRowRoot()
        {
            var rowLayout = GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                rowLayout.spacing = 14f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
            }

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = 180f;
                layoutElement.preferredHeight = -1f;
                layoutElement.flexibleHeight = 0f;
            }

            var fitter = gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ConfigureInfoColumn()
        {
            if (guessedByLabel == null || guessedByLabel.transform.parent == null)
            {
                return;
            }

            var infoColumn = guessedByLabel.transform.parent.gameObject;
            var layoutElement = infoColumn.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = infoColumn.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 240f;
            layoutElement.preferredWidth = 260f;
            layoutElement.flexibleWidth = 0f;
        }

        private void ConfigureComparisonRow()
        {
            if (plantTypeCell == null || plantTypeCell.transform.parent == null)
            {
                return;
            }

            var comparisonsRow = plantTypeCell.transform.parent.gameObject;
            var layoutElement = comparisonsRow.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = comparisonsRow.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 760f;
            layoutElement.flexibleWidth = 1f;

            var layoutGroup = comparisonsRow.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = 12f;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
            }
        }

        private void ConfigureComparisonCell(Image cellImage)
        {
            if (cellImage == null)
            {
                return;
            }

            var cellObject = cellImage.gameObject;
            var layoutElement = cellObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cellObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 170f;
            layoutElement.preferredWidth = 190f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.preferredHeight = -1f;

            var fitter = cellObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = cellObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var verticalLayout = cellObject.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.padding = new RectOffset(10, 10, 10, 12);
                verticalLayout.spacing = 8f;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = true;
                verticalLayout.childForceExpandHeight = false;
            }

            ConfigureCellHeader(ResolveHeaderLabel(cellImage));
            ConfigureCellValueLabel(ResolveValueLabel(cellImage));
        }

        private static void ConfigureCellHeader(Text headerLabel)
        {
            if (headerLabel == null)
            {
                return;
            }

            headerLabel.alignment = TextAnchor.UpperCenter;
            headerLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            headerLabel.verticalOverflow = VerticalWrapMode.Overflow;
            headerLabel.resizeTextForBestFit = false;

            var layoutElement = headerLabel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = headerLabel.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 24f;
            layoutElement.preferredHeight = -1f;
        }

        private static void ConfigureCellValueLabel(Text valueLabel)
        {
            if (valueLabel == null)
            {
                return;
            }

            valueLabel.alignment = TextAnchor.UpperCenter;
            valueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueLabel.verticalOverflow = VerticalWrapMode.Overflow;
            valueLabel.resizeTextForBestFit = false;
            valueLabel.supportRichText = true;

            var layoutElement = valueLabel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = valueLabel.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = -1f;
            layoutElement.flexibleHeight = 0f;
        }

        private static void ConfigureInfoText(Text label, TextAnchor alignment, int maxLines)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;

            var layoutElement = label.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = label.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = -1f;
            layoutElement.minHeight = 22f * maxLines;
        }

        private void RefreshLayout()
        {
            if (transform is not RectTransform rowRectTransform)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRectTransform);
        }

        private static Text ResolveHeaderLabel(Image cellImage)
        {
            if (cellImage == null)
            {
                return null;
            }

            var headerTransform = cellImage.transform.Find("HeaderLabel");
            return headerTransform == null ? null : headerTransform.GetComponent<Text>();
        }

        private static Text ResolveValueLabel(Image cellImage)
        {
            if (cellImage == null)
            {
                return null;
            }

            var valueTransform = cellImage.transform.Find("ValueLabel");
            return valueTransform == null ? null : valueTransform.GetComponent<Text>();
        }
    }

    public static class CollaborativePlantGuessHintProgressionService
    {
        public const string LockedValue = "?";

        public static bool ShouldRevealLeafType(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= GetLeafTypeRevealAttempt(config);
        }

        public static bool ShouldRevealFruitDetails(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= GetFruitDetailRevealAttempt(config);
        }

        public static bool ShouldRevealPlantType(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= GetPlantTypeRevealAttempt(config);
        }

        public static string GetPlantTypeDisplayValue(CollaborativePlantGuessPlantDefinition plantDefinition, int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return ShouldRevealPlantType(attemptIndex, config) && plantDefinition != null
                ? plantDefinition.PlantType
                : LockedValue;
        }

        public static string GetLeafTypeDisplayValue(CollaborativePlantGuessPlantDefinition plantDefinition, int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return ShouldRevealLeafType(attemptIndex, config) && plantDefinition != null
                ? plantDefinition.LeafType
                : LockedValue;
        }

        public static string GetFruitDisplayValue(CollaborativePlantGuessPlantDefinition plantDefinition, int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            if (plantDefinition == null)
            {
                return LockedValue;
            }

            if (!ShouldRevealFruitDetails(attemptIndex, config) ||
                string.IsNullOrWhiteSpace(plantDefinition.FruitType) ||
                string.Equals(plantDefinition.FruitCategory, plantDefinition.FruitType, System.StringComparison.OrdinalIgnoreCase))
            {
                return plantDefinition.FruitCategory;
            }

            return $"{plantDefinition.FruitCategory} / {plantDefinition.FruitType}";
        }

        private static int GetLeafTypeRevealAttempt(CollaborativePlantGuessMinigameConfig config)
        {
            return config == null ? 3 : config.LeafTypeRevealAttempt;
        }

        private static int GetFruitDetailRevealAttempt(CollaborativePlantGuessMinigameConfig config)
        {
            return config == null ? 5 : config.FruitDetailRevealAttempt;
        }

        private static int GetPlantTypeRevealAttempt(CollaborativePlantGuessMinigameConfig config)
        {
            return config == null ? 7 : config.PlantTypeRevealAttempt;
        }
    }
}
