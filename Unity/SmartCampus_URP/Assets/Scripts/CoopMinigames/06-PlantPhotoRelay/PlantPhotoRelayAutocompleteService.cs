using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public static class PlantPhotoRelayAutocompleteService
    {
        public static IReadOnlyList<PlantPhotoRelayPlantDefinition> BuildSuggestions(
            IReadOnlyList<PlantPhotoRelayPlantDefinition> plantDefinitions,
            string rawInput,
            int maxSuggestions)
        {
            if (plantDefinitions == null || plantDefinitions.Count == 0 || string.IsNullOrWhiteSpace(rawInput))
            {
                return Array.Empty<PlantPhotoRelayPlantDefinition>();
            }

            var normalizedInput = Normalize(rawInput);
            return plantDefinitions
                .Where(plant => MatchesPrefix(plant, normalizedInput))
                .OrderBy(plant => plant.DisplayCommonName, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, maxSuggestions))
                .ToArray();
        }

        public static bool TryResolvePlant(
            IReadOnlyList<PlantPhotoRelayPlantDefinition> plantDefinitions,
            string rawInput,
            out PlantPhotoRelayPlantDefinition plantDefinition)
        {
            plantDefinition = null;
            if (plantDefinitions == null || plantDefinitions.Count == 0 || string.IsNullOrWhiteSpace(rawInput))
            {
                return false;
            }

            var normalizedInput = Normalize(rawInput);
            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                if (MatchesExact(plantDefinitions[index], normalizedInput))
                {
                    plantDefinition = plantDefinitions[index];
                    return true;
                }
            }

            PlantPhotoRelayPlantDefinition uniquePrefixMatch = null;
            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                var candidate = plantDefinitions[index];
                if (!MatchesPrefix(candidate, normalizedInput))
                {
                    continue;
                }

                if (uniquePrefixMatch != null)
                {
                    plantDefinition = null;
                    return false;
                }

                uniquePrefixMatch = candidate;
            }

            plantDefinition = uniquePrefixMatch;
            return uniquePrefixMatch != null;
        }

        public static string Normalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            var normalized = rawValue.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            for (var index = 0; index < normalized.Length; index++)
            {
                var character = normalized[index];
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool MatchesExact(PlantPhotoRelayPlantDefinition plantDefinition, string normalizedInput)
        {
            if (Normalize(plantDefinition.CommonNameCanonical) == normalizedInput ||
                Normalize(plantDefinition.DisplayCommonName) == normalizedInput)
            {
                return true;
            }

            for (var index = 0; index < plantDefinition.AcceptedCommonNameVariants.Count; index++)
            {
                if (Normalize(plantDefinition.AcceptedCommonNameVariants[index]) == normalizedInput)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPrefix(PlantPhotoRelayPlantDefinition plantDefinition, string normalizedInput)
        {
            if (Normalize(plantDefinition.CommonNameCanonical).StartsWith(normalizedInput, StringComparison.Ordinal) ||
                Normalize(plantDefinition.DisplayCommonName).StartsWith(normalizedInput, StringComparison.Ordinal))
            {
                return true;
            }

            for (var index = 0; index < plantDefinition.AcceptedCommonNameVariants.Count; index++)
            {
                if (Normalize(plantDefinition.AcceptedCommonNameVariants[index]).StartsWith(normalizedInput, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
