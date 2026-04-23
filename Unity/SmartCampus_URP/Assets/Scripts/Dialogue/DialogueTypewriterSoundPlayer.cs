using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class DialogueTypewriterSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DialogueAudioProfileConfig audioProfile;

        private float lastBlipTime = -999f;

        private void Awake()
        {
            audioSource ??= GetComponent<AudioSource>();
        }

        public void PlayForVisibleCharacter(string characterName, char visibleCharacter, int visibleCharacterIndex)
        {
            if (audioSource == null || audioProfile == null)
            {
                return;
            }

            if (!ShouldPlayForCharacter(visibleCharacter, visibleCharacterIndex))
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - lastBlipTime < audioProfile.MinSecondsBetweenBlips)
            {
                return;
            }

            if (!audioProfile.TryGetVoice(characterName, visibleCharacter, out var clip, out var volume, out var pitch))
            {
                return;
            }

            lastBlipTime = now;
            audioSource.pitch = Mathf.Max(0.01f, pitch);
            audioSource.PlayOneShot(clip, volume);
        }

        private bool ShouldPlayForCharacter(char visibleCharacter, int visibleCharacterIndex)
        {
            if (!audioProfile.PlayOnWhitespace && char.IsWhiteSpace(visibleCharacter))
            {
                return false;
            }

            if (!audioProfile.PlayOnPunctuation && char.IsPunctuation(visibleCharacter))
            {
                return false;
            }

            return visibleCharacterIndex % audioProfile.CharactersPerBlip == 0;
        }
    }
}
