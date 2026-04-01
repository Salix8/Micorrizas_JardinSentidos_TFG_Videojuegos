using System;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    public struct GardenImageVotingPlayerProgressNetworkState : INetworkSerializable, IEquatable<GardenImageVotingPlayerProgressNetworkState>
    {
        public ulong ClientId;
        public int CurrentCardIndex;
        public int CorrectAnswers;
        public int IncorrectAnswers;
        public bool HasCompleted;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref CurrentCardIndex);
            serializer.SerializeValue(ref CorrectAnswers);
            serializer.SerializeValue(ref IncorrectAnswers);
            serializer.SerializeValue(ref HasCompleted);
        }

        public bool Equals(GardenImageVotingPlayerProgressNetworkState other)
        {
            return ClientId == other.ClientId &&
                   CurrentCardIndex == other.CurrentCardIndex &&
                   CorrectAnswers == other.CorrectAnswers &&
                   IncorrectAnswers == other.IncorrectAnswers &&
                   HasCompleted == other.HasCompleted;
        }

        public override bool Equals(object obj)
        {
            return obj is GardenImageVotingPlayerProgressNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, CurrentCardIndex, CorrectAnswers, IncorrectAnswers, HasCompleted);
        }
    }
}
