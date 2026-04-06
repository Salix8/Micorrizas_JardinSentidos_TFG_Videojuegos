using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public struct CollaborativePlantGuessHistoryEntryNetworkState : INetworkSerializable, IEquatable<CollaborativePlantGuessHistoryEntryNetworkState>
    {
        public int AttemptIndex;
        public ulong GuessingClientId;
        public FixedString128Bytes PlantId;
        public CollaborativePlantGuessComparisonOutcome PlantTypeOutcome;
        public CollaborativePlantGuessComparisonOutcome SurfaceRoughnessOutcome;
        public CollaborativePlantGuessComparisonOutcome LeafTypeOutcome;
        public CollaborativePlantGuessComparisonOutcome FruitOutcome;
        public bool IsExactPlantMatch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AttemptIndex);
            serializer.SerializeValue(ref GuessingClientId);
            serializer.SerializeValue(ref PlantId);

            var plantType = (int)PlantTypeOutcome;
            var surfaceRoughness = (int)SurfaceRoughnessOutcome;
            var leafType = (int)LeafTypeOutcome;
            var fruit = (int)FruitOutcome;

            serializer.SerializeValue(ref plantType);
            serializer.SerializeValue(ref surfaceRoughness);
            serializer.SerializeValue(ref leafType);
            serializer.SerializeValue(ref fruit);
            serializer.SerializeValue(ref IsExactPlantMatch);

            PlantTypeOutcome = (CollaborativePlantGuessComparisonOutcome)plantType;
            SurfaceRoughnessOutcome = (CollaborativePlantGuessComparisonOutcome)surfaceRoughness;
            LeafTypeOutcome = (CollaborativePlantGuessComparisonOutcome)leafType;
            FruitOutcome = (CollaborativePlantGuessComparisonOutcome)fruit;
        }

        public bool Equals(CollaborativePlantGuessHistoryEntryNetworkState other)
        {
            return AttemptIndex == other.AttemptIndex &&
                   GuessingClientId == other.GuessingClientId &&
                   PlantId.Equals(other.PlantId) &&
                   PlantTypeOutcome == other.PlantTypeOutcome &&
                   SurfaceRoughnessOutcome == other.SurfaceRoughnessOutcome &&
                   LeafTypeOutcome == other.LeafTypeOutcome &&
                   FruitOutcome == other.FruitOutcome &&
                   IsExactPlantMatch == other.IsExactPlantMatch;
        }

        public override bool Equals(object obj)
        {
            return obj is CollaborativePlantGuessHistoryEntryNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AttemptIndex;
                hashCode = (hashCode * 397) ^ GuessingClientId.GetHashCode();
                hashCode = (hashCode * 397) ^ PlantId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)PlantTypeOutcome;
                hashCode = (hashCode * 397) ^ (int)SurfaceRoughnessOutcome;
                hashCode = (hashCode * 397) ^ (int)LeafTypeOutcome;
                hashCode = (hashCode * 397) ^ (int)FruitOutcome;
                hashCode = (hashCode * 397) ^ IsExactPlantMatch.GetHashCode();
                return hashCode;
            }
        }
    }
}
