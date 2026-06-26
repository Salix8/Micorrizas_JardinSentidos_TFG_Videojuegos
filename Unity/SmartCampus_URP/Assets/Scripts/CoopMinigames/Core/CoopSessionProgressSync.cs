using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopSessionProgressSync : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private bool persistAcrossScenes = true;

        [Header("Progress Rules")]
        [SerializeField] [Min(1)] private int playableMinigameCount = 5;

        private readonly NetworkList<CoopMinigameProgressNetworkState> progressStates = new();
        private int nextCompletionOrder;

        public int ConfiguredMinigameCount => ResolvePlayableMinigameCount();
        public int TrackedMinigameCount => progressStates.Count;
        public int PlayableMinigameCount => playableMinigameCount;
        public int CompletedCount => CoopSessionProgressService.CountCompleted(GetProgressStatesSnapshot(), ConfiguredMinigameCount);
        public bool AreAllMinigamesCompleted => CoopSessionProgressService.AreAllCompleted(GetProgressStatesSnapshot(), ConfiguredMinigameCount);
        public float AverageScoreOutOfTen => CoopSessionProgressService.CalculateAverageScore(GetProgressStatesSnapshot(), ConfiguredMinigameCount);

        public event Action ProgressChanged;

        private void Awake()
        {
            playableMinigameCount = Mathf.Max(1, playableMinigameCount);
            ResolveReferences();

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnValidate()
        {
            playableMinigameCount = Mathf.Max(1, playableMinigameCount);
        }

        public override void OnNetworkSpawn()
        {
            ResolveReferences();
            progressStates.OnListChanged += HandleProgressStatesChanged;

            if (IsServer)
            {
                SynchronizeConfigurationServer(resetProgress: progressStates.Count == 0);
            }

            ProgressChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            progressStates.OnListChanged -= HandleProgressStatesChanged;
        }

        public bool IsMinigameCompleted(int minigameIndex)
        {
            return TryGetProgressState(minigameIndex, out var state) && state.IsCompleted;
        }

        public bool TryGetResult(int minigameIndex, out MinigameResultData result)
        {
            if (TryGetProgressState(minigameIndex, out var state) && state.IsCompleted)
            {
                result = state.ToResultData();
                return true;
            }

            result = default;
            return false;
        }

        public bool TryGetProgressState(int minigameIndex, out CoopMinigameProgressNetworkState state)
        {
            if (minigameIndex >= 0 && minigameIndex < progressStates.Count)
            {
                state = progressStates[minigameIndex];
                return true;
            }

            state = default;
            return false;
        }

        public IReadOnlyList<CoopMinigameProgressNetworkState> GetProgressStatesSnapshot()
        {
            var snapshot = new List<CoopMinigameProgressNetworkState>(progressStates.Count);
            for (var index = 0; index < progressStates.Count; index++)
            {
                snapshot.Add(progressStates[index]);
            }

            return snapshot;
        }

        public bool TryRegisterResultServer(int minigameIndex, MinigameResultData result)
        {
            if (!IsServer)
            {
                return false;
            }

            SynchronizeConfigurationServer(resetProgress: false);

            if (minigameIndex < 0 || minigameIndex >= progressStates.Count)
            {
                return false;
            }

            var state = progressStates[minigameIndex];
            if (!CoopSessionProgressService.TryRegisterResult(ref state, minigameIndex, result, nextCompletionOrder))
            {
                return false;
            }

            progressStates[minigameIndex] = state;
            nextCompletionOrder += 1;
            return true;
        }

        public void ResetProgressServer()
        {
            if (!IsServer)
            {
                return;
            }

            SynchronizeConfigurationServer(resetProgress: true);
        }

        public void SynchronizeConfigurationServer(bool resetProgress)
        {
            if (!IsServer)
            {
                return;
            }

            ResolveReferences();
            var configuredCount = coopSessionCoordinator == null ? 0 : coopSessionCoordinator.ConfiguredMinigameCount;
            configuredCount = Mathf.Max(0, configuredCount);

            if (!resetProgress && progressStates.Count == configuredCount && progressStates.Count > 0)
            {
                return;
            }

            progressStates.Clear();
            var defaultStates = CoopSessionProgressService.CreateDefaultStates(configuredCount);
            for (var index = 0; index < defaultStates.Count; index++)
            {
                progressStates.Add(defaultStates[index]);
            }

            nextCompletionOrder = 0;
            ProgressChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        }

        private int ResolvePlayableMinigameCount()
        {
            if (progressStates.Count <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(playableMinigameCount, 1, progressStates.Count);
        }

        private void HandleProgressStatesChanged(NetworkListEvent<CoopMinigameProgressNetworkState> _)
        {
            ProgressChanged?.Invoke();
        }
    }
}
