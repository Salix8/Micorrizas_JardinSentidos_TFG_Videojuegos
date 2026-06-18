using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class MinigameFailureFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinigameFailureFeedbackConfig sharedConfig;
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private AudioSource feedbackAudioSource;

        private Coroutine activeFeedbackCoroutine;
        private Vector2 originalShakeAnchoredPosition;
        private bool hasCapturedOriginalPosition;
        private static AudioClip generatedFallbackClip;

        public void PlayFeedback()
        {
            EnsureReferences();

            if (SharedConfig == null || SharedConfig.VibrateOnFailure)
            {
                Handheld.Vibrate();
            }

            PlayFailureSound();

            if (!isActiveAndEnabled)
            {
                ResetPresentationState();
                return;
            }

            if (activeFeedbackCoroutine != null)
            {
                StopCoroutine(activeFeedbackCoroutine);
            }

            activeFeedbackCoroutine = StartCoroutine(PlayFeedbackCoroutine());
        }

        private void Awake()
        {
            EnsureReferences();
            CaptureOriginalPosition();
            ResetPresentationState();
        }

        private void OnValidate()
        {
            EnsureReferences();

            if (!Application.isPlaying)
            {
                CaptureOriginalPosition();
                ResetPresentationState();
            }
        }

        private void OnDisable()
        {
            if (activeFeedbackCoroutine != null)
            {
                StopCoroutine(activeFeedbackCoroutine);
                activeFeedbackCoroutine = null;
            }

            ResetPresentationState();
        }

        private IEnumerator PlayFeedbackCoroutine()
        {
            CaptureOriginalPosition();
            var duration = Mathf.Max(0.05f, SharedConfig != null ? SharedConfig.DurationSeconds : 0.3f);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var inverse = 1f - normalizedTime;
                var envelope = inverse * inverse;

                ApplyShake(envelope);
                ApplyFlash(envelope);
                yield return null;
            }

            activeFeedbackCoroutine = null;
            ResetPresentationState();
        }

        private void ApplyShake(float envelope)
        {
            if (shakeTarget == null)
            {
                return;
            }

            var shakeDistance = SharedConfig != null ? SharedConfig.ShakeDistance : 18f;
            var randomOffset = Random.insideUnitCircle * (shakeDistance * envelope);
            shakeTarget.anchoredPosition = originalShakeAnchoredPosition + randomOffset;
        }

        private void ApplyFlash(float envelope)
        {
            if (flashOverlay == null)
            {
                return;
            }

            var overlayColor = SharedConfig != null ? SharedConfig.FlashColor : new Color(0.82f, 0.14f, 0.14f, 1f);
            overlayColor.a = (SharedConfig != null ? SharedConfig.FlashAlpha : 0.22f) * envelope;
            flashOverlay.color = overlayColor;
            if (!flashOverlay.gameObject.activeSelf)
            {
                flashOverlay.gameObject.SetActive(true);
            }
        }

        private void PlayFailureSound()
        {
            if (feedbackAudioSource == null)
            {
                return;
            }

            var clip = SharedConfig != null && SharedConfig.FeedbackAudioClip != null
                ? SharedConfig.FeedbackAudioClip
                : GetGeneratedFallbackClip();
            if (clip == null)
            {
                return;
            }

            feedbackAudioSource.PlayOneShot(clip, SharedConfig != null ? SharedConfig.SoundVolume : 0.6f);
        }

        private void EnsureReferences()
        {
            if (shakeTarget == null)
            {
                shakeTarget = transform as RectTransform;
            }

            if (feedbackAudioSource == null)
            {
                feedbackAudioSource = GetComponent<AudioSource>();
                if (feedbackAudioSource == null)
                {
                    feedbackAudioSource = gameObject.AddComponent<AudioSource>();
                }

                feedbackAudioSource.playOnAwake = false;
                feedbackAudioSource.loop = false;
                feedbackAudioSource.spatialBlend = 0f;
            }

            if (flashOverlay == null)
            {
                var overlayTransform = transform.Find("FailureFlashOverlay") as RectTransform;
                if (overlayTransform == null)
                {
                    var overlayObject = new GameObject("FailureFlashOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    overlayObject.layer = gameObject.layer;
                    overlayTransform = overlayObject.GetComponent<RectTransform>();
                    overlayTransform.SetParent(transform, false);
                    overlayTransform.anchorMin = Vector2.zero;
                    overlayTransform.anchorMax = Vector2.one;
                    overlayTransform.offsetMin = Vector2.zero;
                    overlayTransform.offsetMax = Vector2.zero;
                    overlayTransform.SetAsLastSibling();
                }

                flashOverlay = overlayTransform.GetComponent<Image>();
                flashOverlay.raycastTarget = false;
            }
        }

        private void CaptureOriginalPosition()
        {
            if (shakeTarget == null)
            {
                return;
            }

            originalShakeAnchoredPosition = shakeTarget.anchoredPosition;
            hasCapturedOriginalPosition = true;
        }

        private void ResetPresentationState()
        {
            if (shakeTarget != null && hasCapturedOriginalPosition)
            {
                shakeTarget.anchoredPosition = originalShakeAnchoredPosition;
            }

            if (flashOverlay != null)
            {
                var overlayColor = SharedConfig != null ? SharedConfig.FlashColor : new Color(0.82f, 0.14f, 0.14f, 1f);
                overlayColor.a = 0f;
                flashOverlay.color = overlayColor;
                flashOverlay.gameObject.SetActive(false);
            }
        }

        private MinigameFailureFeedbackConfig SharedConfig => sharedConfig;

        private static AudioClip GetGeneratedFallbackClip()
        {
            if (generatedFallbackClip != null)
            {
                return generatedFallbackClip;
            }

            const int sampleRate = 44100;
            const float clipDuration = 0.14f;
            var totalSamples = Mathf.CeilToInt(sampleRate * clipDuration);
            var samples = new float[totalSamples];

            for (var index = 0; index < totalSamples; index++)
            {
                var time = index / (float)sampleRate;
                var progress = index / (float)Mathf.Max(1, totalSamples - 1);
                var amplitude = (1f - progress) * (1f - progress);
                var frequency = Mathf.Lerp(240f, 140f, progress);
                samples[index] = Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude * 0.22f;
            }

            generatedFallbackClip = AudioClip.Create(
                "MinigameFailureFeedbackClip",
                totalSamples,
                1,
                sampleRate,
                false);
            generatedFallbackClip.SetData(samples, 0);
            return generatedFallbackClip;
        }
    }
}
