using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public sealed class CollaborativePlantGuessPlantDefinition
    {
        public CollaborativePlantGuessPlantDefinition(
            string plantId,
            string commonName,
            string scientificName,
            string[] synonyms,
            string imagePath,
            string plantType,
            string surfaceRoughness,
            string leafPersistence,
            string leafType,
            string fruitCategory,
            string fruitType)
        {
            PlantId = plantId;
            CommonName = commonName;
            ScientificName = scientificName;
            Synonyms = synonyms ?? Array.Empty<string>();
            ImagePath = imagePath ?? string.Empty;
            PlantType = plantType;
            SurfaceRoughness = surfaceRoughness;
            LeafPersistence = leafPersistence;
            LeafType = leafType;
            FruitCategory = fruitCategory;
            FruitType = fruitType;
        }

        public string PlantId { get; }
        public string CommonName { get; }
        public string ScientificName { get; }
        public IReadOnlyList<string> Synonyms { get; }
        public string ImagePath { get; }
        public string PlantType { get; }
        public string SurfaceRoughness { get; }
        public string LeafPersistence { get; }
        public string LeafType { get; }
        public string FruitCategory { get; }
        public string FruitType { get; }

        public string DisplayName => CommonName;

        public string FullDisplayName => string.IsNullOrWhiteSpace(ScientificName)
            ? CommonName
            : $"{CommonName} ({ScientificName})";
    }

    public static class CollaborativePlantGuessCsvService
    {
        public static bool TryParse(
            string csvContent,
            out List<CollaborativePlantGuessPlantDefinition> plantDefinitions,
            out string errorMessage)
        {
            plantDefinitions = new List<CollaborativePlantGuessPlantDefinition>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV de plantas esta vacio.";
                return false;
            }

            var rows = ParseRows(csvContent);
            if (rows.Count <= 1)
            {
                errorMessage = "El CSV necesita cabecera y al menos una fila.";
                return false;
            }

            var headerMap = BuildHeaderMap(rows[0]);
            if (!ValidateRequiredColumns(headerMap, out errorMessage))
            {
                return false;
            }

            var seenPlantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenDisplayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                var plantId = GetValue(row, headerMap, "plantId").Trim();
                var commonName = GetValue(row, headerMap, "commonName").Trim();
                var scientificName = GetValue(row, headerMap, "scientificName").Trim();
                var imagePath = GetValue(row, headerMap, "imagePath").Trim();
                var plantType = GetValue(row, headerMap, "plantType").Trim();
                var surfaceRoughness = GetValue(row, headerMap, "surfaceRoughness").Trim();
                var leafPersistence = GetValue(row, headerMap, "leafPersistence").Trim();
                var leafType = GetValue(row, headerMap, "leafType").Trim();
                var fruitCategory = GetRequiredValue(row, headerMap, "fruitCategory").Trim();
                var fruitType = GetRequiredValue(row, headerMap, "fruitType").Trim();
                var synonyms = SplitSynonyms(GetRequiredValue(row, headerMap, "synonyms", "aliases"));

                if (string.IsNullOrWhiteSpace(plantId) ||
                    string.IsNullOrWhiteSpace(commonName) ||
                    string.IsNullOrWhiteSpace(scientificName) ||
                    string.IsNullOrWhiteSpace(plantType) ||
                    string.IsNullOrWhiteSpace(surfaceRoughness) ||
                    string.IsNullOrWhiteSpace(leafPersistence) ||
                    string.IsNullOrWhiteSpace(leafType) ||
                    string.IsNullOrWhiteSpace(fruitCategory) ||
                    string.IsNullOrWhiteSpace(fruitType))
                {
                    errorMessage = $"Fila {rowIndex + 1}: faltan campos obligatorios.";
                    plantDefinitions.Clear();
                    return false;
                }

                if (!seenPlantIds.Add(plantId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: plantId repetido '{plantId}'.";
                    plantDefinitions.Clear();
                    return false;
                }

                if (!seenDisplayNames.Add(commonName))
                {
                    errorMessage = $"Fila {rowIndex + 1}: commonName repetido '{commonName}'.";
                    plantDefinitions.Clear();
                    return false;
                }

                plantDefinitions.Add(new CollaborativePlantGuessPlantDefinition(
                    plantId,
                    commonName,
                    scientificName,
                    synonyms,
                    imagePath,
                    plantType,
                    surfaceRoughness,
                    leafPersistence,
                    leafType,
                    fruitCategory,
                    fruitType));
            }

            plantDefinitions.Sort((left, right) => string.Compare(left.CommonName, right.CommonName, StringComparison.OrdinalIgnoreCase));

            if (plantDefinitions.Count < 2)
            {
                errorMessage = "El CSV necesita al menos dos plantas validas.";
                return false;
            }

            return true;
        }

        private static bool ValidateRequiredColumns(IReadOnlyDictionary<string, int> headerMap, out string errorMessage)
        {
            var requiredColumns = new[]
            {
                "plantId",
                "commonName",
                "scientificName",
                "plantType",
                "surfaceRoughness",
                "leafPersistence",
                "leafType",
                "fruitCategory",
                "fruitType"
            };

            foreach (var requiredColumn in requiredColumns)
            {
                if (!headerMap.ContainsKey(requiredColumn))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumn}'.";
                    return false;
                }
            }

            if (!headerMap.ContainsKey("synonyms") && !headerMap.ContainsKey("aliases"))
            {
                errorMessage = "Falta la columna requerida 'synonyms' (o su alias legado 'aliases').";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headerRow.Count; index++)
            {
                var key = headerRow[index] == null ? string.Empty : headerRow[index].Trim();
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                {
                    map.Add(key, index);
                }
            }

            return map;
        }

        private static string[] SplitSynonyms(string rawSynonyms)
        {
            return string.IsNullOrWhiteSpace(rawSynonyms)
                ? Array.Empty<string>()
                : rawSynonyms
                    .Split('|')
                    .Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }

        private static string GetValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key)
        {
            if (!headerMap.TryGetValue(key, out var index))
            {
                return string.Empty;
            }

            return index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
        }

        private static string GetRequiredValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, params string[] candidateKeys)
        {
            foreach (var candidateKey in candidateKeys)
            {
                var value = GetValue(row, headerMap, candidateKey);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static bool IsEmptyRow(IReadOnlyList<string> row)
        {
            for (var index = 0; index < row.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(row[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<List<string>> ParseRows(string csvContent)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = string.Empty;
            var insideQuotes = false;

            for (var index = 0; index < csvContent.Length; index++)
            {
                var character = csvContent[index];

                if (character == '"')
                {
                    if (insideQuotes && index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                    {
                        currentCell += '"';
                        index++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }

                    continue;
                }

                if (!insideQuotes && character == ',')
                {
                    currentRow.Add(currentCell);
                    currentCell = string.Empty;
                    continue;
                }

                if (!insideQuotes && (character == '\n' || character == '\r'))
                {
                    if (character == '\r' && index + 1 < csvContent.Length && csvContent[index + 1] == '\n')
                    {
                        index++;
                    }

                    currentRow.Add(currentCell);
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    currentCell = string.Empty;
                    continue;
                }

                currentCell += character;
            }

            currentRow.Add(currentCell);
            rows.Add(currentRow);
            return rows;
        }
    }
}
