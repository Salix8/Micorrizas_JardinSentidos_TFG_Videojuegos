using System;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames.GardenSmellTaxonomy
{
    public sealed class GardenSmellTaxonomyPlantDefinition
    {
        public GardenSmellTaxonomyPlantDefinition(string plantId, string scientificName, string imagePath, GardenSmellTaxonomyCategory correctCategory)
        {
            PlantId = plantId ?? string.Empty;
            ScientificName = scientificName ?? string.Empty;
            ImagePath = imagePath ?? string.Empty;
            CorrectCategory = correctCategory;
        }

        public string PlantId { get; }
        public string ScientificName { get; }
        public string ImagePath { get; }
        public GardenSmellTaxonomyCategory CorrectCategory { get; }
    }

    public static class GardenSmellTaxonomyCsvService
    {
        public static bool TryParse(string csvContent, out List<GardenSmellTaxonomyPlantDefinition> definitions, out string errorMessage)
        {
            definitions = new List<GardenSmellTaxonomyPlantDefinition>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV de taxonomia esta vacio.";
                return false;
            }

            var rows = ParseRows(csvContent);
            if (rows.Count <= 1)
            {
                errorMessage = "El CSV necesita cabecera y al menos una fila.";
                return false;
            }

            var headerMap = BuildHeaderMap(rows[0]);
            var requiredColumns = new[] { "plantId", "scientificName", "imagePath", "correctCategory" };
            for (var index = 0; index < requiredColumns.Length; index++)
            {
                if (!headerMap.ContainsKey(requiredColumns[index]))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumns[index]}'.";
                    return false;
                }
            }

            var seenPlantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                var plantId = GetValue(row, headerMap, "plantId").Trim();
                var scientificName = GetValue(row, headerMap, "scientificName").Trim();
                var imagePath = GetValue(row, headerMap, "imagePath").Trim();
                var rawCategory = GetValue(row, headerMap, "correctCategory").Trim();

                if (string.IsNullOrWhiteSpace(plantId) ||
                    string.IsNullOrWhiteSpace(scientificName) ||
                    string.IsNullOrWhiteSpace(imagePath) ||
                    string.IsNullOrWhiteSpace(rawCategory))
                {
                    errorMessage = $"Fila {rowIndex + 1}: faltan campos obligatorios.";
                    definitions.Clear();
                    return false;
                }

                if (!seenPlantIds.Add(plantId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: plantId repetido '{plantId}'.";
                    definitions.Clear();
                    return false;
                }

                if (!GardenSmellTaxonomyCategoryLabels.TryParse(rawCategory, out var category))
                {
                    errorMessage = $"Fila {rowIndex + 1}: categoria no valida '{rawCategory}'.";
                    definitions.Clear();
                    return false;
                }

                definitions.Add(new GardenSmellTaxonomyPlantDefinition(plantId, scientificName, imagePath, category));
            }

            if (definitions.Count < 3)
            {
                errorMessage = "El CSV necesita al menos tres plantas validas.";
                return false;
            }

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

        private static string GetValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key)
        {
            if (!headerMap.TryGetValue(key, out var columnIndex))
            {
                return string.Empty;
            }

            return columnIndex >= 0 && columnIndex < row.Count ? row[columnIndex] ?? string.Empty : string.Empty;
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
            var currentCell = new System.Text.StringBuilder();
            var insideQuotes = false;

            for (var index = 0; index < csvContent.Length; index++)
            {
                var currentChar = csvContent[index];
                if (insideQuotes)
                {
                    if (currentChar == '"')
                    {
                        var isEscapedQuote = index + 1 < csvContent.Length && csvContent[index + 1] == '"';
                        if (isEscapedQuote)
                        {
                            currentCell.Append('"');
                            index++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(currentChar);
                    }

                    continue;
                }

                switch (currentChar)
                {
                    case '"':
                        insideQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    default:
                        currentCell.Append(currentChar);
                        break;
                }
            }

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]))
            {
                rows.Add(currentRow);
            }

            return rows;
        }
    }
}
