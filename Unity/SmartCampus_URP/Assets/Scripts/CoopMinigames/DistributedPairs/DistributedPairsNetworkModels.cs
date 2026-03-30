using System;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    public enum DistributedPairsCardZone : byte
    {
        DrawPile = 0,
        Hand = 1,
        Discarded = 2
    }

    public struct DistributedPairsCardNetworkState : INetworkSerializable, IEquatable<DistributedPairsCardNetworkState>
    {
        public int CardInstanceId;
        public int PairId;
        public ulong OwnerClientId;
        public bool IsSelected;
        public int HandOrder;
        public DistributedPairsCardZone Zone;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var zoneValue = (byte)Zone;
            serializer.SerializeValue(ref CardInstanceId);
            serializer.SerializeValue(ref PairId);
            serializer.SerializeValue(ref OwnerClientId);
            serializer.SerializeValue(ref IsSelected);
            serializer.SerializeValue(ref HandOrder);
            serializer.SerializeValue(ref zoneValue);
            Zone = (DistributedPairsCardZone)zoneValue;
        }

        public bool Equals(DistributedPairsCardNetworkState other)
        {
            return CardInstanceId == other.CardInstanceId &&
                   PairId == other.PairId &&
                   OwnerClientId == other.OwnerClientId &&
                   IsSelected == other.IsSelected &&
                   HandOrder == other.HandOrder &&
                   Zone == other.Zone;
        }

        public override bool Equals(object obj)
        {
            return obj is DistributedPairsCardNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CardInstanceId, PairId, OwnerClientId, IsSelected, HandOrder, (int)Zone);
        }
    }
}
