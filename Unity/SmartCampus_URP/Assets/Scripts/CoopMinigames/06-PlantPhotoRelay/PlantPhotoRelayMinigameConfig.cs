using System;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Plant Photo Relay Config", fileName = "PlantPhotoRelayMinigameConfig")]
    public sealed class PlantPhotoRelayMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Catalog")]
        [SerializeField] private string catalogCsvRelativePath = "CoopMinigames/06-PlantPhotoRelay/PlantPhotoRelayPlants.csv";
        [SerializeField] [Min(1)] private int maxAutocompleteSuggestionCount = 6;
        [SerializeField] [Min(2)] private int minimumSupportedPlayers = 2;
        [SerializeField] [Min(2)] private int maxSupportedDevices = 6;

        [Header("Flow")]
        [SerializeField] [Min(1)] private int roundCount = 3;
        [SerializeField] [Min(3f)] private float cluePhaseDurationSeconds = 12f;
        [SerializeField] [Min(10f)] private float capturePhaseDurationSeconds = 45f;
        [SerializeField] [Min(10f)] private float guessPhaseDurationSeconds = 30f;
        [SerializeField] [Min(1f)] private float resultsRevealDurationSeconds = 4f;
        [SerializeField] private string timeoutMessage = "Tiempo agotado";
        [SerializeField] private string invalidSelectionMessage = "Selecciona una planta valida del catalogo";
        [SerializeField] private string cameraUnavailableMessage = "La camara no esta disponible en este dispositivo";

        [Header("Scoring")]
        [SerializeField] [Min(0f)] private float scoreExactMatch = 2.5f;
        [SerializeField] [Min(0f)] private float scorePromptMatchBonus = 0.5f;

        [Header("Capture")]
        [SerializeField] [Range(128, 2048)] private int targetPhotoMaxDimension = 768;
        [SerializeField] [Range(30, 95)] private int jpegQuality = 80;

        [Header("Visuals")]
        [SerializeField] private PlantPhotoRelayVisualSettings visualSettings = PlantPhotoRelayVisualSettings.CreateDefault();

        public string CatalogCsvRelativePath => catalogCsvRelativePath;
        public int MaxAutocompleteSuggestionCount => maxAutocompleteSuggestionCount;
        public int MinimumSupportedPlayers => minimumSupportedPlayers;
        public int MaxSupportedDevices => maxSupportedDevices;
        public int RoundCount => roundCount;
        public float CluePhaseDurationSeconds => cluePhaseDurationSeconds;
        public float CapturePhaseDurationSeconds => capturePhaseDurationSeconds;
        public float GuessPhaseDurationSeconds => guessPhaseDurationSeconds;
        public float ResultsRevealDurationSeconds => resultsRevealDurationSeconds;
        public string TimeoutMessage => timeoutMessage;
        public string InvalidSelectionMessage => invalidSelectionMessage;
        public string CameraUnavailableMessage => cameraUnavailableMessage;
        public float ScoreExactMatch => scoreExactMatch;
        public float ScorePromptMatchBonus => scorePromptMatchBonus;
        public int TargetPhotoMaxDimension => targetPhotoMaxDimension;
        public int JpegQuality => jpegQuality;
        public PlantPhotoRelayVisualSettings VisualSettings => visualSettings;

        private void OnValidate()
        {
            maxAutocompleteSuggestionCount = Mathf.Max(1, maxAutocompleteSuggestionCount);
            minimumSupportedPlayers = Mathf.Max(2, minimumSupportedPlayers);
            maxSupportedDevices = Mathf.Max(minimumSupportedPlayers, maxSupportedDevices);
            roundCount = Mathf.Max(1, roundCount);
            cluePhaseDurationSeconds = Mathf.Max(3f, cluePhaseDurationSeconds);
            capturePhaseDurationSeconds = Mathf.Max(10f, capturePhaseDurationSeconds);
            guessPhaseDurationSeconds = Mathf.Max(10f, guessPhaseDurationSeconds);
            resultsRevealDurationSeconds = Mathf.Max(1f, resultsRevealDurationSeconds);
            scoreExactMatch = Mathf.Max(0f, scoreExactMatch);
            scorePromptMatchBonus = Mathf.Max(0f, scorePromptMatchBonus);
            targetPhotoMaxDimension = Mathf.Clamp(targetPhotoMaxDimension, 128, 2048);
            jpegQuality = Mathf.Clamp(jpegQuality, 30, 95);
            visualSettings.Clamp();
        }
    }

    [Serializable]
    public struct PlantPhotoRelayVisualSettings
    {
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color panelColor;
        [SerializeField] private Color accentColor;
        [SerializeField] private Color secondaryAccentColor;
        [SerializeField] private Color successColor;
        [SerializeField] private Color failureColor;

        public Color BackgroundColor => backgroundColor;
        public Color PanelColor => panelColor;
        public Color AccentColor => accentColor;
        public Color SecondaryAccentColor => secondaryAccentColor;
        public Color SuccessColor => successColor;
        public Color FailureColor => failureColor;

        public static PlantPhotoRelayVisualSettings CreateDefault()
        {
            return new PlantPhotoRelayVisualSettings
            {
                backgroundColor = new Color(0.92f, 0.95f, 0.9f, 1f),
                panelColor = new Color(1f, 1f, 1f, 0.86f),
                accentColor = new Color(0.22f, 0.42f, 0.32f, 1f),
                secondaryAccentColor = new Color(0.26f, 0.49f, 0.62f, 1f),
                successColor = new Color(0.22f, 0.64f, 0.31f, 1f),
                failureColor = new Color(0.76f, 0.28f, 0.24f, 1f)
            };
        }

        public void Clamp()
        {
            backgroundColor.a = 1f;
            accentColor.a = 1f;
            secondaryAccentColor.a = 1f;
            successColor.a = 1f;
            failureColor.a = 1f;
        }
    }
}
