using System;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    public readonly struct AudioWordConsensusPlannedRound
    {
        public AudioWordConsensusPlannedRound(int roundDefinitionIndex, ulong emitterClientId)
        {
            RoundDefinitionIndex = roundDefinitionIndex;
            EmitterClientId = emitterClientId;
        }

        public int RoundDefinitionIndex { get; }
        public ulong EmitterClientId { get; }
    }

    public static class AudioWordConsensusRoundPlanService
    {
        public static bool TryBuildRoundPlan(
            IReadOnlyList<ulong> participantIds,
            IReadOnlyList<AudioWordConsensusRoundDefinition> roundDefinitions,
            int maxRoundCount,
            int randomSeed,
            out List<AudioWordConsensusPlannedRound> plannedRounds,
            out string errorMessage)
        {
            plannedRounds = new List<AudioWordConsensusPlannedRound>();
            errorMessage = string.Empty;

            if (participantIds == null || participantIds.Count < 2)
            {
                errorMessage = "Se necesitan al menos dos participantes para iniciar el minijuego.";
                return false;
            }

            if (roundDefinitions == null || roundDefinitions.Count == 0)
            {
                errorMessage = "No hay rondas de audio configuradas.";
                return false;
            }

            var receiverCount = participantIds.Count - 1;
            var usableRoundDefinitionIndices = new List<int>();
            for (var index = 0; index < roundDefinitions.Count; index++)
            {
                if (AudioWordConsensusRoundDefinitionValidator.IsUsable(roundDefinitions[index], receiverCount))
                {
                    usableRoundDefinitionIndices.Add(index);
                }
            }

            if (usableRoundDefinitionIndices.Count == 0)
            {
                errorMessage = "No hay sonidos configurados con palabras validas para esta partida.";
                return false;
            }

            var effectiveMaxRoundCount = Math.Max(1, maxRoundCount);
            var plannedRoundCount = Math.Min(usableRoundDefinitionIndices.Count, Math.Min(participantIds.Count, effectiveMaxRoundCount));
            if (plannedRoundCount <= 0)
            {
                errorMessage = "No se ha podido planificar ninguna ronda.";
                return false;
            }

            var random = new Random(randomSeed);
            ShuffleInPlace(usableRoundDefinitionIndices, random);

            for (var roundIndex = 0; roundIndex < plannedRoundCount; roundIndex++)
            {
                plannedRounds.Add(new AudioWordConsensusPlannedRound(
                    usableRoundDefinitionIndices[roundIndex],
                    participantIds[roundIndex % participantIds.Count]));
            }

            return true;
        }

        private static void ShuffleInPlace<T>(IList<T> values, Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }
    }
}
