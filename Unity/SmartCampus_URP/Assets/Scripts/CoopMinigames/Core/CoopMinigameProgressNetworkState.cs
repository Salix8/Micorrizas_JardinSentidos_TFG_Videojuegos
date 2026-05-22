using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames
{
    public struct CoopMinigameProgressNetworkState : INetworkSerializable, IEquatable<CoopMinigameProgressNetworkState>
    {
        public int MinigameIndex;
        public bool IsCompleted;
        public float ScoreOutOfTen;
        public int SuccessfulActions;
        public int FailedActions;
        public int CompletionOrder;
        public FixedString128Bytes ResultMessage;

        public readonly MinigameResultData ToResultData()
        {
            return new MinigameResultData(
                ResultMessage.ToString(),
                ScoreOutOfTen,
                SuccessfulActions,
                FailedActions);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MinigameIndex);
            serializer.SerializeValue(ref IsCompleted);
            serializer.SerializeValue(ref ScoreOutOfTen);
            serializer.SerializeValue(ref SuccessfulActions);
            serializer.SerializeValue(ref FailedActions);
            serializer.SerializeValue(ref CompletionOrder);
            serializer.SerializeValue(ref ResultMessage);
        }

        public readonly bool Equals(CoopMinigameProgressNetworkState other)
        {
            return MinigameIndex == other.MinigameIndex &&
                   IsCompleted == other.IsCompleted &&
                   ScoreOutOfTen.Equals(other.ScoreOutOfTen) &&
                   SuccessfulActions == other.SuccessfulActions &&
                   FailedActions == other.FailedActions &&
                   CompletionOrder == other.CompletionOrder &&
                   ResultMessage.Equals(other.ResultMessage);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is CoopMinigameProgressNetworkState other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(
                MinigameIndex,
                IsCompleted,
                ScoreOutOfTen,
                SuccessfulActions,
                FailedActions,
                CompletionOrder,
                ResultMessage.GetHashCode());
        }
    }
}
