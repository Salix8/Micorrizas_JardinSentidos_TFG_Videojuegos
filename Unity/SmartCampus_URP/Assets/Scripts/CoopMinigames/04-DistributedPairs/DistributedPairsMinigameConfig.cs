using System;
using System.Collections.Generic;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Distributed Pairs Config", fileName = "DistributedPairsMinigameConfig")]
    public sealed class DistributedPairsMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Gameplay")]
        [SerializeField] [Min(1)] private int cardsPerDevice = 4;
        [SerializeField] [Min(1)] private int pairsToUse = 10;
        [SerializeField] private List<DistributedPairDefinition> pairDefinitions = new();

        [Header("Scoring")]
        [SerializeField] private DistributedPairsScoreSettings scoreSettings = DistributedPairsScoreSettings.CreateDefault();

        [Header("Cards")]
        [SerializeField] private DistributedPairsCardVisualSettings cardVisualSettings = DistributedPairsCardVisualSettings.CreateDefault();

        public int CardsPerDevice => cardsPerDevice;
        public int ActivePairCount => Mathf.Min(pairsToUse, pairDefinitions.Count);
        public int DeckCardCount => ActivePairCount * 2;
        public DistributedPairsScoreSettings ScoreSettings => scoreSettings;
        public DistributedPairsCardVisualSettings CardVisualSettings => cardVisualSettings;

        public DistributedPairDefinition GetPairDefinition(int pairId)
        {
            return pairId >= 0 && pairId < ActivePairCount ? pairDefinitions[pairId] : null;
        }

        private void OnValidate()
        {
            cardsPerDevice = Mathf.Max(1, cardsPerDevice);
            pairsToUse = Mathf.Max(1, pairsToUse);
            scoreSettings.Clamp();
            cardVisualSettings.Clamp();
        }
    }

    [Serializable]
    public sealed class DistributedPairDefinition
    {
        [SerializeField] private string title = "Carta";
        [SerializeField] [TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private Sprite illustration;
        [SerializeField] private Color faceColor = new(0.83f, 0.89f, 0.68f, 1f);

        public string Title => title;
        public string Description => description;
        public Sprite Illustration => illustration;
        public Color FaceColor => faceColor;
    }

    [Serializable]
    public struct DistributedPairsScoreSettings
    {
        [SerializeField] [Min(1f)] private float maxScore;
        [SerializeField] [Min(0f)] private float minimumScore;
        [SerializeField] [Min(0f)] private float completionWeight;
        [SerializeField] [Min(0f)] private float efficiencyWeight;
        [SerializeField] [Range(0, 2)] private int decimalPlaces;

        public float MaxScore => maxScore;
        public float MinimumScore => minimumScore;
        public float CompletionWeight => completionWeight;
        public float EfficiencyWeight => efficiencyWeight;
        public int DecimalPlaces => decimalPlaces;

        public static DistributedPairsScoreSettings CreateDefault()
        {
            return new DistributedPairsScoreSettings
            {
                maxScore = 10f,
                minimumScore = 0f,
                completionWeight = 0.4f,
                efficiencyWeight = 0.6f,
                decimalPlaces = 1
            };
        }

        public void Clamp()
        {
            maxScore = Mathf.Max(1f, maxScore);
            minimumScore = Mathf.Clamp(minimumScore, 0f, maxScore);
            completionWeight = Mathf.Max(0f, completionWeight);
            efficiencyWeight = Mathf.Max(0f, efficiencyWeight);
            decimalPlaces = Mathf.Clamp(decimalPlaces, 0, 2);
        }
    }

    [Serializable]
    public struct DistributedPairsCardVisualSettings
    {
        [SerializeField] private Color backColor;
        [SerializeField] private Color backTextColor;
        [SerializeField] private Color frameColor;
        [SerializeField] private Color selectedFrameColor;
        [SerializeField] private Color frontTextColor;
        [SerializeField] [Min(1)] private int maxColumns;
        [SerializeField] private Vector2 minCardSize;
        [SerializeField] private Vector2 maxCardSize;
        [SerializeField] [Min(0.3f)] private float cardAspectRatio;

        public Color BackColor => backColor;
        public Color BackTextColor => backTextColor;
        public Color FrameColor => frameColor;
        public Color SelectedFrameColor => selectedFrameColor;
        public Color FrontTextColor => frontTextColor;
        public int MaxColumns => maxColumns;
        public Vector2 MinCardSize => minCardSize;
        public Vector2 MaxCardSize => maxCardSize;
        public float CardAspectRatio => cardAspectRatio;

        public static DistributedPairsCardVisualSettings CreateDefault()
        {
            return new DistributedPairsCardVisualSettings
            {
                backColor = new Color(0.16f, 0.29f, 0.35f, 1f),
                backTextColor = Color.white,
                frameColor = new Color(0.77f, 0.87f, 0.88f, 1f),
                selectedFrameColor = new Color(0.95f, 0.69f, 0.22f, 1f),
                frontTextColor = new Color(0.12f, 0.15f, 0.17f, 1f),
                maxColumns = 4,
                minCardSize = new Vector2(120f, 170f),
                maxCardSize = new Vector2(240f, 330f),
                cardAspectRatio = 0.72f
            };
        }

        public void Clamp()
        {
            maxColumns = Mathf.Max(1, maxColumns);
            minCardSize.x = Mathf.Max(80f, minCardSize.x);
            minCardSize.y = Mathf.Max(120f, minCardSize.y);
            maxCardSize.x = Mathf.Max(minCardSize.x, maxCardSize.x);
            maxCardSize.y = Mathf.Max(minCardSize.y, maxCardSize.y);
            cardAspectRatio = Mathf.Max(0.3f, cardAspectRatio);
        }
    }
}
