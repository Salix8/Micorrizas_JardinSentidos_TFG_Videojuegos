using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    [DisallowMultipleComponent]
    public sealed class AudioWordConsensusMinigameSession : CooperativeMinigameBase
    {
        [SerializeField] private AudioWordConsensusMinigameConfig audioWordConsensusMinigameConfig;
        private const int MaximumScheduledRoundCount = 6;

        private readonly NetworkList<AudioWordConsensusPlayerWordAssignmentNetworkState> wordAssignments = new();
        private readonly NetworkVariable<int> activeRoundIndex = new(-1);
        private readonly NetworkVariable<int> activeRoundDefinitionIndex = new(-1);
        private readonly NetworkVariable<ulong> activeEmitterClientId = new(ulong.MaxValue);
        private readonly NetworkVariable<int> correctRoundCount = new();
        private readonly NetworkVariable<int> incorrectRoundCount = new();
        private readonly NetworkVariable<int> totalScheduledRounds = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<bool> isRoundLocked = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();

        private Coroutine pendingRoundTransitionCoroutine;
        private bool serverGameplayActive;
        private double gameplayEndServerTime;
        private int assignmentSeed;
        private int lastPublishedWholeSecond = -1;
        private readonly List<AudioWordConsensusPlannedRound> plannedRounds = new();

        public int ActiveRoundIndex => activeRoundIndex.Value;
        public int CorrectRoundCount => correctRoundCount.Value;
        public int IncorrectRoundCount => incorrectRoundCount.Value;
        public int TotalScheduledRounds => totalScheduledRounds.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public bool IsRoundLocked => isRoundLocked.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();
        public bool IsLocalEmitter => activeEmitterClientId.Value == GetLocalClientId();

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return audioWordConsensusMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            wordAssignments.OnListChanged += HandleWordAssignmentsChanged;
            activeRoundIndex.OnValueChanged += HandleIntChanged;
            activeRoundDefinitionIndex.OnValueChanged += HandleIntChanged;
            activeEmitterClientId.OnValueChanged += HandleEmitterChanged;
            correctRoundCount.OnValueChanged += HandleIntChanged;
            incorrectRoundCount.OnValueChanged += HandleIntChanged;
            totalScheduledRounds.OnValueChanged += HandleIntChanged;
            remainingTimeSeconds.OnValueChanged += HandleFloatChanged;
            isRoundLocked.OnValueChanged += HandleBoolChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusChanged;

            base.OnNetworkSpawn();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            wordAssignments.OnListChanged -= HandleWordAssignmentsChanged;
            activeRoundIndex.OnValueChanged -= HandleIntChanged;
            activeRoundDefinitionIndex.OnValueChanged -= HandleIntChanged;
            activeEmitterClientId.OnValueChanged -= HandleEmitterChanged;
            correctRoundCount.OnValueChanged -= HandleIntChanged;
            incorrectRoundCount.OnValueChanged -= HandleIntChanged;
            totalScheduledRounds.OnValueChanged -= HandleIntChanged;
            remainingTimeSeconds.OnValueChanged -= HandleFloatChanged;
            isRoundLocked.OnValueChanged -= HandleBoolChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusChanged;

            if (pendingRoundTransitionCoroutine != null)
            {
                StopCoroutine(pendingRoundTransitionCoroutine);
                pendingRoundTransitionCoroutine = null;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !serverGameplayActive || Stage != CooperativeMinigameStage.Playing || HasPublishedResult)
            {
                return;
            }

            var remainingSeconds = Mathf.Max(0f, (float)(gameplayEndServerTime - NetworkManager.ServerTime.Time));
            var wholeSecond = Mathf.CeilToInt(remainingSeconds);
            if (wholeSecond != lastPublishedWholeSecond)
            {
                remainingTimeSeconds.Value = remainingSeconds;
                lastPublishedWholeSecond = wholeSecond;
            }

            if (remainingSeconds <= 0f)
            {
                CompleteMinigameServer(completedAllRounds: false);
            }
        }

        public AudioWordConsensusRoundDefinition GetCurrentRoundDefinition()
        {
            return audioWordConsensusMinigameConfig == null ? null : audioWordConsensusMinigameConfig.GetRoundDefinition(activeRoundDefinitionIndex.Value);
        }

        public bool TryGetAssignedWordForLocalPlayer(out string assignedWord)
        {
            if (TryGetAssignedWordsForLocalPlayer(out var assignedWords) && assignedWords.Count > 0)
            {
                assignedWord = assignedWords[0];
                return true;
            }

            assignedWord = string.Empty;
            return false;
        }

        public bool TryGetAssignedWordsForLocalPlayer(out IReadOnlyList<string> assignedWords)
        {
            return TryGetAssignedWords(GetLocalClientId(), out assignedWords);
        }

        public bool CanLocalSubmitAssignedWord()
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   !IsRoundLocked &&
                   !IsLocalEmitter &&
                   TryGetAssignedWordsForLocalPlayer(out var assignedWords) &&
                   assignedWords.Count > 0;
        }

        public void SubmitLocalAssignedWord(string selectedWord)
        {
            if (!CanLocalSubmitAssignedWord() || string.IsNullOrWhiteSpace(selectedWord))
            {
                return;
            }

            if (IsServer)
            {
                SubmitAssignedWordServer(GetLocalClientId(), selectedWord);
                return;
            }

            SubmitAssignedWordServerRpc(selectedWord);
        }

        protected override void InitializeMinigameServer()
        {
            assignmentSeed = Environment.TickCount;
            serverGameplayActive = false;
            correctRoundCount.Value = 0;
            incorrectRoundCount.Value = 0;
            activeRoundIndex.Value = -1;
            activeRoundDefinitionIndex.Value = -1;
            activeEmitterClientId.Value = ulong.MaxValue;
            totalScheduledRounds.Value = 0;
            remainingTimeSeconds.Value = audioWordConsensusMinigameConfig == null ? 0f : audioWordConsensusMinigameConfig.TimeLimitSeconds;
            isRoundLocked.Value = false;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando la secuencia cooperativa de sonidos.");
            wordAssignments.Clear();
            plannedRounds.Clear();

            var participantIds = GetParticipantIds();
            if (audioWordConsensusMinigameConfig == null)
            {
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            if (!audioWordConsensusMinigameConfig.TryValidateForParticipantCount(participantIds.Count, out var validationError))
            {
                PublishResultServer(new MinigameResultData(validationError, 0f, 0, 0));
                return;
            }

        }

        protected override void OnGameplayStartedServer()
        {
            if (!IsServer || audioWordConsensusMinigameConfig == null || HasPublishedResult)
            {
                return;
            }

            serverGameplayActive = true;
            gameplayEndServerTime = NetworkManager.ServerTime.Time + audioWordConsensusMinigameConfig.TimeLimitSeconds;
            remainingTimeSeconds.Value = audioWordConsensusMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = Mathf.CeilToInt(audioWordConsensusMinigameConfig.TimeLimitSeconds);
            if (!TryBuildRoundPlanServer(out var errorMessage))
            {
                PublishResultServer(new MinigameResultData(errorMessage, 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            BeginRoundServer(0);
        }

        [Rpc(SendTo.Server)]
        private void SubmitAssignedWordServerRpc(FixedString128Bytes selectedWord, RpcParams rpcParams = default)
        {
            SubmitAssignedWordServer(rpcParams.Receive.SenderClientId, selectedWord.ToString());
        }

        private void SubmitAssignedWordServer(ulong senderClientId, string selectedWord)
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing || !serverGameplayActive || IsRoundLocked || HasPublishedResult)
            {
                return;
            }

            if (senderClientId == activeEmitterClientId.Value)
            {
                return;
            }

            if (!TryGetAssignedWords(senderClientId, out var assignedWords))
            {
                return;
            }

            var isAssignedWord = false;
            for (var index = 0; index < assignedWords.Count; index++)
            {
                if (string.Equals(assignedWords[index], selectedWord, StringComparison.OrdinalIgnoreCase))
                {
                    isAssignedWord = true;
                    selectedWord = assignedWords[index];
                    break;
                }
            }

            if (!isAssignedWord)
            {
                return;
            }

            var roundDefinition = GetCurrentRoundDefinition();
            if (roundDefinition == null)
            {
                PublishResultServer(new MinigameResultData("Ronda invalida", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            var wasCorrect = string.Equals(selectedWord, roundDefinition.CorrectWord, StringComparison.OrdinalIgnoreCase);
            if (wasCorrect)
            {
                correctRoundCount.Value += 1;
            }
            else
            {
                incorrectRoundCount.Value += 1;
            }

            isRoundLocked.Value = true;
            sharedStatusMessage.Value = wasCorrect
                ? new FixedString128Bytes($"Decision correcta. La respuesta era {roundDefinition.CorrectWord}.")
                : new FixedString128Bytes($"Decision incorrecta. La respuesta correcta era {roundDefinition.CorrectWord}.");

            var nextRoundIndex = activeRoundIndex.Value + 1;
            var completedAllRounds = nextRoundIndex >= totalScheduledRounds.Value;
            QueueRoundTransitionServer(nextRoundIndex, completedAllRounds);
        }

        private void QueueRoundTransitionServer(int nextRoundIndex, bool completedAllRounds)
        {
            if (pendingRoundTransitionCoroutine != null)
            {
                StopCoroutine(pendingRoundTransitionCoroutine);
            }

            pendingRoundTransitionCoroutine = StartCoroutine(AdvanceRoundAfterFeedbackCoroutine(nextRoundIndex, completedAllRounds));
        }

        private IEnumerator AdvanceRoundAfterFeedbackCoroutine(int nextRoundIndex, bool completedAllRounds)
        {
            var delay = audioWordConsensusMinigameConfig == null ? 0f : audioWordConsensusMinigameConfig.FeedbackDurationSeconds;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            pendingRoundTransitionCoroutine = null;

            if (!IsServer || HasPublishedResult)
            {
                yield break;
            }

            if (completedAllRounds)
            {
                CompleteMinigameServer(completedAllRounds: true);
            }
            else
            {
                BeginRoundServer(nextRoundIndex);
            }
        }

        private void BeginRoundServer(int roundIndex)
        {
            if (!IsServer || audioWordConsensusMinigameConfig == null)
            {
                return;
            }

            if (roundIndex < 0 || roundIndex >= plannedRounds.Count || roundIndex >= totalScheduledRounds.Value)
            {
                CompleteMinigameServer(completedAllRounds: true);
                return;
            }

            var plannedRound = plannedRounds[roundIndex];
            var roundDefinition = audioWordConsensusMinigameConfig.GetRoundDefinition(plannedRound.RoundDefinitionIndex);
            if (roundDefinition == null || !roundDefinition.IsUsableForReceiverCount(Mathf.Max(1, GetParticipantIds().Count - 1)))
            {
                AudioWordConsensusRoundDefinitionValidator.TryValidate(
                    roundDefinition,
                    Mathf.Max(1, GetParticipantIds().Count - 1),
                    out var roundError);
                PublishResultServer(new MinigameResultData($"Ronda no configurada. {roundError}", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            var participantIds = GetParticipantIds();
            var emitterClientId = plannedRound.EmitterClientId;
            var receiverClientIds = new List<ulong>();
            for (var index = 0; index < participantIds.Count; index++)
            {
                if (participantIds[index] != emitterClientId)
                {
                    receiverClientIds.Add(participantIds[index]);
                }
            }

            if (!AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                    receiverClientIds,
                    roundDefinition.CorrectWord,
                    roundDefinition.DistractorWords,
                    assignmentSeed++,
                    out var assignments))
            {
                PublishResultServer(new MinigameResultData("Palabras insuficientes para la ronda", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            wordAssignments.Clear();
            foreach (var assignment in assignments)
            {
                for (var optionIndex = 0; optionIndex < assignment.Value.Count; optionIndex++)
                {
                    wordAssignments.Add(new AudioWordConsensusPlayerWordAssignmentNetworkState
                    {
                        ClientId = assignment.Key,
                        DisplayOrder = optionIndex,
                        AssignedWord = new FixedString128Bytes(assignment.Value[optionIndex])
                    });
                }
            }

            activeRoundIndex.Value = roundIndex;
            activeRoundDefinitionIndex.Value = plannedRound.RoundDefinitionIndex;
            activeEmitterClientId.Value = emitterClientId;
            isRoundLocked.Value = false;
            sharedStatusMessage.Value = new FixedString128Bytes(
                $"Turno del dispositivo {GetDisplaySlotForClient(emitterClientId)}. Escuchad el sonido y tomad una decision conjunta.");
        }

        private void CompleteMinigameServer(bool completedAllRounds)
        {
            if (!IsServer || HasPublishedResult)
            {
                return;
            }

            serverGameplayActive = false;
            isRoundLocked.Value = true;
            remainingTimeSeconds.Value = Mathf.Max(0f, remainingTimeSeconds.Value);
            PublishResultServer(AudioWordConsensusScoreService.CreateResult(
                audioWordConsensusMinigameConfig,
                correctRoundCount.Value,
                incorrectRoundCount.Value,
                totalScheduledRounds.Value,
                completedAllRounds));
        }

        private bool TryGetAssignedWords(ulong clientId, out IReadOnlyList<string> assignedWords)
        {
            var clientAssignments = new List<AudioWordConsensusPlayerWordAssignmentNetworkState>();
            for (var index = 0; index < wordAssignments.Count; index++)
            {
                if (wordAssignments[index].ClientId == clientId)
                {
                    clientAssignments.Add(wordAssignments[index]);
                }
            }

            if (clientAssignments.Count == 0)
            {
                assignedWords = Array.Empty<string>();
                return false;
            }

            clientAssignments.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
            var results = new List<string>(clientAssignments.Count);
            for (var index = 0; index < clientAssignments.Count; index++)
            {
                results.Add(clientAssignments[index].AssignedWord.ToString());
            }

            assignedWords = results;
            return true;
        }

        private int GetDisplaySlotForClient(ulong clientId)
        {
            if (SessionCoordinator != null)
            {
                var slot = SessionCoordinator.GetPlayerSlot(clientId);
                if (slot >= 0)
                {
                    return slot + 1;
                }
            }

            var participantIds = GetParticipantIds();
            for (var index = 0; index < participantIds.Count; index++)
            {
                if (participantIds[index] == clientId)
                {
                    return index + 1;
                }
            }

            return -1;
        }

        private bool TryBuildRoundPlanServer(out string errorMessage)
        {
            errorMessage = string.Empty;
            plannedRounds.Clear();

            if (audioWordConsensusMinigameConfig == null)
            {
                errorMessage = "Configuracion invalida";
                return false;
            }

            var participantIds = GetParticipantIds();
            if (!AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                    participantIds,
                    audioWordConsensusMinigameConfig.RoundDefinitions,
                    MaximumScheduledRoundCount,
                    assignmentSeed++,
                    out var newPlannedRounds,
                    out errorMessage))
            {
                return false;
            }

            plannedRounds.AddRange(newPlannedRounds);
            totalScheduledRounds.Value = plannedRounds.Count;
            return plannedRounds.Count > 0;
        }

        private void HandleWordAssignmentsChanged(NetworkListEvent<AudioWordConsensusPlayerWordAssignmentNetworkState> _)
        {
            StateChanged?.Invoke();
        }

        private void HandleIntChanged(int _, int __)
        {
            StateChanged?.Invoke();
        }

        private void HandleEmitterChanged(ulong _, ulong __)
        {
            StateChanged?.Invoke();
        }

        private void HandleFloatChanged(float _, float __)
        {
            StateChanged?.Invoke();
        }

        private void HandleBoolChanged(bool _, bool __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }
    }
}
