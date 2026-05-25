using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessHistoryRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text guessedByLabel;
        [SerializeField] private TMP_Text plantNameLabel;
        [SerializeField] private Image plantImage;
        [SerializeField] private GameObject plantImagePlaceholder;
        [SerializeField] [FormerlySerializedAs("leafPersistenceCell")] private Image plantTypeCell;
        [SerializeField] [FormerlySerializedAs("leafPersistenceLabel")] private TMP_Text plantTypeLabel;
        [SerializeField] [FormerlySerializedAs("leafSizeCell")] private Image surfaceRoughnessCell;
        [SerializeField] [FormerlySerializedAs("leafSizeLabel")] private TMP_Text surfaceRoughnessLabel;
        [SerializeField] private Image leafPersistenceCell;
        [SerializeField] private TMP_Text leafPersistenceLabel;
        [SerializeField] [FormerlySerializedAs("leafTextureCell")] private Image leafTypeCell;
        [SerializeField] [FormerlySerializedAs("leafTextureLabel")] private TMP_Text leafTypeLabel;
        [SerializeField] private Image fruitCategoryCell;
        [SerializeField] private TMP_Text fruitCategoryLabel;
        [SerializeField] private Image fruitTypeCell;
        [SerializeField] private TMP_Text fruitTypeLabel;

        private bool responsiveLayoutConfigured;
        private Coroutine imageLoadingCoroutine;
        private Sprite runtimeSprite;

        public void Bind(
            CollaborativePlantGuessPlantDefinition plantDefinition,
            int guessingPlayerSlot,
            CollaborativePlantGuessHistoryEntryNetworkState historyEntry,
            CollaborativePlantGuessMinigameConfig config)
        {
            if (plantNameLabel != null)
            {
                plantNameLabel.text = plantDefinition == null
                    ? "Planta no encontrada"
                    : plantDefinition.FullDisplayName;
            }

            EnsureResponsiveLayout();
            BindPlantImage(plantDefinition);

            BindCell(surfaceRoughnessCell, surfaceRoughnessLabel, "Rugosidad", plantDefinition?.SurfaceRoughness, historyEntry.SurfaceRoughnessOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealSurfaceRoughness(historyEntry.AttemptIndex, config));
            BindCell(leafTypeCell, leafTypeLabel, "Forma de hoja", plantDefinition?.LeafType, historyEntry.LeafTypeOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealLeafType(historyEntry.AttemptIndex, config));
            BindCell(fruitCategoryCell, fruitCategoryLabel, "Categoria del fruto", plantDefinition?.FruitCategory, historyEntry.FruitCategoryOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealFruitCategory(historyEntry.AttemptIndex, config));
            BindCell(fruitTypeCell, fruitTypeLabel, "Tipo de fruto", plantDefinition?.FruitType, historyEntry.FruitTypeOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealFruitType(historyEntry.AttemptIndex, config));
            BindCell(leafPersistenceCell, leafPersistenceLabel, "Tipo de hoja", plantDefinition?.LeafPersistence, historyEntry.LeafPersistenceOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealLeafPersistence(historyEntry.AttemptIndex, config));
            BindCell(plantTypeCell, plantTypeLabel, "Tipo de planta", plantDefinition?.PlantType, historyEntry.PlantTypeOutcome, config.VisualSettings, CollaborativePlantGuessHintProgressionService.ShouldRevealPlantType(historyEntry.AttemptIndex, config));

            RefreshLayout();
        }

        private void OnDisable()
        {
            ReleaseRuntimeSprite();
        }

        private void BindCell(
            Image cellImage,
            TMP_Text label,
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
                label.text = isRevealed && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : CollaborativePlantGuessHintProgressionService.LockedValue;
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
            EnsureDynamicComparisonCells();
            ConfigureComparisonRow();
            ConfigureComparisonCell(surfaceRoughnessCell);
            ConfigureComparisonCell(leafTypeCell);
            ConfigureComparisonCell(fruitCategoryCell);
            ConfigureComparisonCell(fruitTypeCell);
            ConfigureComparisonCell(leafPersistenceCell);
            ConfigureComparisonCell(plantTypeCell);
            HideAttemptLabel();
            ConfigureInfoText(plantNameLabel, TextAnchor.UpperLeft, 3);
            ReorderComparisonCells();
            responsiveLayoutConfigured = true;
        }

        private void HideAttemptLabel()
        {
            if (guessedByLabel == null)
            {
                return;
            }

            guessedByLabel.gameObject.SetActive(false);
        }

        private void ConfigureRowRoot()
        {
            var verticalLayout = GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                Destroy(verticalLayout);
            }

            var horizontalLayout = GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout == null)
            {
                horizontalLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            horizontalLayout.padding = new RectOffset(12, 12, 12, 12);
            horizontalLayout.spacing = 10f;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childAlignment = TextAnchor.UpperLeft;

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = 136f;
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
            var infoColumnTransform = plantNameLabel != null
                ? plantNameLabel.transform.parent
                : guessedByLabel != null ? guessedByLabel.transform.parent : null;
            if (infoColumnTransform == null)
            {
                return;
            }

            var infoColumn = infoColumnTransform.gameObject;
            var layoutElement = infoColumn.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = infoColumn.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 196f;
            layoutElement.preferredWidth = 228f;
            layoutElement.flexibleWidth = 0f;
        }

        private void ConfigureComparisonRow()
        {
            var comparisonsRow = ResolveComparisonsRow();
            if (comparisonsRow == null)
            {
                return;
            }

            var layoutElement = comparisonsRow.gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = comparisonsRow.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = -1f;
            layoutElement.flexibleWidth = 1f;

            var gridLayout = comparisonsRow.gameObject.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                Destroy(gridLayout);
            }

            var responsiveGridLayoutController = comparisonsRow.gameObject.GetComponent<ResponsiveGridLayoutController>();
            if (responsiveGridLayoutController != null)
            {
                Destroy(responsiveGridLayoutController);
            }

            var horizontalLayout = comparisonsRow.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout == null)
            {
                horizontalLayout = comparisonsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            horizontalLayout.padding = new RectOffset(0, 0, 0, 0);
            horizontalLayout.spacing = 8f;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childAlignment = TextAnchor.UpperLeft;
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

            layoutElement.minWidth = 84f;
            layoutElement.preferredWidth = 104f;
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
                verticalLayout.padding = new RectOffset(6, 6, 6, 8);
                verticalLayout.spacing = 4f;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = true;
                verticalLayout.childForceExpandHeight = false;
            }

            ConfigureCellHeader(ResolveHeaderLabel(cellImage));
            ConfigureCellValueLabel(ResolveValueLabel(cellImage));
        }

        private static void ConfigureCellHeader(TMP_Text headerLabel)
        {
            if (headerLabel == null)
            {
                return;
            }

            headerLabel.alignment = TextAlignmentOptions.Top;
            headerLabel.enableWordWrapping = true;
            headerLabel.overflowMode = TextOverflowModes.Overflow;
            headerLabel.enableAutoSizing = false;

            var layoutElement = headerLabel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = headerLabel.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 34f;
            layoutElement.preferredHeight = -1f;
        }

        private static void ConfigureCellValueLabel(TMP_Text valueLabel)
        {
            if (valueLabel == null)
            {
                return;
            }

            valueLabel.alignment = TextAlignmentOptions.Top;
            valueLabel.enableWordWrapping = true;
            valueLabel.overflowMode = TextOverflowModes.Overflow;
            valueLabel.enableAutoSizing = false;
            valueLabel.richText = true;

            var layoutElement = valueLabel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = valueLabel.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 36f;
            layoutElement.preferredHeight = -1f;
            layoutElement.flexibleHeight = 0f;
        }

        private static void ConfigureInfoText(TMP_Text label, TextAnchor alignment, int maxLines)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = ConvertAlignment(alignment);
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = false;

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
            if (transform is RectTransform rowRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRectTransform);
            }
        }

        private static TMP_Text ResolveHeaderLabel(Image cellImage)
        {
            if (cellImage == null)
            {
                return null;
            }

            var headerTransform = cellImage.transform.Find("HeaderLabel");
            return headerTransform == null ? null : headerTransform.GetComponent<TMP_Text>();
        }

        private static TMP_Text ResolveValueLabel(Image cellImage)
        {
            if (cellImage == null)
            {
                return null;
            }

            var valueTransform = cellImage.transform.Find("ValueLabel");
            return valueTransform == null ? null : valueTransform.GetComponent<TMP_Text>();
        }

        private Transform ResolveComparisonsRow()
        {
            if (plantTypeCell != null && plantTypeCell.transform.parent != null)
            {
                return plantTypeCell.transform.parent;
            }

            if (surfaceRoughnessCell != null && surfaceRoughnessCell.transform.parent != null)
            {
                return surfaceRoughnessCell.transform.parent;
            }

            return null;
        }

        private void EnsureDynamicComparisonCells()
        {
            var comparisonsRow = ResolveComparisonsRow();
            if (comparisonsRow == null)
            {
                return;
            }

            if (leafPersistenceCell == null)
            {
                leafPersistenceCell = CreateComparisonCell("LeafPersistenceCell", "Hoja perenne/caduca", comparisonsRow);
                leafPersistenceLabel = ResolveValueLabel(leafPersistenceCell);
            }

            if (fruitCategoryCell == null)
            {
                fruitCategoryCell = PromoteLegacyFruitCell(comparisonsRow);
                fruitCategoryLabel = ResolveValueLabel(fruitCategoryCell);
            }

            if (fruitTypeCell == null)
            {
                fruitTypeCell = CreateComparisonCell("FruitTypeCell", "Tipo de fruto", comparisonsRow);
                fruitTypeLabel = ResolveValueLabel(fruitTypeCell);
            }

            ReorderComparisonCells();
        }

        private void ReorderComparisonCells()
        {
            var comparisonsRow = ResolveComparisonsRow();
            if (comparisonsRow == null)
            {
                return;
            }

            SetSiblingIfPresent(surfaceRoughnessCell, 0);
            SetSiblingIfPresent(leafTypeCell, 1);
            SetSiblingIfPresent(fruitCategoryCell, 2);
            SetSiblingIfPresent(fruitTypeCell, 3);
            SetSiblingIfPresent(leafPersistenceCell, 4);
            SetSiblingIfPresent(plantTypeCell, 5);
        }

        private static void SetSiblingIfPresent(Component component, int index)
        {
            if (component != null)
            {
                component.transform.SetSiblingIndex(index);
            }
        }

        private Image PromoteLegacyFruitCell(Transform comparisonsRow)
        {
            var candidate = FindLegacyComparisonCell(comparisonsRow, "FrutoCell");
            candidate ??= FindLegacyComparisonCell(comparisonsRow, "FruitCell");
            candidate ??= FindLegacyComparisonCell(comparisonsRow, "FruitTypeCell");
            if (candidate == null)
            {
                candidate = CreateComparisonCell("FruitCategoryCell", "Categoria del fruto", comparisonsRow);
            }
            else
            {
                candidate.name = "FruitCategoryCell";
                var headerLabel = ResolveHeaderLabel(candidate);
                if (headerLabel != null)
                {
                    headerLabel.text = "Categoria del fruto";
                }
            }

            return candidate;
        }

        private static Image FindLegacyComparisonCell(Transform comparisonsRow, string candidateName)
        {
            for (var childIndex = 0; childIndex < comparisonsRow.childCount; childIndex++)
            {
                var child = comparisonsRow.GetChild(childIndex);
                if (!string.Equals(child.name, candidateName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return child.GetComponent<Image>();
            }

            return null;
        }

        private static Image CreateComparisonCell(string objectName, string headerText, Transform parent)
        {
            var cellObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            cellObject.transform.SetParent(parent, false);

            var headerObject = new GameObject("HeaderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(cellObject.transform, false);
            var headerLabel = headerObject.GetComponent<TextMeshProUGUI>();
            headerLabel.fontSize = 15;
            headerLabel.alignment = TextAlignmentOptions.Top;
            headerLabel.color = new Color(0.16f, 0.2f, 0.18f, 1f);
            headerLabel.enableWordWrapping = true;
            headerLabel.overflowMode = TextOverflowModes.Overflow;
            headerLabel.text = headerText;

            var valueObject = new GameObject("ValueLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            valueObject.transform.SetParent(cellObject.transform, false);
            var valueLabel = valueObject.GetComponent<TextMeshProUGUI>();
            valueLabel.fontSize = 17;
            valueLabel.alignment = TextAlignmentOptions.Top;
            valueLabel.color = new Color(0.1f, 0.12f, 0.11f, 1f);
            valueLabel.enableWordWrapping = true;
            valueLabel.overflowMode = TextOverflowModes.Overflow;
            valueLabel.text = CollaborativePlantGuessHintProgressionService.LockedValue;

            return cellObject.GetComponent<Image>();
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.TopLeft
            };
        }

        private void BindPlantImage(CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            ReleaseRuntimeSprite();

            if (plantImage == null)
            {
                return;
            }

            plantImage.sprite = null;
            plantImage.gameObject.SetActive(false);
            if (plantImagePlaceholder != null)
            {
                plantImagePlaceholder.SetActive(true);
            }

            if (plantDefinition == null)
            {
                return;
            }

            if (plantDefinition.InspectorSprite != null)
            {
                plantImage.sprite = plantDefinition.InspectorSprite;
                plantImage.gameObject.SetActive(true);
                if (plantImagePlaceholder != null)
                {
                    plantImagePlaceholder.SetActive(false);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(plantDefinition.ImagePath))
            {
                return;
            }

            imageLoadingCoroutine = StartCoroutine(CoopMinigameExternalContentService.LoadSpriteAsync(
                plantDefinition.ImagePath,
                (sprite, error) =>
                {
                    imageLoadingCoroutine = null;
                    if (sprite == null)
                    {
                        Debug.LogWarning($"[CollaborativePlantGuess] No se ha podido cargar la imagen de '{plantDefinition.FullDisplayName}'. Error: {error}", this);
                        return;
                    }

                    runtimeSprite = sprite;
                    plantImage.sprite = runtimeSprite;
                    plantImage.gameObject.SetActive(true);
                    if (plantImagePlaceholder != null)
                    {
                        plantImagePlaceholder.SetActive(false);
                    }
                }));
        }

        private void ReleaseRuntimeSprite()
        {
            if (imageLoadingCoroutine != null)
            {
                StopCoroutine(imageLoadingCoroutine);
                imageLoadingCoroutine = null;
            }

            if (plantImage != null)
            {
                plantImage.sprite = null;
            }

            if (runtimeSprite != null)
            {
                Destroy(runtimeSprite);
                runtimeSprite = null;
            }
        }
    }

    public static class CollaborativePlantGuessHintProgressionService
    {
        public const string LockedValue = "?";

        public static bool ShouldRevealSurfaceRoughness(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= 1;
        }

        public static bool ShouldRevealLeafType(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= (config == null ? 1 : config.LeafTypeRevealAttempt);
        }

        public static bool ShouldRevealFruitCategory(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= 1;
        }

        public static bool ShouldRevealFruitType(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= (config == null ? 2 : config.FruitDetailRevealAttempt);
        }

        public static bool ShouldRevealLeafPersistence(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= (config == null ? 4 : config.LeafPersistenceRevealAttempt);
        }

        public static bool ShouldRevealPlantType(int attemptIndex, CollaborativePlantGuessMinigameConfig config)
        {
            return attemptIndex >= (config == null ? 6 : config.PlantTypeRevealAttempt);
        }
    }
}
