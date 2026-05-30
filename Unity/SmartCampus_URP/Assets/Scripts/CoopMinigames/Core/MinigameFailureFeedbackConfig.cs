using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [CreateAssetMenu(
        fileName = "MinigameFailureFeedbackConfig",
        menuName = "SmartCampus/Coop/Minigame Failure Feedback Config")]
    public sealed class MinigameFailureFeedbackConfig : ScriptableObject
    {
        [Header("Audio")]
        [SerializeField] private AudioClip feedbackAudioClip;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.6f;

        [Header("Haptics")]
        [SerializeField] private bool vibrateOnFailure = true;

        [Header("Visuals")]
        [SerializeField] [Min(0.05f)] private float durationSeconds = 0.3f;
        [SerializeField] [Min(0f)] private float shakeDistance = 18f;
        [SerializeField] [Range(0f, 1f)] private float flashAlpha = 0.22f;
        [SerializeField] private Color flashColor = new(0.82f, 0.14f, 0.14f, 1f);

        public AudioClip FeedbackAudioClip => feedbackAudioClip;
        public float SoundVolume => soundVolume;
        public bool VibrateOnFailure => vibrateOnFailure;
        public float DurationSeconds => durationSeconds;
        public float ShakeDistance => shakeDistance;
        public float FlashAlpha => flashAlpha;
        public Color FlashColor => flashColor;
    }
}
