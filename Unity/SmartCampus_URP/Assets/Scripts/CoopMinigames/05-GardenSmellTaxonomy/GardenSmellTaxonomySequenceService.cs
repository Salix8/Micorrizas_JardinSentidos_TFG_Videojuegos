using System;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    public static class GardenSmellTaxonomySequenceService
    {
        public static List<GardenSmellTaxonomyPlantDefinition> BuildSchedule(
            IReadOnlyList<GardenSmellTaxonomyPlantDefinition> loadedDefinitions,
            int maxPlantsPerMatch,
            bool shufflePlants,
            int seed)
        {
            var schedule = new List<GardenSmellTaxonomyPlantDefinition>();
            if (loadedDefinitions == null || loadedDefinitions.Count == 0 || maxPlantsPerMatch <= 0)
            {
                return schedule;
            }

            schedule.AddRange(loadedDefinitions);

            if (shufflePlants)
            {
                var random = new Random(seed);
                for (var index = schedule.Count - 1; index > 0; index--)
                {
                    var swapIndex = random.Next(index + 1);
                    var buffer = schedule[index];
                    schedule[index] = schedule[swapIndex];
                    schedule[swapIndex] = buffer;
                }
            }

            if (schedule.Count > maxPlantsPerMatch)
            {
                schedule.RemoveRange(maxPlantsPerMatch, schedule.Count - maxPlantsPerMatch);
            }

            return schedule;
        }
    }
}
