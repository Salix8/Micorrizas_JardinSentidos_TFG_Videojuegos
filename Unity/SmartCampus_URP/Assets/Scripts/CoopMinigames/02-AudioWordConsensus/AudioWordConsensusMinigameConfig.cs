using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Audio Word Consensus Config", fileName = "AudioWordConsensusMinigameConfig")]
    public sealed class AudioWordConsensusMinigameConfig : CooperativeMinigameConfigBase
    {
        [Header("Gameplay")]
        [SerializeField] [Min(2)] private int maxSupportedDevices = 6;
        [SerializeField] [Min(10f)] private float timeLimitSeconds = 120f;
        [SerializeField] [Min(0f)] private float feedbackDurationSeconds = 1.25f;
        [SerializeField] private string timeoutMessage = "Tiempo agotado";
        [SerializeField] private string missingAudioClipLabel = "Sonido pendiente de asignar";
        [SerializeField] private List<AudioWordConsensusRoundDefinition> roundDefinitions = new();

        [Header("Scoring")]
        [SerializeField] private AudioWordConsensusScoreSettings scoreSettings = AudioWordConsensusScoreSettings.CreateDefault();

        [Header("Visuals")]
        [SerializeField] private AudioWordConsensusVisualSettings visualSettings = AudioWordConsensusVisualSettings.CreateDefault();

        public int MaxSupportedDevices => maxSupportedDevices;
        public float TimeLimitSeconds => timeLimitSeconds;
        public float FeedbackDurationSeconds => feedbackDurationSeconds;
        public string TimeoutMessage => timeoutMessage;
        public string MissingAudioClipLabel => missingAudioClipLabel;
        public int ActiveRoundCount => roundDefinitions.Count;
        public IReadOnlyList<AudioWordConsensusRoundDefinition> RoundDefinitions => roundDefinitions;
        public AudioWordConsensusScoreSettings ScoreSettings => scoreSettings;
        public AudioWordConsensusVisualSettings VisualSettings => visualSettings;

        public AudioWordConsensusRoundDefinition GetRoundDefinition(int roundIndex)
        {
            return roundIndex >= 0 && roundIndex < roundDefinitions.Count ? roundDefinitions[roundIndex] : null;
        }

        public bool SupportsParticipantCount(int participantCount)
        {
            return TryValidateForParticipantCount(participantCount, out _);
        }

        public int CountUsableRoundDefinitions(int participantCount)
        {
            if (participantCount < 2)
            {
                return 0;
            }

            var usableRoundCount = 0;
            for (var index = 0; index < roundDefinitions.Count; index++)
            {
                if (AudioWordConsensusRoundDefinitionValidator.IsUsable(roundDefinitions[index]))
                {
                    usableRoundCount++;
                }
            }

            return usableRoundCount;
        }

        public bool TryValidateForParticipantCount(int participantCount, out string errorMessage)
        {
            if (participantCount < 2)
            {
                errorMessage = "Se necesitan al menos dos participantes.";
                return false;
            }

            if (participantCount > maxSupportedDevices)
            {
                errorMessage = $"La configuracion solo admite hasta {maxSupportedDevices} dispositivos.";
                return false;
            }

            if (roundDefinitions == null || roundDefinitions.Count == 0)
            {
                errorMessage = "No hay rondas de audio configuradas.";
                return false;
            }

            var usableRoundCount = CountUsableRoundDefinitions(participantCount);
            if (usableRoundCount > 0)
            {
                errorMessage = string.Empty;
                return true;
            }

            var builder = new StringBuilder("No hay rondas utilizables.");
            for (var index = 0; index < roundDefinitions.Count; index++)
            {
                if (AudioWordConsensusRoundDefinitionValidator.TryValidate(roundDefinitions[index], out var roundError))
                {
                    continue;
                }

                builder.Append(' ');
                builder.Append($"R{index + 1}: {roundError}");
            }

            errorMessage = builder.ToString();
            return false;
        }

        private void OnValidate()
        {
            maxSupportedDevices = Mathf.Max(2, maxSupportedDevices);
            timeLimitSeconds = Mathf.Max(10f, timeLimitSeconds);
            feedbackDurationSeconds = Mathf.Max(0f, feedbackDurationSeconds);
            scoreSettings.Clamp();
            visualSettings.Clamp();
        }
    }

    [Serializable]
    public sealed class AudioWordConsensusRoundDefinition
    {
        [SerializeField] private string promptLabel = "Sonido";
        [SerializeField] private AudioClip soundClip;
        [SerializeField] private string correctWord = "Palabra correcta";
        [SerializeField] private List<string> distractorWords = new();

        public string PromptLabel => promptLabel;
        public AudioClip SoundClip => soundClip;
        public string CorrectWord => correctWord;
        public IReadOnlyList<string> DistractorWords => distractorWords;

        public bool IsUsableForReceiverCount(int receiverCount)
        {
            return AudioWordConsensusRoundDefinitionValidator.IsUsable(this);
        }
    }

    internal static class AudioWordConsensusRoundDefinitionValidator
    {
        public static bool IsUsable(AudioWordConsensusRoundDefinition roundDefinition)
        {
            return TryValidate(roundDefinition, out _);
        }

        public static bool TryValidate(AudioWordConsensusRoundDefinition roundDefinition, out string errorMessage)
        {
            if (roundDefinition == null ||
                string.IsNullOrWhiteSpace(roundDefinition.CorrectWord))
            {
                errorMessage = "Falta la palabra correcta.";
                return false;
            }

            if (roundDefinition.SoundClip == null)
            {
                errorMessage = "Falta asignar el AudioClip.";
                return false;
            }

            var optionWords = AudioWordConsensusWordAssignmentService.BuildDistinctOptionWords(
                roundDefinition.CorrectWord,
                roundDefinition.DistractorWords);
            if (optionWords.Count <= 1)
            {
                errorMessage = "Cada sonido necesita al menos una respuesta incorrecta distinta de la correcta.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public struct AudioWordConsensusScoreSettings
    {
        [SerializeField] [Min(1f)] private float maxScore;
        [SerializeField] [Min(0f)] private float minimumScore;
        [SerializeField] [Range(0, 2)] private int decimalPlaces;

        public float MaxScore => maxScore;
        public float MinimumScore => minimumScore;
        public int DecimalPlaces => decimalPlaces;

        public static AudioWordConsensusScoreSettings CreateDefault()
        {
            return new AudioWordConsensusScoreSettings
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
    public struct AudioWordConsensusVisualSettings
    {
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color panelColor;
        [SerializeField] private Color primaryButtonColor;
        [SerializeField] private Color receiverButtonColor;
        [SerializeField] private Color emitterAccentColor;
        [SerializeField] private Color receiverAccentColor;
        [SerializeField] private Color textColor;

        public Color BackgroundColor => backgroundColor;
        public Color PanelColor => panelColor;
        public Color PrimaryButtonColor => primaryButtonColor;
        public Color ReceiverButtonColor => receiverButtonColor;
        public Color EmitterAccentColor => emitterAccentColor;
        public Color ReceiverAccentColor => receiverAccentColor;
        public Color TextColor => textColor;

        public static AudioWordConsensusVisualSettings CreateDefault()
        {
            return new AudioWordConsensusVisualSettings
            {
                backgroundColor = new Color(0.93f, 0.95f, 0.89f, 1f),
                panelColor = new Color(1f, 1f, 1f, 0.78f),
                primaryButtonColor = new Color(0.19f, 0.38f, 0.43f, 1f),
                receiverButtonColor = new Color(0.27f, 0.49f, 0.31f, 1f),
                emitterAccentColor = new Color(0.84f, 0.59f, 0.18f, 1f),
                receiverAccentColor = new Color(0.28f, 0.52f, 0.34f, 1f),
                textColor = new Color(0.12f, 0.15f, 0.17f, 1f)
            };
        }

        public void Clamp()
        {
            backgroundColor.a = 1f;
            primaryButtonColor.a = 1f;
            receiverButtonColor.a = 1f;
            emitterAccentColor.a = 1f;
            receiverAccentColor.a = 1f;
            textColor.a = 1f;
        }
    }
}
