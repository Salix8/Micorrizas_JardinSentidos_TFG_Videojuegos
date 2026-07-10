using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("badgeImage")]
        [SerializeField] private Image categoryIconImage;
        [SerializeField] private Sprite categoryIconSprite;
        [SerializeField] private TMP_Text badgeLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text emptyStateLabel;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private TMP_Text historyEntryTemplate;

        private readonly List<TMP_Text> historyEntryPool = new();

        public GardenSmellTaxonomyCategory Category => category;

        private void Awake()
        {
            zoneTransform ??= transform as RectTransform;
            ApplyCategoryIconPresentation();
        }

        private void OnValidate()
        {
            zoneTransform ??= transform as RectTransform;
            ApplyCategoryIconPresentation();
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return zoneTransform != null && RectTransformUtility.RectangleContainsScreenPoint(zoneTransform, screenPoint, eventCamera);
        }

        public void Bind(IReadOnlyList<GardenSmellTaxonomyClassificationEntryNetworkState> entries, GardenSmellTaxonomyVisualSettings visuals, bool isHighlighted)
        {
            if (panelImage != null)
            {
                panelImage.color = isHighlighted ? visuals.DropHighlightColor : visuals.PanelColor;
            }

            if (accentImage != null)
            {
                accentImage.gameObject.SetActive(false);
            }

            if (categoryIconImage != null)
            {
                categoryIconImage.sprite = ResolveCategoryIconSprite();
                categoryIconImage.color = Color.white;
                categoryIconImage.preserveAspect = true;
                categoryIconImage.gameObject.SetActive(categoryIconImage.sprite != null);
            }

            if (badgeLabel != null)
            {
                badgeLabel.gameObject.SetActive(false);
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

        private void ApplyCategoryIconPresentation()
        {
            if (accentImage != null)
            {
                accentImage.gameObject.SetActive(false);
            }

            if (badgeLabel != null)
            {
                badgeLabel.gameObject.SetActive(false);
            }

            if (categoryIconImage == null)
            {
                return;
            }

            categoryIconImage.sprite = ResolveCategoryIconSprite();
            categoryIconImage.color = Color.white;
            categoryIconImage.preserveAspect = true;
            categoryIconImage.raycastTarget = false;
        }

        private Sprite ResolveCategoryIconSprite()
        {
            if (categoryIconSprite != null)
            {
                return categoryIconSprite;
            }

            return Resources.Load<Sprite>($"CoopMinigames/GardenSmellTaxonomy/Icons/{GetCategoryIconResourceName(category)}");
        }

        private static string GetCategoryIconResourceName(GardenSmellTaxonomyCategory category)
        {
            return category switch
            {
                GardenSmellTaxonomyCategory.Decoration => "garden-smell-decoration",
                GardenSmellTaxonomyCategory.Food => "garden-smell-food",
                GardenSmellTaxonomyCategory.Healing => "garden-smell-healing",
                _ => "garden-smell-decoration"
            };
        }
    }
}
