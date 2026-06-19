using System.Collections.Generic;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    public static class DialogueGardenPresenceService
    {
        public static bool TryAreAllPlayersInside(
            IReadOnlyList<ulong> playerIds,
            CoopGpsStateSync gpsStateSync,
            CoopGpsMarkerController gpsMarkerController,
            DialogueGardenBoundary boundary,
            out bool areAllInside)
        {
            areAllInside = false;
            if (playerIds == null || playerIds.Count == 0 ||
                gpsStateSync == null || gpsMarkerController == null || boundary == null)
            {
                return false;
            }

            for (var index = 0; index < playerIds.Count; index++)
            {
                var clientId = playerIds[index];
                if (!gpsStateSync.TryGetState(clientId, out var state) ||
                    !state.HasFix ||
                    !gpsMarkerController.TryGetMarkerWorldPosition(clientId, out var worldPosition))
                {
                    return false;
                }

                if (!boundary.Contains(worldPosition))
                {
                    areAllInside = false;
                    return true;
                }
            }

            areAllInside = true;
            return true;
        }
    }
}
