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
        [SerializeField] private float beardRotationDegrees = 2.4f;
        [SerializeField] [Min(0f)] private float beardHeadFollowPosition = 0.65f;
        [SerializeField] [Min(0f)] private float beardHeadFollowRotation = 0.5f;
        [SerializeField] [Min(0f)] private float mouthScaleMultiplier = 1.16f;
        [SerializeField] [Min(0f)] private float speechCyclesPerSecond = 8f;
        [SerializeField] [Min(0f)] private float transitionSpeed = 5f;

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
        private float speakingBlend;
        private Vector2 currentHeadOffset;
        private float currentHeadRotationDegrees;

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
            if (transitionSpeed <= 0f)
            {
                speakingBlend = isSpeaking ? 1f : 0f;
            }
            else
            {
                speakingBlend = Mathf.MoveTowards(
                    speakingBlend,
                    isSpeaking ? 1f : 0f,
                    transitionSpeed * Time.unscaledDeltaTime);
            }

            if (speakingBlend > 0.0001f)
            {
                var idlePhase = Time.unscaledTime * idleCyclesPerSecond * Mathf.PI * 2f;
                ApplyIdlePose(idlePhase, speakingBlend);

                var speechPhase = Mathf.Sin(Time.unscaledTime * speechCyclesPerSecond * Mathf.PI * 2f);
                var normalized = (speechPhase + 1f) * 0.5f;
                ApplySpeakingPose(normalized, speakingBlend);
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

        private void ApplyIdlePose(float idlePhase, float blend)
        {
            var sway = Mathf.Sin(idlePhase);
            var counterSway = Mathf.Sin(idlePhase + 1.1f);

            currentHeadOffset = Vector2.zero;
            currentHeadRotationDegrees = 0f;

            if (headTransform != null && hasHeadBasePose)
            {
                currentHeadOffset = Vector2.up * (headBobPixels * sway * blend);
                currentHeadRotationDegrees = headSwayDegrees * sway * blend;
                headTransform.anchoredPosition = headBasePosition + currentHeadOffset;
                headTransform.localRotation = headBaseRotation * Quaternion.Euler(0f, 0f, currentHeadRotationDegrees);
            }

            if (leftArmTransform != null && hasLeftArmBasePose)
            {
                leftArmTransform.anchoredPosition = leftArmBasePosition + Vector2.up * (armBobPixels * counterSway * blend);
                leftArmTransform.localRotation = leftArmBaseRotation * Quaternion.Euler(0f, 0f, armSwayDegrees * counterSway * blend);
            }

            if (rightArmTransform != null && hasRightArmBasePose)
            {
                rightArmTransform.anchoredPosition = rightArmBasePosition + Vector2.up * (armBobPixels * -counterSway * blend);
                rightArmTransform.localRotation = rightArmBaseRotation * Quaternion.Euler(0f, 0f, armSwayDegrees * -counterSway * blend);
            }
        }

        private void ApplySpeakingPose(float normalized, float blend)
        {
            if (beardTransform != null)
            {
                var beardFollowOffset = currentHeadOffset * beardHeadFollowPosition;
                var beardTalkRotation = Mathf.Lerp(-beardRotationDegrees, beardRotationDegrees, normalized) * blend;
                var beardFollowRotation = currentHeadRotationDegrees * beardHeadFollowRotation;
                beardTransform.anchoredPosition = beardBasePosition + beardFollowOffset + Vector2.down * (beardTravel * normalized * blend);
                beardTransform.localRotation = beardBaseRotation * Quaternion.Euler(0f, 0f, beardFollowRotation + beardTalkRotation);
            }

            if (mouthTransform != null)
            {
                var verticalScale = Mathf.Lerp(1f, mouthScaleMultiplier, normalized * blend);
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
