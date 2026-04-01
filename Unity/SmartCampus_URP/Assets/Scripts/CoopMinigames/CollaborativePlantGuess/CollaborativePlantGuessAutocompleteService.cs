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
            var containsMatches = new List<CollaborativePlantGuessPlantDefinition>();

            for (var index = 0; index < plantDefinitions.Count; index++)
            {
                var plant = plantDefinitions[index];
                var bestMatch = GetBestMatchScore(plant, normalizedInput);
                if (bestMatch == MatchScore.None)
                {
                    continue;
                }

                if (bestMatch == MatchScore.Prefix)
                {
                    prefixMatches.Add(plant);
                }
                else
                {
                    containsMatches.Add(plant);
                }
            }

            prefixMatches.Sort(SortByDisplayName);
            containsMatches.Sort(SortByDisplayName);

            return prefixMatches
                .Concat(containsMatches)
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
            if (Normalize(plantDefinition.DisplayName) == normalizedInput)
            {
                return true;
            }

            for (var aliasIndex = 0; aliasIndex < plantDefinition.Aliases.Count; aliasIndex++)
            {
                if (Normalize(plantDefinition.Aliases[aliasIndex]) == normalizedInput)
                {
                    return true;
                }
            }

            return false;
        }

        private static MatchScore GetBestMatchScore(CollaborativePlantGuessPlantDefinition plantDefinition, string normalizedInput)
        {
            if (MatchesPrefix(plantDefinition.DisplayName, normalizedInput))
            {
                return MatchScore.Prefix;
            }

            if (ContainsValue(plantDefinition.DisplayName, normalizedInput))
            {
                return MatchScore.Contains;
            }

            for (var aliasIndex = 0; aliasIndex < plantDefinition.Aliases.Count; aliasIndex++)
            {
                if (MatchesPrefix(plantDefinition.Aliases[aliasIndex], normalizedInput))
                {
                    return MatchScore.Prefix;
                }

                if (ContainsValue(plantDefinition.Aliases[aliasIndex], normalizedInput))
                {
                    return MatchScore.Contains;
                }
            }

            return MatchScore.None;
        }

        private static bool MatchesPrefix(string value, string normalizedInput)
        {
            return Normalize(value).StartsWith(normalizedInput, StringComparison.Ordinal);
        }

        private static bool ContainsValue(string value, string normalizedInput)
        {
            return Normalize(value).IndexOf(normalizedInput, StringComparison.Ordinal) >= 0;
        }

        private static int SortByDisplayName(CollaborativePlantGuessPlantDefinition left, CollaborativePlantGuessPlantDefinition right)
        {
            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private enum MatchScore
        {
            None = 0,
            Contains = 1,
            Prefix = 2
        }
    }
}
