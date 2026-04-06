using System;
using UnityEngine;
using UnityEngine.Serialization;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Collaborative Plant Guess Config", fileName = "CollaborativePlantGuessMinigameConfig")]
    public sealed class CollaborativePlantGuessMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Dataset")]
        [SerializeField] private string csvRelativePath = "CoopMinigames/CollaborativePlantGuessPlants.csv";
        [SerializeField] [Min(2)] private int minimumSupportedPlayers = 2;
        [SerializeField] [Min(2)] private int maxSupportedDevices = 6;

        [Header("Gameplay")]
        [SerializeField] [Min(10f)] private float timeLimitSeconds = 180f;
        [SerializeField] [Min(3)] private int maxAttempts = 8;
        [SerializeField] [Min(1)] private int leafTypeRevealAttempt = 1;
        [SerializeField] [Min(1)] private int fruitDetailRevealAttempt = 2;
        [SerializeField] [Min(1)] private int leafPersistenceRevealAttempt = 4;
        [SerializeField] [Min(1)] private int plantTypeRevealAttempt = 6;
        [SerializeField] [Min(0f)] private float victoryRevealDelaySeconds = 1.2f;
        [SerializeField] [Min(1)] private int autocompleteSuggestionCount = 6;
        [SerializeField] private string timeoutMessage = "Tiempo agotado";
        [SerializeField] private string attemptsExhaustedMessage = "Intentos agotados";
        [SerializeField] private string invalidGuessMessage = "Selecciona una planta valida del autocompletado";

        [Header("Scoring")]
        [SerializeField] private CollaborativePlantGuessScoreSettings scoreSettings = CollaborativePlantGuessScoreSettings.CreateDefault();

        [Header("Visuals")]
        [SerializeField] private CollaborativePlantGuessVisualSettings visualSettings = CollaborativePlantGuessVisualSettings.CreateDefault();

        public string CsvRelativePath => csvRelativePath;
        public int MinimumSupportedPlayers => minimumSupportedPlayers;
        public int MaxSupportedDevices => maxSupportedDevices;
        public float TimeLimitSeconds => timeLimitSeconds;
        public int MaxAttempts => maxAttempts;
        public int LeafTypeRevealAttempt => leafTypeRevealAttempt;
        public int FruitDetailRevealAttempt => fruitDetailRevealAttempt;
        public int LeafPersistenceRevealAttempt => leafPersistenceRevealAttempt;
        public int PlantTypeRevealAttempt => plantTypeRevealAttempt;
        public float VictoryRevealDelaySeconds => victoryRevealDelaySeconds;
        public int AutocompleteSuggestionCount => autocompleteSuggestionCount;
        public string TimeoutMessage => timeoutMessage;
        public string AttemptsExhaustedMessage => attemptsExhaustedMessage;
        public string InvalidGuessMessage => invalidGuessMessage;
        public CollaborativePlantGuessScoreSettings ScoreSettings => scoreSettings;
        public CollaborativePlantGuessVisualSettings VisualSettings => visualSettings;

        private void OnValidate()
        {
            minimumSupportedPlayers = Mathf.Max(2, minimumSupportedPlayers);
            maxSupportedDevices = Mathf.Max(minimumSupportedPlayers, maxSupportedDevices);
            timeLimitSeconds = Mathf.Max(10f, timeLimitSeconds);
            maxAttempts = Mathf.Max(3, maxAttempts);
            leafTypeRevealAttempt = Mathf.Clamp(leafTypeRevealAttempt, 1, maxAttempts);
            fruitDetailRevealAttempt = Mathf.Clamp(fruitDetailRevealAttempt, 1, maxAttempts);
            leafPersistenceRevealAttempt = Mathf.Clamp(leafPersistenceRevealAttempt, 1, maxAttempts);
            plantTypeRevealAttempt = Mathf.Clamp(plantTypeRevealAttempt, 1, maxAttempts);
            victoryRevealDelaySeconds = Mathf.Max(0f, victoryRevealDelaySeconds);
            autocompleteSuggestionCount = Mathf.Max(1, autocompleteSuggestionCount);
            scoreSettings.Clamp();
            visualSettings.Clamp();
        }
    }

    [Serializable]
    public struct CollaborativePlantGuessScoreSettings
    {
        [SerializeField] [Min(1f)] private float maxScore;
        [SerializeField] [Range(0f, 1f)] private float minimumSolvedScoreRatio;
        [SerializeField] [Range(0, 2)] private int decimalPlaces;

        public float MaxScore => maxScore;
        public float MinimumSolvedScoreRatio => minimumSolvedScoreRatio;
        public int DecimalPlaces => decimalPlaces;

        public static CollaborativePlantGuessScoreSettings CreateDefault()
        {
            return new CollaborativePlantGuessScoreSettings
            {
                maxScore = 10f,
                minimumSolvedScoreRatio = 0.4f,
                decimalPlaces = 1
            };
        }

        public void Clamp()
        {
            maxScore = Mathf.Max(1f, maxScore);
            minimumSolvedScoreRatio = Mathf.Clamp01(minimumSolvedScoreRatio);
            decimalPlaces = Mathf.Clamp(decimalPlaces, 0, 2);
        }
    }

    [Serializable]
    public struct CollaborativePlantGuessVisualSettings
    {
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color panelColor;
        [SerializeField] private Color primaryButtonColor;
        [SerializeField] private Color secondaryButtonColor;
        [SerializeField] private Color exactMatchColor;
        [SerializeField] private Color closeMatchColor;
        [SerializeField] private Color incorrectMatchColor;
        [SerializeField] private Color neutralCellColor;

        public Color BackgroundColor => backgroundColor;
        public Color PanelColor => panelColor;
        public Color PrimaryButtonColor => primaryButtonColor;
        public Color SecondaryButtonColor => secondaryButtonColor;
        public Color ExactMatchColor => exactMatchColor;
        public Color CloseMatchColor => closeMatchColor;
        public Color IncorrectMatchColor => incorrectMatchColor;
        public Color NeutralCellColor => neutralCellColor;

        public static CollaborativePlantGuessVisualSettings CreateDefault()
        {
            return new CollaborativePlantGuessVisualSettings
            {
                backgroundColor = new Color(0.92f, 0.96f, 0.9f, 1f),
                panelColor = new Color(1f, 1f, 1f, 0.82f),
                primaryButtonColor = new Color(0.22f, 0.4f, 0.29f, 1f),
                secondaryButtonColor = new Color(0.21f, 0.42f, 0.46f, 1f),
                exactMatchColor = new Color(0.25f, 0.63f, 0.31f, 1f),
                closeMatchColor = new Color(0.9f, 0.62f, 0.2f, 1f),
                incorrectMatchColor = new Color(0.75f, 0.27f, 0.24f, 1f),
                neutralCellColor = new Color(0.88f, 0.9f, 0.86f, 1f)
            };
        }

        public void Clamp()
        {
            backgroundColor.a = 1f;
            primaryButtonColor.a = 1f;
            secondaryButtonColor.a = 1f;
            exactMatchColor.a = 1f;
            closeMatchColor.a = 1f;
            incorrectMatchColor.a = 1f;
            neutralCellColor.a = 1f;
        }
    }
}
