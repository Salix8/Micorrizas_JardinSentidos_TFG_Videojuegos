using System;
using System.Collections.Generic;
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

    public readonly struct DistributedPairsHandSlotModel
    {
        public DistributedPairsHandSlotModel(int slotIndex, bool hasCard, DistributedPairsCardNetworkState cardState = default)
        {
            SlotIndex = slotIndex;
            HasCard = hasCard;
            CardState = cardState;
        }

        public int SlotIndex { get; }
        public bool HasCard { get; }
        public DistributedPairsCardNetworkState CardState { get; }
    }

    public static class DistributedPairsHandSlotService
    {
        public static IReadOnlyList<DistributedPairsHandSlotModel> BuildSlots(IReadOnlyList<DistributedPairsCardNetworkState> handStates, int slotCount)
        {
            var orderedSlots = new DistributedPairsHandSlotModel[slotCount];
            var nextFallbackSlot = 0;

            for (var index = 0; index < handStates.Count; index++)
            {
                var state = handStates[index];
                var targetSlot = state.HandOrder;
                if (targetSlot < 0 || targetSlot >= slotCount || orderedSlots[targetSlot].HasCard)
                {
                    while (nextFallbackSlot < slotCount && orderedSlots[nextFallbackSlot].HasCard)
                    {
                        nextFallbackSlot++;
                    }

                    targetSlot = nextFallbackSlot;
                }

                if (targetSlot < 0 || targetSlot >= slotCount)
                {
                    continue;
                }

                orderedSlots[targetSlot] = new DistributedPairsHandSlotModel(targetSlot, hasCard: true, state);
            }

            for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (!orderedSlots[slotIndex].HasCard)
                {
                    orderedSlots[slotIndex] = new DistributedPairsHandSlotModel(slotIndex, hasCard: false);
                }
            }

            return orderedSlots;
        }
    }
}
