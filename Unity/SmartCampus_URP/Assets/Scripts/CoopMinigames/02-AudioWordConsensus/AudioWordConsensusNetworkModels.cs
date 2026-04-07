using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    public struct AudioWordConsensusPlayerWordAssignmentNetworkState : INetworkSerializable, IEquatable<AudioWordConsensusPlayerWordAssignmentNetworkState>
    {
        public ulong ClientId;
        public int DisplayOrder;
        public FixedString128Bytes AssignedWord;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref DisplayOrder);
            serializer.SerializeValue(ref AssignedWord);
        }

        public bool Equals(AudioWordConsensusPlayerWordAssignmentNetworkState other)
        {
            return ClientId == other.ClientId &&
                   DisplayOrder == other.DisplayOrder &&
                   AssignedWord.Equals(other.AssignedWord);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioWordConsensusPlayerWordAssignmentNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, DisplayOrder, AssignedWord.GetHashCode());
        }
    }
}
