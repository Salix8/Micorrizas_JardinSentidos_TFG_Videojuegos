using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    public struct GardenSmellTaxonomyClassificationEntryNetworkState : INetworkSerializable, IEquatable<GardenSmellTaxonomyClassificationEntryNetworkState>
    {
        public FixedString64Bytes PlantId;
        public FixedString128Bytes ScientificName;
        public GardenSmellTaxonomyCategory ChosenCategory;
        public GardenSmellTaxonomyCategory CorrectCategory;
        public bool IsCorrect;
        public ulong SubmittedByClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var chosenCategoryValue = (byte)ChosenCategory;
            var correctCategoryValue = (byte)CorrectCategory;

            serializer.SerializeValue(ref PlantId);
            serializer.SerializeValue(ref ScientificName);
            serializer.SerializeValue(ref chosenCategoryValue);
            serializer.SerializeValue(ref correctCategoryValue);
            serializer.SerializeValue(ref IsCorrect);
            serializer.SerializeValue(ref SubmittedByClientId);

            ChosenCategory = (GardenSmellTaxonomyCategory)chosenCategoryValue;
            CorrectCategory = (GardenSmellTaxonomyCategory)correctCategoryValue;
        }

        public bool Equals(GardenSmellTaxonomyClassificationEntryNetworkState other)
        {
            return PlantId.Equals(other.PlantId) &&
                   ScientificName.Equals(other.ScientificName) &&
                   ChosenCategory == other.ChosenCategory &&
                   CorrectCategory == other.CorrectCategory &&
                   IsCorrect == other.IsCorrect &&
                   SubmittedByClientId == other.SubmittedByClientId;
        }

        public override bool Equals(object obj)
        {
            return obj is GardenSmellTaxonomyClassificationEntryNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlantId.GetHashCode(), ScientificName.GetHashCode(), (int)ChosenCategory, (int)CorrectCategory, IsCorrect, SubmittedByClientId);
        }
    }
}
