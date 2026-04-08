using System;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Garden Smell Taxonomy Config", fileName = "GardenSmellTaxonomyMinigameConfig")]
    public sealed class GardenSmellTaxonomyMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Dataset")]
        [SerializeField] private string csvRelativePath = "CoopMinigames/05-GardenSmellTaxonomy/GardenSmellTaxonomyPlants.csv";
        [SerializeField] [Min(2)] private int minimumSupportedPlayers = 2;
        [SerializeField] [Min(2)] private int maxSupportedDevices = 6;
        [SerializeField] [Min(3)] private int minimumRequiredPlants = 6;
        [SerializeField] [Min(3)] private int maxPlantsPerMatch = 9;
        [SerializeField] private bool shufflePlants = true;

        [Header("Gameplay")]
        [SerializeField] [Min(15f)] private float timeLimitSeconds = 240f;
        [SerializeField] [Min(0.05f)] private float transitionDuration = 0.2f;
        [SerializeField] private string timeoutMessage = "Tiempo agotado";

        [Header("Scoring")]
        [SerializeField] private GardenSmellTaxonomyScoreSettings scoreSettings = GardenSmellTaxonomyScoreSettings.CreateDefault();

        [Header("Visuals")]
        [SerializeField] private GardenSmellTaxonomyVisualSettings visualSettings = GardenSmellTaxonomyVisualSettings.CreateDefault();

        public string CsvRelativePath => csvRelativePath;
        public int MinimumSupportedPlayers => minimumSupportedPlayers;
        public int MaxSupportedDevices => maxSupportedDevices;
        public int MinimumRequiredPlants => minimumRequiredPlants;
        public int MaxPlantsPerMatch => maxPlantsPerMatch;
        public bool ShufflePlants => shufflePlants;
        public float TimeLimitSeconds => timeLimitSeconds;
        public float TransitionDuration => transitionDuration;
        public string TimeoutMessage => timeoutMessage;
        public GardenSmellTaxonomyScoreSettings ScoreSettings => scoreSettings;
        public GardenSmellTaxonomyVisualSettings VisualSettings => visualSettings;

        private void OnValidate()
        {
            minimumSupportedPlayers = Mathf.Max(2, minimumSupportedPlayers);
            maxSupportedDevices = Mathf.Max(minimumSupportedPlayers, maxSupportedDevices);
            minimumRequiredPlants = Mathf.Max(3, minimumRequiredPlants);
            maxPlantsPerMatch = Mathf.Max(minimumRequiredPlants, maxPlantsPerMatch);
            timeLimitSeconds = Mathf.Max(15f, timeLimitSeconds);
            transitionDuration = Mathf.Max(0.05f, transitionDuration);
            scoreSettings.Clamp();
            visualSettings.Clamp();
        }
    }

    [Serializable]
    public struct GardenSmellTaxonomyScoreSettings
    {
        [SerializeField] [Min(1f)] private float maxScore;
        [SerializeField] [Min(0f)] private float minimumScore;
        [SerializeField] [Min(0f)] private float completionWeight;
        [SerializeField] [Min(0f)] private float accuracyWeight;
        [SerializeField] [Range(0, 2)] private int decimalPlaces;

        public float MaxScore => maxScore;
        public float MinimumScore => minimumScore;
        public float CompletionWeight => completionWeight;
        public float AccuracyWeight => accuracyWeight;
        public int DecimalPlaces => decimalPlaces;

        public static GardenSmellTaxonomyScoreSettings CreateDefault()
        {
            return new GardenSmellTaxonomyScoreSettings
            {
                maxScore = 10f,
                minimumScore = 0f,
                completionWeight = 0.35f,
                accuracyWeight = 0.65f,
                decimalPlaces = 1
            };
        }

        public void Clamp()
        {
            maxScore = Mathf.Max(1f, maxScore);
            minimumScore = Mathf.Clamp(minimumScore, 0f, maxScore);
            completionWeight = Mathf.Max(0f, completionWeight);
            accuracyWeight = Mathf.Max(0f, accuracyWeight);
            decimalPlaces = Mathf.Clamp(decimalPlaces, 0, 2);
        }
    }

    [Serializable]
    public struct GardenSmellTaxonomyVisualSettings
    {
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color panelColor;
        [SerializeField] private Color cardColor;
        [SerializeField] private Color cardFrameColor;
        [SerializeField] private Color titleColor;
        [SerializeField] private Color subtitleColor;
        [SerializeField] private Color bodyColor;
        [SerializeField] private Color correctColor;
        [SerializeField] private Color incorrectColor;
        [SerializeField] private Color decorationColor;
        [SerializeField] private Color foodColor;
        [SerializeField] private Color healingColor;
        [SerializeField] private Color dropHighlightColor;
        [SerializeField] private Color emptyStateColor;

        public Color BackgroundColor => backgroundColor;
        public Color PanelColor => panelColor;
        public Color CardColor => cardColor;
        public Color CardFrameColor => cardFrameColor;
        public Color TitleColor => titleColor;
        public Color SubtitleColor => subtitleColor;
        public Color BodyColor => bodyColor;
        public Color CorrectColor => correctColor;
        public Color IncorrectColor => incorrectColor;
        public Color DecorationColor => decorationColor;
        public Color FoodColor => foodColor;
        public Color HealingColor => healingColor;
        public Color DropHighlightColor => dropHighlightColor;
        public Color EmptyStateColor => emptyStateColor;

        public static GardenSmellTaxonomyVisualSettings CreateDefault()
        {
            return new GardenSmellTaxonomyVisualSettings
            {
                backgroundColor = new Color(0.9f, 0.93f, 0.89f, 1f),
                panelColor = new Color(1f, 1f, 1f, 0.84f),
                cardColor = new Color(0.97f, 0.96f, 0.92f, 1f),
                cardFrameColor = new Color(0.29f, 0.38f, 0.25f, 1f),
                titleColor = new Color(0.14f, 0.18f, 0.15f, 1f),
                subtitleColor = new Color(0.25f, 0.34f, 0.26f, 1f),
                bodyColor = new Color(0.22f, 0.26f, 0.22f, 1f),
                correctColor = new Color(0.22f, 0.57f, 0.27f, 1f),
                incorrectColor = new Color(0.73f, 0.23f, 0.22f, 1f),
                decorationColor = new Color(0.69f, 0.51f, 0.73f, 1f),
                foodColor = new Color(0.85f, 0.63f, 0.25f, 1f),
                healingColor = new Color(0.28f, 0.58f, 0.49f, 1f),
                dropHighlightColor = new Color(0.14f, 0.38f, 0.27f, 0.16f),
                emptyStateColor = new Color(0.34f, 0.39f, 0.35f, 1f)
            };
        }

        public Color GetCategoryColor(GardenSmellTaxonomyCategory category)
        {
            switch (category)
            {
                case GardenSmellTaxonomyCategory.Decoration:
                    return decorationColor;
                case GardenSmellTaxonomyCategory.Food:
                    return foodColor;
                case GardenSmellTaxonomyCategory.Healing:
                    return healingColor;
                default:
                    return bodyColor;
            }
        }

        public void Clamp()
        {
            backgroundColor.a = 1f;
            cardColor.a = 1f;
            cardFrameColor.a = 1f;
            titleColor.a = 1f;
            subtitleColor.a = 1f;
            bodyColor.a = 1f;
            correctColor.a = 1f;
            incorrectColor.a = 1f;
            decorationColor.a = 1f;
            foodColor.a = 1f;
            healingColor.a = 1f;
            emptyStateColor.a = 1f;
            dropHighlightColor.a = Mathf.Clamp01(dropHighlightColor.a);
        }
    }
}
