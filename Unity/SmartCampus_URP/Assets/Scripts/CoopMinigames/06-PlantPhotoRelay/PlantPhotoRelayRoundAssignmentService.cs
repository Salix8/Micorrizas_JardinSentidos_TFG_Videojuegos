using System;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public readonly struct PlantPhotoRelayRoleAssignment
    {
        public PlantPhotoRelayRoleAssignment(ulong photographerId, ulong guesserId)
        {
            PhotographerId = photographerId;
            GuesserId = guesserId;
        }

        public ulong PhotographerId { get; }
        public ulong GuesserId { get; }
    }

    public static class PlantPhotoRelayRoundAssignmentService
    {
        public static PlantPhotoRelayRoleAssignment CreateAssignment(IReadOnlyList<ulong> participantIds, int roundIndex)
        {
            if (participantIds == null || participantIds.Count < 2)
            {
                throw new ArgumentException("Se necesitan al menos dos participantes.");
            }

            var safeRoundIndex = Math.Max(0, roundIndex);
            var photographerIndex = safeRoundIndex % participantIds.Count;
            var guesserIndex = (photographerIndex + 1) % participantIds.Count;
            return new PlantPhotoRelayRoleAssignment(participantIds[photographerIndex], participantIds[guesserIndex]);
        }
    }
}
