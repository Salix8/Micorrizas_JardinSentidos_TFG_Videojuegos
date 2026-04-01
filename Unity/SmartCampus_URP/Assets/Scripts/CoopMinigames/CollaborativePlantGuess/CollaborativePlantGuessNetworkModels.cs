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
        public CollaborativePlantGuessComparisonOutcome LeafPersistenceOutcome;
        public CollaborativePlantGuessComparisonOutcome LeafSizeOutcome;
        public CollaborativePlantGuessComparisonOutcome LeafTextureOutcome;
        public CollaborativePlantGuessComparisonOutcome FruitTypeOutcome;
        public bool IsExactPlantMatch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AttemptIndex);
            serializer.SerializeValue(ref GuessingClientId);
            serializer.SerializeValue(ref PlantId);

            var leafPersistence = (int)LeafPersistenceOutcome;
            var leafSize = (int)LeafSizeOutcome;
            var leafTexture = (int)LeafTextureOutcome;
            var fruitType = (int)FruitTypeOutcome;

            serializer.SerializeValue(ref leafPersistence);
            serializer.SerializeValue(ref leafSize);
            serializer.SerializeValue(ref leafTexture);
            serializer.SerializeValue(ref fruitType);
            serializer.SerializeValue(ref IsExactPlantMatch);

            LeafPersistenceOutcome = (CollaborativePlantGuessComparisonOutcome)leafPersistence;
            LeafSizeOutcome = (CollaborativePlantGuessComparisonOutcome)leafSize;
            LeafTextureOutcome = (CollaborativePlantGuessComparisonOutcome)leafTexture;
            FruitTypeOutcome = (CollaborativePlantGuessComparisonOutcome)fruitType;
        }

        public bool Equals(CollaborativePlantGuessHistoryEntryNetworkState other)
        {
            return AttemptIndex == other.AttemptIndex &&
                   GuessingClientId == other.GuessingClientId &&
                   PlantId.Equals(other.PlantId) &&
                   LeafPersistenceOutcome == other.LeafPersistenceOutcome &&
                   LeafSizeOutcome == other.LeafSizeOutcome &&
                   LeafTextureOutcome == other.LeafTextureOutcome &&
                   FruitTypeOutcome == other.FruitTypeOutcome &&
                   IsExactPlantMatch == other.IsExactPlantMatch;
        }

        public override bool Equals(object obj)
        {
            return obj is CollaborativePlantGuessHistoryEntryNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                AttemptIndex,
                GuessingClientId,
                PlantId.GetHashCode(),
                (int)LeafPersistenceOutcome,
                (int)LeafSizeOutcome,
                (int)LeafTextureOutcome,
                (int)FruitTypeOutcome,
                IsExactPlantMatch);
        }
    }
}
