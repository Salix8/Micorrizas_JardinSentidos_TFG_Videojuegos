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
        public CollaborativePlantGuessComparisonOutcome LeafPersistenceOutcome;
        public CollaborativePlantGuessComparisonOutcome LeafTypeOutcome;
        public CollaborativePlantGuessComparisonOutcome FruitCategoryOutcome;
        public CollaborativePlantGuessComparisonOutcome FruitTypeOutcome;
        public bool IsExactPlantMatch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AttemptIndex);
            serializer.SerializeValue(ref GuessingClientId);
            serializer.SerializeValue(ref PlantId);

            var plantType = (int)PlantTypeOutcome;
            var surfaceRoughness = (int)SurfaceRoughnessOutcome;
            var leafPersistence = (int)LeafPersistenceOutcome;
            var leafType = (int)LeafTypeOutcome;
            var fruitCategory = (int)FruitCategoryOutcome;
            var fruitType = (int)FruitTypeOutcome;

            serializer.SerializeValue(ref plantType);
            serializer.SerializeValue(ref surfaceRoughness);
            serializer.SerializeValue(ref leafPersistence);
            serializer.SerializeValue(ref leafType);
            serializer.SerializeValue(ref fruitCategory);
            serializer.SerializeValue(ref fruitType);
            serializer.SerializeValue(ref IsExactPlantMatch);

            PlantTypeOutcome = (CollaborativePlantGuessComparisonOutcome)plantType;
            SurfaceRoughnessOutcome = (CollaborativePlantGuessComparisonOutcome)surfaceRoughness;
            LeafPersistenceOutcome = (CollaborativePlantGuessComparisonOutcome)leafPersistence;
            LeafTypeOutcome = (CollaborativePlantGuessComparisonOutcome)leafType;
            FruitCategoryOutcome = (CollaborativePlantGuessComparisonOutcome)fruitCategory;
            FruitTypeOutcome = (CollaborativePlantGuessComparisonOutcome)fruitType;
        }

        public bool Equals(CollaborativePlantGuessHistoryEntryNetworkState other)
        {
            return AttemptIndex == other.AttemptIndex &&
                   GuessingClientId == other.GuessingClientId &&
                   PlantId.Equals(other.PlantId) &&
                   PlantTypeOutcome == other.PlantTypeOutcome &&
                   SurfaceRoughnessOutcome == other.SurfaceRoughnessOutcome &&
                   LeafPersistenceOutcome == other.LeafPersistenceOutcome &&
                   LeafTypeOutcome == other.LeafTypeOutcome &&
                   FruitCategoryOutcome == other.FruitCategoryOutcome &&
                   FruitTypeOutcome == other.FruitTypeOutcome &&
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
                hashCode = (hashCode * 397) ^ (int)LeafPersistenceOutcome;
                hashCode = (hashCode * 397) ^ (int)LeafTypeOutcome;
                hashCode = (hashCode * 397) ^ (int)FruitCategoryOutcome;
                hashCode = (hashCode * 397) ^ (int)FruitTypeOutcome;
                hashCode = (hashCode * 397) ^ IsExactPlantMatch.GetHashCode();
                return hashCode;
            }
        }
    }
}
