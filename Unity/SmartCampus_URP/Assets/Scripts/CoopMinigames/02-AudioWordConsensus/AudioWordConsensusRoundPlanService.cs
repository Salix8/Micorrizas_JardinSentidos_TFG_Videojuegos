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

            var usableRoundDefinitionIndices = new List<int>();
            for (var index = 0; index < roundDefinitions.Count; index++)
            {
                if (AudioWordConsensusRoundDefinitionValidator.IsUsable(roundDefinitions[index]))
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
            var plannedRoundCount = Math.Min(usableRoundDefinitionIndices.Count, effectiveMaxRoundCount);
            if (plannedRoundCount <= 0)
            {
                errorMessage = "No se ha podido planificar ninguna ronda.";
                return false;
            }

            var random = new Random(randomSeed);
            ShuffleInPlace(usableRoundDefinitionIndices, random);
            var lastEmitterClientId = ulong.MaxValue;

            for (var roundIndex = 0; roundIndex < plannedRoundCount; roundIndex++)
            {
                var emitterClientId = SelectEmitterClientId(participantIds, lastEmitterClientId, random);
                plannedRounds.Add(new AudioWordConsensusPlannedRound(
                    usableRoundDefinitionIndices[roundIndex],
                    emitterClientId));
                lastEmitterClientId = emitterClientId;
            }

            return true;
        }

        private static ulong SelectEmitterClientId(IReadOnlyList<ulong> participantIds, ulong previousEmitterClientId, Random random)
        {
            if (participantIds.Count == 1)
            {
                return participantIds[0];
            }

            var candidateIndices = new List<int>(participantIds.Count);
            for (var index = 0; index < participantIds.Count; index++)
            {
                if (participantIds[index] != previousEmitterClientId)
                {
                    candidateIndices.Add(index);
                }
            }

            var selectedIndex = candidateIndices[random.Next(candidateIndices.Count)];
            return participantIds[selectedIndex];
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
