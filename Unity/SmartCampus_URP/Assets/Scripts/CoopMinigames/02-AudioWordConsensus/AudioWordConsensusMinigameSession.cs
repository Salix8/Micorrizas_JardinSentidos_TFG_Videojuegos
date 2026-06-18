using System;
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

        private readonly NetworkVariable<int> activeRoundIndex = new(-1);
        private readonly NetworkVariable<int> activeRoundDefinitionIndex = new(-1);
        private readonly NetworkVariable<ulong> activeEmitterClientId = new(ulong.MaxValue);
        private readonly NetworkVariable<int> activeRoundOptionSeed = new();
        private readonly NetworkVariable<int> correctRoundCount = new();
        private readonly NetworkVariable<int> incorrectRoundCount = new();
        private readonly NetworkVariable<int> totalScheduledRounds = new();
        private readonly NetworkVariable<float> remainingTimeSeconds = new();
        private readonly NetworkVariable<bool> isRoundLocked = new();
        private readonly NetworkVariable<int> activeRoundMistakeCount = new();
        private readonly NetworkVariable<int> totalMistakeCount = new();
        private readonly NetworkVariable<int> activeRevealStageIndex = new();
        private readonly NetworkVariable<AudioWordConsensusRoundPhase> activeRoundPhase = new(AudioWordConsensusRoundPhase.Guessing);
        private readonly NetworkVariable<bool> activeRoundSolved = new();
        private readonly NetworkVariable<FixedString128Bytes> sharedStatusMessage = new();
        private readonly NetworkList<FixedString128Bytes> disabledOptionWords = new();

        private readonly List<AudioWordConsensusPlannedRound> plannedRounds = new();
        private readonly List<AudioWordConsensusRoundScoreEntry> completedRoundResults = new();
        private bool serverGameplayActive;
        private double gameplayEndServerTime;
        private int randomSeed;
        private int lastPublishedWholeSecond = -1;

        public int ActiveRoundIndex => activeRoundIndex.Value;
        public int CorrectRoundCount => correctRoundCount.Value;
        public int IncorrectRoundCount => incorrectRoundCount.Value;
        public int TotalScheduledRounds => totalScheduledRounds.Value;
        public float RemainingTimeSeconds => remainingTimeSeconds.Value;
        public bool IsRoundLocked => isRoundLocked.Value;
        public int ActiveRoundMistakeCount => activeRoundMistakeCount.Value;
        public int TotalMistakeCount => totalMistakeCount.Value;
        public int ActiveRevealStageIndex => activeRevealStageIndex.Value;
        public AudioWordConsensusRoundPhase ActiveRoundPhase => activeRoundPhase.Value;
        public bool IsAwaitingEmitterContinue => activeRoundPhase.Value == AudioWordConsensusRoundPhase.AwaitingEmitterContinue;
        public bool IsCurrentRoundSolved => activeRoundSolved.Value;
        public string SharedStatusMessage => sharedStatusMessage.Value.ToString();
        public bool IsLocalEmitter => activeEmitterClientId.Value == GetLocalClientId();

        public event Action StateChanged;

        protected override CooperativeMinigameConfigBase GetMinigameConfig()
        {
            return audioWordConsensusMinigameConfig;
        }

        public override void OnNetworkSpawn()
        {
            activeRoundIndex.OnValueChanged += HandleIntChanged;
            activeRoundDefinitionIndex.OnValueChanged += HandleIntChanged;
            activeEmitterClientId.OnValueChanged += HandleEmitterChanged;
            activeRoundOptionSeed.OnValueChanged += HandleIntChanged;
            correctRoundCount.OnValueChanged += HandleIntChanged;
            incorrectRoundCount.OnValueChanged += HandleIntChanged;
            totalScheduledRounds.OnValueChanged += HandleIntChanged;
            remainingTimeSeconds.OnValueChanged += HandleFloatChanged;
            isRoundLocked.OnValueChanged += HandleBoolChanged;
            activeRoundMistakeCount.OnValueChanged += HandleIntChanged;
            totalMistakeCount.OnValueChanged += HandleIntChanged;
            activeRevealStageIndex.OnValueChanged += HandleIntChanged;
            activeRoundPhase.OnValueChanged += HandleRoundPhaseChanged;
            activeRoundSolved.OnValueChanged += HandleBoolChanged;
            sharedStatusMessage.OnValueChanged += HandleStatusChanged;
            disabledOptionWords.OnListChanged += HandleDisabledOptionWordsChanged;

            base.OnNetworkSpawn();
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            activeRoundIndex.OnValueChanged -= HandleIntChanged;
            activeRoundDefinitionIndex.OnValueChanged -= HandleIntChanged;
            activeEmitterClientId.OnValueChanged -= HandleEmitterChanged;
            activeRoundOptionSeed.OnValueChanged -= HandleIntChanged;
            correctRoundCount.OnValueChanged -= HandleIntChanged;
            incorrectRoundCount.OnValueChanged -= HandleIntChanged;
            totalScheduledRounds.OnValueChanged -= HandleIntChanged;
            remainingTimeSeconds.OnValueChanged -= HandleFloatChanged;
            isRoundLocked.OnValueChanged -= HandleBoolChanged;
            activeRoundMistakeCount.OnValueChanged -= HandleIntChanged;
            totalMistakeCount.OnValueChanged -= HandleIntChanged;
            activeRevealStageIndex.OnValueChanged -= HandleIntChanged;
            activeRoundPhase.OnValueChanged -= HandleRoundPhaseChanged;
            activeRoundSolved.OnValueChanged -= HandleBoolChanged;
            sharedStatusMessage.OnValueChanged -= HandleStatusChanged;
            disabledOptionWords.OnListChanged -= HandleDisabledOptionWordsChanged;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer ||
                !serverGameplayActive ||
                Stage != CooperativeMinigameStage.Playing ||
                HasPublishedResult ||
                activeRoundPhase.Value != AudioWordConsensusRoundPhase.Guessing)
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
            if (Stage != CooperativeMinigameStage.Playing || IsLocalEmitter)
            {
                assignedWords = Array.Empty<string>();
                return false;
            }

            if (!TryBuildCurrentRoundAssignments(out var assignments) ||
                !assignments.TryGetValue(GetLocalClientId(), out var optionWords) ||
                optionWords == null)
            {
                assignedWords = Array.Empty<string>();
                return false;
            }

            assignedWords = optionWords;
            return optionWords.Count > 0;
        }

        public bool CanLocalSubmitAssignedWord()
        {
            if (Stage != CooperativeMinigameStage.Playing ||
                activeRoundPhase.Value != AudioWordConsensusRoundPhase.Guessing ||
                IsLocalEmitter ||
                !TryGetAssignedWordsForLocalPlayer(out var assignedWords))
            {
                return false;
            }

            for (var index = 0; index < assignedWords.Count; index++)
            {
                if (!IsAssignedWordDisabled(assignedWords[index]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanLocalSubmitAssignedWord(string selectedWord)
        {
            if (!CanLocalSubmitAssignedWord() || string.IsNullOrWhiteSpace(selectedWord))
            {
                return false;
            }

            if (IsAssignedWordDisabled(selectedWord))
            {
                return false;
            }

            if (!TryGetAssignedWordsForLocalPlayer(out var assignedWords))
            {
                return false;
            }

            for (var index = 0; index < assignedWords.Count; index++)
            {
                if (string.Equals(assignedWords[index], selectedWord, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanLocalAdvanceClosedRound()
        {
            return AudioWordConsensusGameplayRules.CanAdvanceFromRoundClosure(Stage, activeRoundPhase.Value, IsLocalEmitter);
        }

        public bool IsAssignedWordDisabled(string selectedWord)
        {
            if (string.IsNullOrWhiteSpace(selectedWord))
            {
                return false;
            }

            for (var index = 0; index < disabledOptionWords.Count; index++)
            {
                if (string.Equals(disabledOptionWords[index].ToString(), selectedWord, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAwaitingLocalAssignedWords()
        {
            return false;
        }

        public void SubmitLocalAssignedWord(string selectedWord)
        {
            if (!CanLocalSubmitAssignedWord(selectedWord))
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

        public void RequestAdvanceClosedRound()
        {
            if (!CanLocalAdvanceClosedRound())
            {
                return;
            }

            if (IsServer)
            {
                AdvanceClosedRoundServer(GetLocalClientId());
                return;
            }

            RequestAdvanceClosedRoundServerRpc();
        }

        private bool TryBuildCurrentRoundAssignments(out Dictionary<ulong, List<string>> assignments)
        {
            assignments = null;

            var roundDefinition = GetCurrentRoundDefinition();
            if (roundDefinition == null)
            {
                return false;
            }

            var receiverClientIds = new List<ulong>();
            var participantIds = GetParticipantIds();
            for (var index = 0; index < participantIds.Count; index++)
            {
                var participantId = participantIds[index];
                if (participantId == activeEmitterClientId.Value)
                {
                    continue;
                }

                receiverClientIds.Add(participantId);
            }

            return AudioWordConsensusWordAssignmentService.TryBuildAssignments(
                receiverClientIds,
                roundDefinition.CorrectWord,
                roundDefinition.DistractorWords,
                activeRoundOptionSeed.Value,
                out assignments);
        }

        protected override void InitializeMinigameServer()
        {
            randomSeed = Environment.TickCount;
            serverGameplayActive = false;
            correctRoundCount.Value = 0;
            incorrectRoundCount.Value = 0;
            activeRoundIndex.Value = -1;
            activeRoundDefinitionIndex.Value = -1;
            activeEmitterClientId.Value = ulong.MaxValue;
            activeRoundOptionSeed.Value = 0;
            totalScheduledRounds.Value = 0;
            remainingTimeSeconds.Value = audioWordConsensusMinigameConfig == null ? 0f : audioWordConsensusMinigameConfig.TimeLimitSeconds;
            isRoundLocked.Value = false;
            activeRoundMistakeCount.Value = 0;
            totalMistakeCount.Value = 0;
            activeRevealStageIndex.Value = 0;
            activeRoundPhase.Value = AudioWordConsensusRoundPhase.Guessing;
            activeRoundSolved.Value = false;
            sharedStatusMessage.Value = new FixedString128Bytes("Preparando la secuencia cooperativa de sonidos.");
            plannedRounds.Clear();
            completedRoundResults.Clear();
            ClearDisabledOptionWordsServer();

            var participantIds = GetParticipantIds();
            if (audioWordConsensusMinigameConfig == null)
            {
                sharedStatusMessage.Value = new FixedString128Bytes("Configuracion invalida.");
                SetBlockingErrorServer("Configuracion invalida");
                return;
            }

            if (!audioWordConsensusMinigameConfig.TryValidateForParticipantCount(participantIds.Count, out var validationError))
            {
                sharedStatusMessage.Value = new FixedString128Bytes(validationError);
                SetBlockingErrorServer(validationError);
                return;
            }
        }

        protected override void OnGameplayStartedServer()
        {
            if (!IsServer || audioWordConsensusMinigameConfig == null || HasPublishedResult || HasBlockingError)
            {
                return;
            }

            serverGameplayActive = true;
            gameplayEndServerTime = NetworkManager.ServerTime.Time + audioWordConsensusMinigameConfig.TimeLimitSeconds;
            remainingTimeSeconds.Value = audioWordConsensusMinigameConfig.TimeLimitSeconds;
            lastPublishedWholeSecond = Mathf.CeilToInt(audioWordConsensusMinigameConfig.TimeLimitSeconds);

            if (!TryBuildRoundPlanServer(out var errorMessage))
            {
                sharedStatusMessage.Value = new FixedString128Bytes(errorMessage);
                SetBlockingErrorServer(errorMessage);
                return;
            }

            BeginRoundServer(0);
        }

        [Rpc(SendTo.Server)]
        private void SubmitAssignedWordServerRpc(FixedString128Bytes selectedWord, RpcParams rpcParams = default)
        {
            SubmitAssignedWordServer(rpcParams.Receive.SenderClientId, selectedWord.ToString());
        }

        [Rpc(SendTo.Server)]
        private void RequestAdvanceClosedRoundServerRpc(RpcParams rpcParams = default)
        {
            AdvanceClosedRoundServer(rpcParams.Receive.SenderClientId);
        }

        private void SubmitAssignedWordServer(ulong senderClientId, string selectedWord)
        {
            if (!IsServer ||
                Stage != CooperativeMinigameStage.Playing ||
                !serverGameplayActive ||
                HasPublishedResult ||
                activeRoundPhase.Value != AudioWordConsensusRoundPhase.Guessing)
            {
                return;
            }

            if (senderClientId == activeEmitterClientId.Value || IsAssignedWordDisabled(selectedWord))
            {
                return;
            }

            var roundDefinition = GetCurrentRoundDefinition();
            if (roundDefinition == null)
            {
                PublishResultServer(new MinigameResultData("Ronda invalida", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            if (!TryBuildCurrentRoundAssignments(out var assignments) ||
                !assignments.TryGetValue(senderClientId, out var validOptions) ||
                validOptions == null ||
                validOptions.Count == 0)
            {
                return;
            }

            var isValidOption = false;
            for (var index = 0; index < validOptions.Count; index++)
            {
                if (string.Equals(validOptions[index], selectedWord, StringComparison.OrdinalIgnoreCase))
                {
                    selectedWord = validOptions[index];
                    isValidOption = true;
                    break;
                }
            }

            if (!isValidOption)
            {
                return;
            }

            var wasCorrect = string.Equals(selectedWord, roundDefinition.CorrectWord, StringComparison.OrdinalIgnoreCase);
            var submissionOutcome = AudioWordConsensusGameplayRules.EvaluateSubmission(
                wasCorrect,
                activeRoundMistakeCount.Value,
                audioWordConsensusMinigameConfig.MaxMistakesPerRound,
                audioWordConsensusMinigameConfig.RevealStageCount);

            activeRoundMistakeCount.Value = submissionOutcome.NextMistakeCount;
            activeRevealStageIndex.Value = submissionOutcome.NextRevealStageIndex;
            activeRoundPhase.Value = submissionOutcome.NextPhase;
            activeRoundSolved.Value = submissionOutcome.WasCorrect;
            isRoundLocked.Value = submissionOutcome.NextPhase != AudioWordConsensusRoundPhase.Guessing;

            if (!wasCorrect)
            {
                AddDisabledOptionWordServer(selectedWord);
                totalMistakeCount.Value += 1;
            }

            if (!submissionOutcome.ShouldEnterClosure)
            {
                sharedStatusMessage.Value = new FixedString128Bytes(
                    $"Incorrecto. Se revela una nueva pista visual. Intento {activeRoundMistakeCount.Value}/{audioWordConsensusMinigameConfig.MaxMistakesPerRound}.");
                return;
            }

            if (submissionOutcome.ShouldCountAsSolvedRound)
            {
                correctRoundCount.Value += 1;
            }
            else if (submissionOutcome.ShouldCountAsFailedRound)
            {
                incorrectRoundCount.Value += 1;
            }

            completedRoundResults.Add(new AudioWordConsensusRoundScoreEntry(
                submissionOutcome.ShouldCountAsSolvedRound,
                submissionOutcome.NextMistakeCount));

            PauseRoundTimerServer();
            sharedStatusMessage.Value = submissionOutcome.ShouldCountAsSolvedRound
                ? new FixedString128Bytes($"Correcto. La respuesta era {roundDefinition.CorrectWord}.")
                : new FixedString128Bytes($"Intentos agotados. La respuesta correcta era {roundDefinition.CorrectWord}.");
        }

        private void AdvanceClosedRoundServer(ulong senderClientId)
        {
            if (!IsServer ||
                Stage != CooperativeMinigameStage.Playing ||
                HasPublishedResult ||
                activeRoundPhase.Value != AudioWordConsensusRoundPhase.AwaitingEmitterContinue ||
                senderClientId != activeEmitterClientId.Value)
            {
                return;
            }

            var nextRoundIndex = activeRoundIndex.Value + 1;
            var completedAllRounds = nextRoundIndex >= totalScheduledRounds.Value;
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
            if (roundDefinition == null || !AudioWordConsensusRoundDefinitionValidator.IsUsable(roundDefinition))
            {
                PublishResultServer(new MinigameResultData("Ronda no configurada correctamente", 0f, correctRoundCount.Value, incorrectRoundCount.Value));
                return;
            }

            activeRoundIndex.Value = roundIndex;
            activeRoundDefinitionIndex.Value = plannedRound.RoundDefinitionIndex;
            activeEmitterClientId.Value = plannedRound.EmitterClientId;
            activeRoundOptionSeed.Value = randomSeed++;
            activeRoundMistakeCount.Value = 0;
            activeRevealStageIndex.Value = 0;
            activeRoundPhase.Value = AudioWordConsensusRoundPhase.Guessing;
            activeRoundSolved.Value = false;
            isRoundLocked.Value = false;
            ClearDisabledOptionWordsServer();
            sharedStatusMessage.Value = new FixedString128Bytes(
                $"Turno del dispositivo {GetDisplaySlotForClient(plannedRound.EmitterClientId)}. Escuchad el sonido y elegid una respuesta.");

            if (serverGameplayActive)
            {
                gameplayEndServerTime = NetworkManager.ServerTime.Time + remainingTimeSeconds.Value;
                lastPublishedWholeSecond = Mathf.CeilToInt(remainingTimeSeconds.Value);
            }
        }

        private void PauseRoundTimerServer()
        {
            if (!IsServer || !serverGameplayActive || NetworkManager == null)
            {
                return;
            }

            remainingTimeSeconds.Value = Mathf.Max(0f, (float)(gameplayEndServerTime - NetworkManager.ServerTime.Time));
            lastPublishedWholeSecond = Mathf.CeilToInt(remainingTimeSeconds.Value);
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
                completedRoundResults,
                correctRoundCount.Value,
                incorrectRoundCount.Value,
                totalScheduledRounds.Value,
                completedAllRounds));
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

            if (!AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                    GetParticipantIds(),
                    audioWordConsensusMinigameConfig.RoundDefinitions,
                    MaximumScheduledRoundCount,
                    randomSeed++,
                    out var newPlannedRounds,
                    out errorMessage))
            {
                return false;
            }

            plannedRounds.AddRange(newPlannedRounds);
            totalScheduledRounds.Value = plannedRounds.Count;
            return plannedRounds.Count > 0;
        }

        private void AddDisabledOptionWordServer(string selectedWord)
        {
            if (string.IsNullOrWhiteSpace(selectedWord) || IsAssignedWordDisabled(selectedWord))
            {
                return;
            }

            disabledOptionWords.Add(new FixedString128Bytes(selectedWord.Trim()));
        }

        private void ClearDisabledOptionWordsServer()
        {
            if (disabledOptionWords.Count > 0)
            {
                disabledOptionWords.Clear();
            }
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

        private void HandleRoundPhaseChanged(AudioWordConsensusRoundPhase _, AudioWordConsensusRoundPhase __)
        {
            StateChanged?.Invoke();
        }

        private void HandleStatusChanged(FixedString128Bytes _, FixedString128Bytes __)
        {
            StateChanged?.Invoke();
        }

        private void HandleDisabledOptionWordsChanged(NetworkListEvent<FixedString128Bytes> _)
        {
            StateChanged?.Invoke();
        }
    }
}
