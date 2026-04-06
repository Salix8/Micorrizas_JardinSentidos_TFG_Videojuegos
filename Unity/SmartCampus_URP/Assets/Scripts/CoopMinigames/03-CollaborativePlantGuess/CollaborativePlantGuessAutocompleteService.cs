using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public static class CollaborativePlantGuessAutocompleteService
    {
        public static IReadOnlyList<CollaborativePlantGuessPlantDefinition> BuildSuggestions(
            IReadOnlyList<CollaborativePlantGuessPlantDefinition> plantDefinitions,
            string rawInput,
            int maxSuggestions)
        {
            if (plantDefinitions == null || plantDefinitions.Count == 0 || string.IsNullOrWhiteSpace(rawInput))
            {
                return Array.Empty<CollaborativePlantGuessPlantDefinition>();
            }

            var normalizedInput = Normalize(rawInput);
            var prefixMatches = new List<CollaborativePlantGuessPlantDefinition>();

            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                var plant = plantDefinitions[index];
                if (!MatchesPrefixPlantName(plant, normalizedInput))
                {
                    continue;
                }

                prefixMatches.Add(plant);
            }

            prefixMatches.Sort(SortByDisplayName);

            return prefixMatches
                .Take(Math.Max(1, maxSuggestions))
                .ToArray();
        }

        public static bool TryResolvePlant(
            IReadOnlyList<CollaborativePlantGuessPlantDefinition> plantDefinitions,
            string rawInput,
            out CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            plantDefinition = null;
            if (plantDefinitions == null || plantDefinitions.Count == 0 || string.IsNullOrWhiteSpace(rawInput))
            {
                return false;
            }

            var normalizedInput = Normalize(rawInput);
            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                var plant = plantDefinitions[index];
                if (MatchesExactPlantName(plant, normalizedInput))
                {
                    plantDefinition = plant;
                    return true;
                }
            }

            CollaborativePlantGuessPlantDefinition prefixMatch = null;
            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                var plant = plantDefinitions[index];
                if (!MatchesPrefixPlantName(plant, normalizedInput))
                {
                    continue;
                }

                if (prefixMatch != null)
                {
                    plantDefinition = null;
                    return false;
                }

                prefixMatch = plant;
            }

            if (prefixMatch != null)
            {
                plantDefinition = prefixMatch;
                return true;
            }

            return false;
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

        private static bool MatchesExactPlantName(CollaborativePlantGuessPlantDefinition plantDefinition, string normalizedInput)
        {
            if (Normalize(plantDefinition.CommonName) == normalizedInput ||
                Normalize(plantDefinition.ScientificName) == normalizedInput ||
                Normalize(plantDefinition.FullDisplayName) == normalizedInput)
            {
                return true;
            }

            for (var synonymIndex = 0; synonymIndex < plantDefinition.Synonyms.Count; synonymIndex++)
            {
                if (Normalize(plantDefinition.Synonyms[synonymIndex]) == normalizedInput)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPrefixPlantName(CollaborativePlantGuessPlantDefinition plantDefinition, string normalizedInput)
        {
            if (MatchesPrefix(plantDefinition.CommonName, normalizedInput) ||
                MatchesPrefix(plantDefinition.ScientificName, normalizedInput) ||
                MatchesPrefix(plantDefinition.FullDisplayName, normalizedInput))
            {
                return true;
            }

            for (var synonymIndex = 0; synonymIndex < plantDefinition.Synonyms.Count; synonymIndex++)
            {
                if (MatchesPrefix(plantDefinition.Synonyms[synonymIndex], normalizedInput))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPrefix(string value, string normalizedInput)
        {
            return Normalize(value).StartsWith(normalizedInput, StringComparison.Ordinal);
        }

        private static int SortByDisplayName(CollaborativePlantGuessPlantDefinition left, CollaborativePlantGuessPlantDefinition right)
        {
            return string.Compare(left.CommonName, right.CommonName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
