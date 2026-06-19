using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Flow Config", fileName = "DialogueFlowConfig")]
    public sealed class DialogueFlowConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private bool dialoguesEnabled = true;
        [SerializeField] private DialogueSystemConfig dialogueSystemConfig;
        [SerializeField] private GameObject dialoguePanelPrefab;

        [Header("Opening")]
        [SerializeField] private string reclaimSequenceKey = "Reclaim";
        [SerializeField] private string warningSequenceKey = "Act I – Warning";
        [SerializeField] private string reconnectionSequenceKey = "Act III – Reconnection";

        [Header("Minigames")]
        [SerializeField] private List<DialogueFlowMinigameEntry> minigameEntries = new();

        [Header("GPS")]
        [SerializeField] [Min(0f)] private float gpsFixTimeoutSeconds = 8f;
        [SerializeField] [Min(0f)] private float editorGpsFallbackSeconds = 1f;
        [SerializeField] private bool treatMissingGpsAsOutsideGarden = true;

        [Header("Opening Feedback")]
        [SerializeField] private bool showOpeningLoadingOverlay = true;
        [SerializeField] private string openingLoadingText = "Cargando...";
        [SerializeField] private bool logOpeningTiming;

        public bool DialoguesEnabled => dialoguesEnabled;
        public DialogueSystemConfig DialogueSystemConfig => dialogueSystemConfig;
        public GameObject DialoguePanelPrefab => dialoguePanelPrefab;
        public string ReclaimSequenceKey => reclaimSequenceKey;
        public string WarningSequenceKey => warningSequenceKey;
        public string ReconnectionSequenceKey => reconnectionSequenceKey;
        public IReadOnlyList<DialogueFlowMinigameEntry> MinigameEntries => minigameEntries;
        public float GpsFixTimeoutSeconds => gpsFixTimeoutSeconds;
        public float EditorGpsFallbackSeconds => editorGpsFallbackSeconds;
        public bool TreatMissingGpsAsOutsideGarden => treatMissingGpsAsOutsideGarden;
        public bool ShowOpeningLoadingOverlay => showOpeningLoadingOverlay;
        public string OpeningLoadingText => openingLoadingText;
        public bool LogOpeningTiming => logOpeningTiming;

        public bool TryGetIntroductionKey(int minigameIndex, out string sequenceKey)
        {
            return DialogueFlowResolver.TryGetSequenceKey(minigameEntries, minigameIndex, success: false, out sequenceKey);
        }

        public bool TryGetSuccessKey(int minigameIndex, out string sequenceKey)
        {
            return DialogueFlowResolver.TryGetSequenceKey(minigameEntries, minigameIndex, success: true, out sequenceKey);
        }
    }

    [Serializable]
    public sealed class DialogueFlowMinigameEntry
    {
        [SerializeField] private int minigameIndex;
        [SerializeField] private string introductionSequenceKey = string.Empty;
        [SerializeField] private string successSequenceKey = string.Empty;

        public int MinigameIndex => minigameIndex;
        public string IntroductionSequenceKey => introductionSequenceKey;
        public string SuccessSequenceKey => successSequenceKey;
    }

    public static class DialogueFlowResolver
    {
        public static bool TryGetSequenceKey(
            IReadOnlyList<DialogueFlowMinigameEntry> entries,
            int minigameIndex,
            bool success,
            out string sequenceKey)
        {
            sequenceKey = string.Empty;
            if (entries == null)
            {
                return false;
            }

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null || entry.MinigameIndex != minigameIndex)
                {
                    continue;
                }

                sequenceKey = success ? entry.SuccessSequenceKey : entry.IntroductionSequenceKey;
                return !string.IsNullOrWhiteSpace(sequenceKey);
            }

            return false;
        }
    }
}
