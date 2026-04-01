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

        private readonly NetworkList<AudioWordConsensusPlayerWordAssignmentNetworkState> wordAssignments = new();
        private readonly NetworkVariable<int> activeRoundIndex = new(-1);
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
            return audioWordConsensusMinigameConfig == null ? null : audioWordConsensusMinigameConfig.GetRoundDefinition(activeRoundIndex.Value);
        }

        public bool TryGetAssignedWordForLocalPlayer(out string assignedWord)
        {
            return TryGetAssignedWord(GetLocalClientId(), out assignedWord);
        }

        public bool CanLocalSubmitAssignedWord()
        {
            return Stage == CooperativeMinigameStage.Playing &&
                   !IsRoundLocked &&
                   !IsLocalEmitter &&
                   TryGetAssignedWordForLocalPlayer(out _);
        }

        public void SubmitLocalAssignedWord()
        {
            if (!CanLocalSubmitAssignedWord())
            {
                return;
            }

            if (IsServer)
            {
                SubmitAssignedWordServer(GetLocalClientId());
                return;
            }

            SubmitAssignedWordServerRpc();
        }

        protected override void InitializeMinigameServer()
        {
            assignmentSeed = Environment.TickCount;
            serverGameplayActive = false;
            correctRoundCount.Value = 0;
            incorrectRoundCount.Value = 0;
            activeRoundIndex.Value = -1;
            activeEmitterClientId.Value = ulong.MaxValue;
            totalScheduledRounds.Value = 0;
            remainingTimeSeconds.Value = audioWordConsensusMinigameConfig == null ? 0f : audioWordConsensusMinigameConfig.TimeLimitSeconds;
            isRoundLocked.Value = false;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando la secuencia cooperativa de sonidos.");
            wordAssignments.Clear();

            var participantIds = GetParticipantIds();
            if (audioWordConsensusMinigameConfig == null)
            {
                PublishResultServer(new MinigameResultData("Configuracion invalida", 0f, 0, 0));
                return;
            }

            if (!audioWordConsensusMinigameConfig.SupportsParticipantCount(participantIds.Count))
            {
                PublishResultServer(new MinigameResultData("Configuracion insuficiente", 0f, 0, 0));
                return;
            }

            totalScheduledRounds.Value = participantIds.Count;
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
            BeginRoundServer(0);
        }

        [Rpc(SendTo.Server)]
        private void SubmitAssignedWordServerRpc(RpcParams rpcParams = default)
        {
            SubmitAssignedWordServer(rpcParams.Receive.SenderClientId);
        }

        private void SubmitAssignedWordServer(ulong senderClientId)
        {
            if (!IsServer || Stage != CooperativeMinigameStage.Playing || !serverGameplayActive || IsRoundLocked || HasPublishedResult)
            {
                return;
            }

            if (senderClientId == activeEmitterClientId.Value)
            {
                return;
            }

            if (!TryGetAssignedWord(senderClientId, out var selectedWord))
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

            var participantIds = GetParticipantIds();
            if (roundIndex < 0 || roundIndex >= participantIds.Count)
            {
                CompleteMinigameServer(completedAllRounds: true);
                return;
            }

            var roundDefinition = audioWordConsensusMinigameConfig.GetRoundDefinition(roundIndex);
            if (roundDefinition == null)
            {
                PublishResultServer(new MinigameResultData("Ronda no configurada", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            var emitterClientId = participantIds[roundIndex];
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
                wordAssignments.Add(new AudioWordConsensusPlayerWordAssignmentNetworkState
                {
                    ClientId = assignment.Key,
                    AssignedWord = new FixedString128Bytes(assignment.Value)
                });
            }

            activeRoundIndex.Value = roundIndex;
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

        private bool TryGetAssignedWord(ulong clientId, out string assignedWord)
        {
            for (var index = 0; index < wordAssignments.Count; index++)
            {
                if (wordAssignments[index].ClientId == clientId)
                {
                    assignedWord = wordAssignments[index].AssignedWord.ToString();
                    return true;
                }
            }

            assignedWord = string.Empty;
            return false;
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
