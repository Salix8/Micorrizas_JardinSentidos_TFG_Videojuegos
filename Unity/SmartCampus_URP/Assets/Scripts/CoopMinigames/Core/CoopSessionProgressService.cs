using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    public static class CoopSessionProgressService
    {
        public static List<CoopMinigameProgressNetworkState> CreateDefaultStates(int minigameCount)
        {
            var states = new List<CoopMinigameProgressNetworkState>(Mathf.Max(0, minigameCount));
            for (var index = 0; index < minigameCount; index++)
            {
                states.Add(CreateDefaultState(index));
            }

            return states;
        }

        public static CoopMinigameProgressNetworkState CreateDefaultState(int minigameIndex)
        {
            return new CoopMinigameProgressNetworkState
            {
                MinigameIndex = minigameIndex,
                IsCompleted = false,
                ScoreOutOfTen = 0f,
                SuccessfulActions = 0,
                FailedActions = 0,
                CompletionOrder = -1,
                ResultMessage = default
            };
        }

        public static bool TryRegisterResult(
            ref CoopMinigameProgressNetworkState state,
            int minigameIndex,
            MinigameResultData result,
            int completionOrder)
        {
            if (state.MinigameIndex != minigameIndex || state.IsCompleted)
            {
                return false;
            }

            state.IsCompleted = true;
            state.ScoreOutOfTen = Mathf.Clamp(result.ScoreOutOfTen, 0f, 10f);
            state.SuccessfulActions = Mathf.Max(0, result.SuccessfulActions);
            state.FailedActions = Mathf.Max(0, result.FailedActions);
            state.CompletionOrder = Mathf.Max(0, completionOrder);
            state.ResultMessage = new FixedString128Bytes(result.Message ?? string.Empty);
            return true;
        }

        public static int CountCompleted(IReadOnlyList<CoopMinigameProgressNetworkState> states)
        {
            return CountCompleted(states, states?.Count ?? 0);
        }

        public static int CountCompleted(IReadOnlyList<CoopMinigameProgressNetworkState> states, int playableMinigameCount)
        {
            if (states == null)
            {
                return 0;
            }

            var completedCount = 0;
            var cappedCount = Mathf.Clamp(playableMinigameCount, 0, states.Count);
            for (var index = 0; index < cappedCount; index++)
            {
                if (states[index].IsCompleted)
                {
                    completedCount += 1;
                }
            }

            return completedCount;
        }

        public static bool AreAllCompleted(IReadOnlyList<CoopMinigameProgressNetworkState> states)
        {
            return states != null && states.Count > 0 && CountCompleted(states) == states.Count;
        }

        public static bool AreAllCompleted(IReadOnlyList<CoopMinigameProgressNetworkState> states, int playableMinigameCount)
        {
            var cappedCount = states == null ? 0 : Mathf.Clamp(playableMinigameCount, 0, states.Count);
            return cappedCount > 0 && CountCompleted(states, cappedCount) == cappedCount;
        }

        public static float CalculateAverageScore(IReadOnlyList<CoopMinigameProgressNetworkState> states)
        {
            return CalculateAverageScore(states, states?.Count ?? 0);
        }

        public static float CalculateAverageScore(IReadOnlyList<CoopMinigameProgressNetworkState> states, int playableMinigameCount)
        {
            if (states == null || states.Count == 0)
            {
                return 0f;
            }

            var scoreSum = 0f;
            var completedCount = 0;
            var cappedCount = Mathf.Clamp(playableMinigameCount, 0, states.Count);
            for (var index = 0; index < cappedCount; index++)
            {
                if (!states[index].IsCompleted)
                {
                    continue;
                }

                scoreSum += states[index].ScoreOutOfTen;
                completedCount += 1;
            }

            return completedCount <= 0 ? 0f : scoreSum / completedCount;
        }
    }
}
