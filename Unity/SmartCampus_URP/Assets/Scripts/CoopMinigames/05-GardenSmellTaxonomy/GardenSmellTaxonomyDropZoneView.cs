using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    [DisallowMultipleComponent]
    public sealed class GardenSmellTaxonomyDropZoneView : MonoBehaviour
    {
        [SerializeField] private GardenSmellTaxonomyCategory category;
        [SerializeField] private RectTransform zoneTransform;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image accentImage;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Text badgeLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Text emptyStateLabel;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private Text historyEntryTemplate;

        private readonly List<Text> historyEntryPool = new();

        public GardenSmellTaxonomyCategory Category => category;

        private void Awake()
        {
            zoneTransform ??= transform as RectTransform;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return zoneTransform != null && RectTransformUtility.RectangleContainsScreenPoint(zoneTransform, screenPoint, eventCamera);
        }

        public void Bind(IReadOnlyList<GardenSmellTaxonomyClassificationEntryNetworkState> entries, GardenSmellTaxonomyVisualSettings visuals, bool isHighlighted)
        {
            var categoryColor = visuals.GetCategoryColor(category);
            if (panelImage != null)
            {
                panelImage.color = isHighlighted ? visuals.DropHighlightColor : visuals.PanelColor;
            }

            if (accentImage != null)
            {
                accentImage.color = categoryColor;
            }

            if (badgeImage != null)
            {
                badgeImage.color = categoryColor;
            }

            if (badgeLabel != null)
            {
                badgeLabel.text = GardenSmellTaxonomyCategoryLabels.GetBadgeLabel(category);
                badgeLabel.color = Color.white;
            }

            if (titleLabel != null)
            {
                titleLabel.text = GardenSmellTaxonomyCategoryLabels.GetDisplayName(category);
                titleLabel.color = visuals.TitleColor;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = GardenSmellTaxonomyCategoryLabels.GetSupportText(category);
                subtitleLabel.color = visuals.SubtitleColor;
            }

            EnsurePoolCapacity(entries == null ? 0 : entries.Count);

            var visibleCount = entries == null ? 0 : entries.Count;
            for (var index = 0; index < historyEntryPool.Count; index++)
            {
                var entryLabel = historyEntryPool[index];
                var isVisible = index < visibleCount;
                entryLabel.gameObject.SetActive(isVisible);
                if (!isVisible)
                {
                    continue;
                }

                var currentEntry = entries[index];
                entryLabel.text = currentEntry.ScientificName.ToString();
                entryLabel.color = currentEntry.IsCorrect ? visuals.CorrectColor : visuals.IncorrectColor;
            }

            if (emptyStateLabel != null)
            {
                emptyStateLabel.gameObject.SetActive(visibleCount == 0);
                emptyStateLabel.text = "Todavia no hay plantas clasificadas.";
                emptyStateLabel.color = visuals.EmptyStateColor;
            }
        }

        private void EnsurePoolCapacity(int requiredCount)
        {
            if (historyRoot == null || historyEntryTemplate == null)
            {
                return;
            }

            while (historyEntryPool.Count < requiredCount)
            {
                var instance = Instantiate(historyEntryTemplate, historyRoot);
                instance.gameObject.SetActive(false);
                historyEntryPool.Add(instance);
            }
        }
    }
}
