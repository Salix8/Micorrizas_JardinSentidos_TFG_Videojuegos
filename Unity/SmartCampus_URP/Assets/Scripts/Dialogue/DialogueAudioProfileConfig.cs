using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Dialogue Audio Profile", fileName = "DialogueAudioProfileConfig")]
    public sealed class DialogueAudioProfileConfig : ScriptableObject
    {
        [Serializable]
        public sealed class CharacterVoiceConfig
        {
            [SerializeField] private string characterName;
            [SerializeField] private AudioClip blipClip;
            [SerializeField] private AudioClip[] letterClips = Array.Empty<AudioClip>();
            [SerializeField] [Range(0f, 2f)] private float volumeMultiplier = 1f;
            [SerializeField] private float pitchOffset;

            public string CharacterName => characterName;
            public AudioClip BlipClip => blipClip;
            public IReadOnlyList<AudioClip> LetterClips => letterClips;
            public float VolumeMultiplier => Mathf.Max(0f, volumeMultiplier);
            public float PitchOffset => pitchOffset;
        }

        [Header("Default Voice")]
        [SerializeField] private AudioClip defaultBlipClip;
        [SerializeField] private AudioClip[] defaultLetterClips = Array.Empty<AudioClip>();
        [SerializeField] [Range(0f, 1f)] private float defaultVolume = 0.55f;
        [SerializeField] private Vector2 randomPitchRange = new(0.96f, 1.08f);

        [Header("Animalese")]
        [SerializeField] private bool useLetterBasedClipSelection = true;
        [SerializeField] private bool useLetterBasedPitch = true;
        [SerializeField] [Range(0f, 0.05f)] private float letterPitchStep = 0.015f;

        [Header("Cadence")]
        [SerializeField] [Min(1)] private int charactersPerBlip = 2;
        [SerializeField] [Min(0f)] private float minSecondsBetweenBlips = 0.035f;
        [SerializeField] private bool playOnWhitespace;
        [SerializeField] private bool playOnPunctuation = true;

        [Header("Character Overrides")]
        [SerializeField] private CharacterVoiceConfig[] characterVoices = Array.Empty<CharacterVoiceConfig>();

        public float DefaultVolume => Mathf.Clamp01(defaultVolume);
        public int CharactersPerBlip => Mathf.Max(1, charactersPerBlip);
        public float MinSecondsBetweenBlips => Mathf.Max(0f, minSecondsBetweenBlips);
        public bool PlayOnWhitespace => playOnWhitespace;
        public bool PlayOnPunctuation => playOnPunctuation;

        public bool TryGetVoice(string characterName, char visibleCharacter, out AudioClip clip, out float volume, out float pitch)
        {
            volume = DefaultVolume;
            pitch = UnityEngine.Random.Range(
                Mathf.Min(randomPitchRange.x, randomPitchRange.y),
                Mathf.Max(randomPitchRange.x, randomPitchRange.y));

            var voiceConfig = FindCharacterVoice(characterName);
            var letterIndex = GetLetterIndex(visibleCharacter);
            clip = SelectClipForLetter(letterIndex, voiceConfig);

            if (voiceConfig != null)
            {
                volume *= voiceConfig.VolumeMultiplier;
                pitch += voiceConfig.PitchOffset;
            }

            if (useLetterBasedPitch && letterIndex >= 0)
            {
                pitch += (letterIndex - 12) * letterPitchStep;
            }

            return clip != null && volume > 0f;
        }

        private AudioClip SelectClipForLetter(int letterIndex, CharacterVoiceConfig voiceConfig)
        {
            if (useLetterBasedClipSelection)
            {
                var characterLetterClip = SelectFromList(voiceConfig == null ? null : voiceConfig.LetterClips, letterIndex);
                if (characterLetterClip != null)
                {
                    return characterLetterClip;
                }

                var defaultLetterClip = SelectFromList(defaultLetterClips, letterIndex);
                if (defaultLetterClip != null)
                {
                    return defaultLetterClip;
                }
            }

            if (voiceConfig != null && voiceConfig.BlipClip != null)
            {
                return voiceConfig.BlipClip;
            }

            return defaultBlipClip;
        }

        private static AudioClip SelectFromList(IReadOnlyList<AudioClip> clips, int letterIndex)
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            var selectedIndex = Mathf.Abs(letterIndex < 0 ? 0 : letterIndex) % clips.Count;
            return clips[selectedIndex];
        }

        private static int GetLetterIndex(char character)
        {
            var normalized = char.ToLowerInvariant(character);
            switch (normalized)
            {
                case 'á':
                case 'à':
                case 'ä':
                    normalized = 'a';
                    break;
                case 'é':
                case 'è':
                case 'ë':
                    normalized = 'e';
                    break;
                case 'í':
                case 'ì':
                case 'ï':
                    normalized = 'i';
                    break;
                case 'ó':
                case 'ò':
                case 'ö':
                    normalized = 'o';
                    break;
                case 'ú':
                case 'ù':
                case 'ü':
                    normalized = 'u';
                    break;
                case 'ç':
                    normalized = 'c';
                    break;
            }

            if (normalized >= 'a' && normalized <= 'z')
            {
                return normalized - 'a';
            }

            if (normalized >= '0' && normalized <= '9')
            {
                return 26 + normalized - '0';
            }

            return -1;
        }

        private CharacterVoiceConfig FindCharacterVoice(string characterName)
        {
            if (string.IsNullOrWhiteSpace(characterName) || characterVoices == null)
            {
                return null;
            }

            for (var index = 0; index < characterVoices.Length; index++)
            {
                var voiceConfig = characterVoices[index];
                if (voiceConfig == null)
                {
                    continue;
                }

                if (string.Equals(voiceConfig.CharacterName, characterName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return voiceConfig;
                }
            }

            return null;
        }
    }
}
