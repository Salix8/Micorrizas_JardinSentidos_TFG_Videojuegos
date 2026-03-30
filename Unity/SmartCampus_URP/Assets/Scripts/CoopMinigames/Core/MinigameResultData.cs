using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames
{
    public readonly struct MinigameResultData
    {
        public MinigameResultData(string message, float scoreOutOfTen, int successfulActions, int failedActions)
        {
            Message = message ?? string.Empty;
            ScoreOutOfTen = scoreOutOfTen;
            SuccessfulActions = successfulActions;
            FailedActions = failedActions;
        }

        public string Message { get; }
        public float ScoreOutOfTen { get; }
        public int SuccessfulActions { get; }
        public int FailedActions { get; }
    }

    public struct MinigameResultNetworkState : INetworkSerializable, IEquatable<MinigameResultNetworkState>
    {
        public bool HasValue;
        public float ScoreOutOfTen;
        public int SuccessfulActions;
        public int FailedActions;
        public FixedString128Bytes Message;

        public MinigameResultData ToData()
        {
            return new MinigameResultData(Message.ToString(), ScoreOutOfTen, SuccessfulActions, FailedActions);
        }

        public static MinigameResultNetworkState FromData(MinigameResultData data)
        {
            return new MinigameResultNetworkState
            {
                HasValue = true,
                ScoreOutOfTen = data.ScoreOutOfTen,
                SuccessfulActions = data.SuccessfulActions,
                FailedActions = data.FailedActions,
                Message = new FixedString128Bytes(data.Message ?? string.Empty)
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HasValue);
            serializer.SerializeValue(ref ScoreOutOfTen);
            serializer.SerializeValue(ref SuccessfulActions);
            serializer.SerializeValue(ref FailedActions);
            serializer.SerializeValue(ref Message);
        }

        public bool Equals(MinigameResultNetworkState other)
        {
            return HasValue == other.HasValue &&
                   ScoreOutOfTen.Equals(other.ScoreOutOfTen) &&
                   SuccessfulActions == other.SuccessfulActions &&
                   FailedActions == other.FailedActions &&
                   Message.Equals(other.Message);
        }

        public override bool Equals(object obj)
        {
            return obj is MinigameResultNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HasValue, ScoreOutOfTen, SuccessfulActions, FailedActions, Message.GetHashCode());
        }
    }
}
