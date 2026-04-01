using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    public sealed class CollaborativePlantGuessPlantDefinition
    {
        public CollaborativePlantGuessPlantDefinition(
            string plantId,
            string displayName,
            string[] aliases,
            string imagePath,
            string leafPersistence,
            string leafSize,
            int leafSizeOrder,
            string leafTexture,
            int leafTextureOrder,
            string fruitType,
            string fruitCategory)
        {
            PlantId = plantId;
            DisplayName = displayName;
            Aliases = aliases ?? Array.Empty<string>();
            ImagePath = imagePath ?? string.Empty;
            LeafPersistence = leafPersistence;
            LeafSize = leafSize;
            LeafSizeOrder = leafSizeOrder;
            LeafTexture = leafTexture;
            LeafTextureOrder = leafTextureOrder;
            FruitType = fruitType;
            FruitCategory = fruitCategory;
        }

        public string PlantId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> Aliases { get; }
        public string ImagePath { get; }
        public string LeafPersistence { get; }
        public string LeafSize { get; }
        public int LeafSizeOrder { get; }
        public string LeafTexture { get; }
        public int LeafTextureOrder { get; }
        public string FruitType { get; }
        public string FruitCategory { get; }
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
                var displayName = GetValue(row, headerMap, "displayName").Trim();
                var imagePath = GetValue(row, headerMap, "imagePath").Trim();
                var leafPersistence = GetValue(row, headerMap, "leafPersistence").Trim();
                var leafSize = GetValue(row, headerMap, "leafSize").Trim();
                var leafTexture = GetValue(row, headerMap, "leafTexture").Trim();
                var fruitType = GetValue(row, headerMap, "fruitType").Trim();
                var fruitCategory = GetValue(row, headerMap, "fruitCategory").Trim();
                var aliases = SplitAliases(GetValue(row, headerMap, "aliases"));

                if (!TryGetInt(row, headerMap, "leafSizeOrder", out var leafSizeOrder) ||
                    !TryGetInt(row, headerMap, "leafTextureOrder", out var leafTextureOrder))
                {
                    errorMessage = $"Fila {rowIndex + 1}: leafSizeOrder y leafTextureOrder deben ser enteros.";
                    plantDefinitions.Clear();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(plantId) ||
                    string.IsNullOrWhiteSpace(displayName) ||
                    string.IsNullOrWhiteSpace(leafPersistence) ||
                    string.IsNullOrWhiteSpace(leafSize) ||
                    string.IsNullOrWhiteSpace(leafTexture) ||
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

                if (!seenDisplayNames.Add(displayName))
                {
                    errorMessage = $"Fila {rowIndex + 1}: displayName repetido '{displayName}'.";
                    plantDefinitions.Clear();
                    return false;
                }

                plantDefinitions.Add(new CollaborativePlantGuessPlantDefinition(
                    plantId,
                    displayName,
                    aliases,
                    imagePath,
                    leafPersistence,
                    leafSize,
                    leafSizeOrder,
                    leafTexture,
                    leafTextureOrder,
                    fruitType,
                    string.IsNullOrWhiteSpace(fruitCategory) ? fruitType : fruitCategory));
            }

            plantDefinitions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));

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
                "plantId", "displayName", "aliases", "imagePath", "leafPersistence",
                "leafSize", "leafSizeOrder", "leafTexture", "leafTextureOrder", "fruitType", "fruitCategory"
            };

            foreach (var requiredColumn in requiredColumns)
            {
                if (!headerMap.ContainsKey(requiredColumn))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumn}'.";
                    return false;
                }
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

        private static string[] SplitAliases(string rawAliases)
        {
            return string.IsNullOrWhiteSpace(rawAliases)
                ? Array.Empty<string>()
                : rawAliases
                    .Split('|')
                    .Select(alias => alias.Trim())
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }

        private static bool TryGetInt(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key, out int value)
        {
            value = 0;
            return int.TryParse(GetValue(row, headerMap, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string GetValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key)
        {
            if (!headerMap.TryGetValue(key, out var index))
            {
                return string.Empty;
            }

            return index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
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
