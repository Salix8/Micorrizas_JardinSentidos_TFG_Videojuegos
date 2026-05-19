using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DeeprootPortraitSpeechAnimator : MonoBehaviour, IDialoguePortraitSpeechAnimator
    {
        [Header("References")]
        [SerializeField] private RectTransform headTransform;
        [SerializeField] private RectTransform leftArmTransform;
        [SerializeField] private RectTransform rightArmTransform;
        [SerializeField] private RectTransform beardTransform;
        [SerializeField] private RectTransform mouthTransform;

        [Header("Idle")]
        [SerializeField] [Min(0f)] private float idleCyclesPerSecond = 0.65f;
        [SerializeField] [Min(0f)] private float headSwayDegrees = 1.75f;
        [SerializeField] [Min(0f)] private float headBobPixels = 5f;
        [SerializeField] [Min(0f)] private float armSwayDegrees = 4f;
        [SerializeField] [Min(0f)] private float armBobPixels = 6f;

        [Header("Speech")]
        [SerializeField] [Min(0f)] private float beardTravel = 12f;
        [SerializeField] [Min(0f)] private float beardRotationDegrees = 2.4f;
        [SerializeField] [Min(0f)] private float mouthScaleMultiplier = 1.16f;
        [SerializeField] [Min(0f)] private float speechCyclesPerSecond = 8f;

        private Vector2 headBasePosition;
        private Quaternion headBaseRotation;
        private Vector2 leftArmBasePosition;
        private Quaternion leftArmBaseRotation;
        private Vector2 rightArmBasePosition;
        private Quaternion rightArmBaseRotation;
        private Vector2 beardBasePosition;
        private Quaternion beardBaseRotation;
        private Vector3 mouthBaseScale;
        private bool hasHeadBasePose;
        private bool hasLeftArmBasePose;
        private bool hasRightArmBasePose;
        private bool hasBeardBasePose;
        private bool hasMouthBasePose;
        private bool isSpeaking;

        public bool IsSpeaking => isSpeaking;

        private void Awake()
        {
            CacheBasePose();
            ApplyRestPose();
        }

        private void OnEnable()
        {
            CacheBasePose();
            ApplyRestPose();
        }

        private void Update()
        {
            if (isSpeaking)
            {
                var idlePhase = Time.unscaledTime * idleCyclesPerSecond * Mathf.PI * 2f;
                ApplyIdlePose(idlePhase);

                var speechPhase = Mathf.Sin(Time.unscaledTime * speechCyclesPerSecond * Mathf.PI * 2f);
                var normalized = (speechPhase + 1f) * 0.5f;
                ApplySpeakingPose(normalized);
            }
            else
            {
                ApplyIdleRestPose();
                ApplySpeechRestPose();
            }
        }

        public void SetSpeaking(bool speaking)
        {
            CacheBasePose();
            isSpeaking = speaking;
            if (!isSpeaking)
            {
                ApplyRestPose();
            }
        }

        private void CacheBasePose()
        {
            if (headTransform != null && !hasHeadBasePose)
            {
                headBasePosition = headTransform.anchoredPosition;
                headBaseRotation = headTransform.localRotation;
                hasHeadBasePose = true;
            }

            if (leftArmTransform != null && !hasLeftArmBasePose)
            {
                leftArmBasePosition = leftArmTransform.anchoredPosition;
                leftArmBaseRotation = leftArmTransform.localRotation;
                hasLeftArmBasePose = true;
            }

            if (rightArmTransform != null && !hasRightArmBasePose)
            {
                rightArmBasePosition = rightArmTransform.anchoredPosition;
                rightArmBaseRotation = rightArmTransform.localRotation;
                hasRightArmBasePose = true;
            }

            if (beardTransform != null && !hasBeardBasePose)
            {
                beardBasePosition = beardTransform.anchoredPosition;
                beardBaseRotation = beardTransform.localRotation;
                hasBeardBasePose = true;
            }

            if (mouthTransform != null && !hasMouthBasePose)
            {
                mouthBaseScale = mouthTransform.localScale;
                hasMouthBasePose = true;
            }
        }

        private void ApplyIdlePose(float idlePhase)
        {
            var sway = Mathf.Sin(idlePhase);
            var counterSway = Mathf.Sin(idlePhase + 1.1f);

            if (headTransform != null && hasHeadBasePose)
            {
                headTransform.anchoredPosition = headBasePosition + Vector2.up * (headBobPixels * sway);
                headTransform.localRotation = headBaseRotation * Quaternion.Euler(0f, 0f, headSwayDegrees * sway);
            }

            if (leftArmTransform != null && hasLeftArmBasePose)
            {
                leftArmTransform.anchoredPosition = leftArmBasePosition + Vector2.up * (armBobPixels * counterSway);
                leftArmTransform.localRotation = leftArmBaseRotation * Quaternion.Euler(0f, 0f, armSwayDegrees * counterSway);
            }

            if (rightArmTransform != null && hasRightArmBasePose)
            {
                rightArmTransform.anchoredPosition = rightArmBasePosition + Vector2.up * (armBobPixels * -counterSway);
                rightArmTransform.localRotation = rightArmBaseRotation * Quaternion.Euler(0f, 0f, armSwayDegrees * -counterSway);
            }
        }

        private void ApplySpeakingPose(float normalized)
        {
            if (beardTransform != null)
            {
                beardTransform.anchoredPosition = beardBasePosition + Vector2.down * (beardTravel * normalized);
                beardTransform.localRotation = beardBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(-beardRotationDegrees, beardRotationDegrees, normalized));
            }

            if (mouthTransform != null)
            {
                var verticalScale = Mathf.Lerp(1f, mouthScaleMultiplier, normalized);
                mouthTransform.localScale = new Vector3(mouthBaseScale.x, mouthBaseScale.y * verticalScale, mouthBaseScale.z);
            }
        }

        private void ApplySpeechRestPose()
        {
            if (beardTransform != null && hasBeardBasePose)
            {
                beardTransform.anchoredPosition = beardBasePosition;
                beardTransform.localRotation = beardBaseRotation;
            }

            if (mouthTransform != null && hasMouthBasePose)
            {
                mouthTransform.localScale = mouthBaseScale;
            }
        }

        private void ApplyIdleRestPose()
        {
            if (headTransform != null && hasHeadBasePose)
            {
                headTransform.anchoredPosition = headBasePosition;
                headTransform.localRotation = headBaseRotation;
            }

            if (leftArmTransform != null && hasLeftArmBasePose)
            {
                leftArmTransform.anchoredPosition = leftArmBasePosition;
                leftArmTransform.localRotation = leftArmBaseRotation;
            }

            if (rightArmTransform != null && hasRightArmBasePose)
            {
                rightArmTransform.anchoredPosition = rightArmBasePosition;
                rightArmTransform.localRotation = rightArmBaseRotation;
            }
        }

        private void ApplyRestPose()
        {
            ApplyIdleRestPose();
            ApplySpeechRestPose();
        }
    }
}
