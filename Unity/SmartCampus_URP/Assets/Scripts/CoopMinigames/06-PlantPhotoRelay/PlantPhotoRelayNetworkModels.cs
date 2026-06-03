using System;
using Unity.Collections;
using Unity.Netcode;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public enum PlantPhotoRelayPhase
    {
        Clue = 0,
        Capture = 1,
        Guess = 2,
        RoundResults = 3
    }

    public enum PlantPhotoRelayRoundOutcome
    {
        None = 0,
        Success = 1,
        FailedMismatch = 2,
        FailedTimeout = 3,
        FailedCameraUnavailable = 4
    }

    public struct PlantPhotoRelayRoundResultNetworkState : INetworkSerializable, IEquatable<PlantPhotoRelayRoundResultNetworkState>
    {
        public int RoundIndex;
        public PlantPhotoRelayRoundOutcome Outcome;
        public FixedString128Bytes TargetCanonicalCommonName;
        public FixedString128Bytes PhotographerCanonicalCommonName;
        public FixedString128Bytes GuesserCanonicalCommonName;
        public bool PhotographerMatchedPrompt;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoundIndex);
            var outcome = (int)Outcome;
            serializer.SerializeValue(ref outcome);
            serializer.SerializeValue(ref TargetCanonicalCommonName);
            serializer.SerializeValue(ref PhotographerCanonicalCommonName);
            serializer.SerializeValue(ref GuesserCanonicalCommonName);
            serializer.SerializeValue(ref PhotographerMatchedPrompt);
            Outcome = (PlantPhotoRelayRoundOutcome)outcome;
        }

        public bool Equals(PlantPhotoRelayRoundResultNetworkState other)
        {
            return RoundIndex == other.RoundIndex &&
                   Outcome == other.Outcome &&
                   TargetCanonicalCommonName.Equals(other.TargetCanonicalCommonName) &&
                   PhotographerCanonicalCommonName.Equals(other.PhotographerCanonicalCommonName) &&
                   GuesserCanonicalCommonName.Equals(other.GuesserCanonicalCommonName) &&
                   PhotographerMatchedPrompt == other.PhotographerMatchedPrompt;
        }

        public override bool Equals(object obj)
        {
            return obj is PlantPhotoRelayRoundResultNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RoundIndex;
                hashCode = (hashCode * 397) ^ (int)Outcome;
                hashCode = (hashCode * 397) ^ TargetCanonicalCommonName.GetHashCode();
                hashCode = (hashCode * 397) ^ PhotographerCanonicalCommonName.GetHashCode();
                hashCode = (hashCode * 397) ^ GuesserCanonicalCommonName.GetHashCode();
                hashCode = (hashCode * 397) ^ PhotographerMatchedPrompt.GetHashCode();
                return hashCode;
            }
        }
    }
}
