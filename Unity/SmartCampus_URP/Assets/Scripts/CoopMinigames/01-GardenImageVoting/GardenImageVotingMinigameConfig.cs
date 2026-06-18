using System;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Garden Image Voting Config", fileName = "GardenImageVotingMinigameConfig")]
    public sealed class GardenImageVotingMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Dataset")]
        [SerializeField] private string csvRelativePath = "CoopMinigames/01-GardenImagenVotingCards/GardenImageVotingCards.csv";
        [SerializeField] [Min(1)] private int cardsPerDevice = 5;
        [SerializeField] [Min(1)] private int maxSupportedDevices = 6;
        [SerializeField] private bool allowRepeatedImagesAcrossDevices = true;

        [Header("Gameplay")]
        [SerializeField] [Min(5f)] private float timeLimitSeconds = 300f;
        [SerializeField] [Min(0f)] private float incorrectAnswerPenaltySeconds = 10f;
        [SerializeField] [Min(40f)] private float swipeThreshold = 120f;
        [SerializeField] [Min(0.05f)] private float transitionDuration = 0.22f;
        [SerializeField] private string timeoutMessage = "Tiempo agotado";

        [Header("Scoring")]
        [SerializeField] private GardenImageVotingScoreSettings scoreSettings = GardenImageVotingScoreSettings.CreateDefault();

        [Header("Visuals")]
        [SerializeField] private GardenImageVotingCardVisualSettings cardVisualSettings = GardenImageVotingCardVisualSettings.CreateDefault();

        public string CsvRelativePath => csvRelativePath;
        public int CardsPerDevice => cardsPerDevice;
        public int MaxSupportedDevices => maxSupportedDevices;
        public bool AllowRepeatedImagesAcrossDevices => allowRepeatedImagesAcrossDevices;
        public float TimeLimitSeconds => timeLimitSeconds;
        public float IncorrectAnswerPenaltySeconds => incorrectAnswerPenaltySeconds;
        public float SwipeThreshold => swipeThreshold;
        public float TransitionDuration => transitionDuration;
        public string TimeoutMessage => timeoutMessage;
        public GardenImageVotingScoreSettings ScoreSettings => scoreSettings;
        public GardenImageVotingCardVisualSettings CardVisualSettings => cardVisualSettings;

        private void OnValidate()
        {
            cardsPerDevice = Mathf.Max(1, cardsPerDevice);
            maxSupportedDevices = Mathf.Max(1, maxSupportedDevices);
            timeLimitSeconds = Mathf.Max(5f, timeLimitSeconds);
            incorrectAnswerPenaltySeconds = Mathf.Max(0f, incorrectAnswerPenaltySeconds);
            swipeThreshold = Mathf.Max(40f, swipeThreshold);
            transitionDuration = Mathf.Max(0.05f, transitionDuration);
            scoreSettings.Clamp();
            cardVisualSettings.Clamp();
        }
    }

    [Serializable]
    public struct GardenImageVotingScoreSettings
    {
        [SerializeField] [Min(1f)] private float maxScore;
        [SerializeField] [Min(0f)] private float minimumScore;
        [SerializeField] [Range(0, 2)] private int decimalPlaces;

        public float MaxScore => maxScore;
        public float MinimumScore => minimumScore;
        public int DecimalPlaces => decimalPlaces;

        public static GardenImageVotingScoreSettings CreateDefault()
        {
            return new GardenImageVotingScoreSettings
            {
                maxScore = 10f,
                minimumScore = 0f,
                decimalPlaces = 1
            };
        }

        public void Clamp()
        {
            maxScore = Mathf.Max(1f, maxScore);
            minimumScore = Mathf.Clamp(minimumScore, 0f, maxScore);
            decimalPlaces = Mathf.Clamp(decimalPlaces, 0, 2);
        }
    }

    [Serializable]
    public struct GardenImageVotingCardVisualSettings
    {
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color frameColor;
        [SerializeField] private Color topicColor;
        [SerializeField] private Color titleColor;
        [SerializeField] private Color bodyColor;
        [SerializeField] private Color swipeRightColor;
        [SerializeField] private Color swipeLeftColor;
        [SerializeField] private Color placeholderColor;

        public Color BackgroundColor => backgroundColor;
        public Color FrameColor => frameColor;
        public Color TopicColor => topicColor;
        public Color TitleColor => titleColor;
        public Color BodyColor => bodyColor;
        public Color SwipeRightColor => swipeRightColor;
        public Color SwipeLeftColor => swipeLeftColor;
        public Color PlaceholderColor => placeholderColor;

        public static GardenImageVotingCardVisualSettings CreateDefault()
        {
            return new GardenImageVotingCardVisualSettings
            {
                backgroundColor = new Color(0.96f, 0.95f, 0.9f, 1f),
                frameColor = new Color(0.23f, 0.39f, 0.29f, 1f),
                topicColor = new Color(0.18f, 0.34f, 0.24f, 1f),
                titleColor = new Color(0.13f, 0.18f, 0.17f, 1f),
                bodyColor = new Color(0.22f, 0.25f, 0.24f, 1f),
                swipeRightColor = new Color(0.3f, 0.55f, 0.31f, 1f),
                swipeLeftColor = new Color(0.67f, 0.29f, 0.24f, 1f),
                placeholderColor = new Color(0.82f, 0.82f, 0.78f, 1f)
            };
        }

        public void Clamp()
        {
            backgroundColor.a = 1f;
            frameColor.a = 1f;
            topicColor.a = 1f;
            titleColor.a = 1f;
            bodyColor.a = 1f;
            swipeRightColor.a = 1f;
            swipeLeftColor.a = 1f;
            placeholderColor.a = 1f;
        }
    }
}
