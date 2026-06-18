using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public sealed class PlantPhotoRelayPlantDefinition
    {
        public PlantPhotoRelayPlantDefinition(
            string commonNameCanonical,
            string displayCommonName,
            string[] acceptedCommonNameVariants,
            string plantType,
            string surfaceTexture,
            bool hasThorns,
            bool hasFruit,
            string leafType,
            string sizeCategory)
        {
            CommonNameCanonical = commonNameCanonical;
            DisplayCommonName = displayCommonName;
            AcceptedCommonNameVariants = acceptedCommonNameVariants ?? Array.Empty<string>();
            PlantType = plantType;
            SurfaceTexture = surfaceTexture;
            HasThorns = hasThorns;
            HasFruit = hasFruit;
            LeafType = leafType;
            SizeCategory = sizeCategory;
        }

        public string CommonNameCanonical { get; }
        public string DisplayCommonName { get; }
        public IReadOnlyList<string> AcceptedCommonNameVariants { get; }
        public string PlantType { get; }
        public string SurfaceTexture { get; }
        public bool HasThorns { get; }
        public bool HasFruit { get; }
        public string LeafType { get; }
        public string SizeCategory { get; }
    }

    public static class PlantPhotoRelayCatalogService
    {
        public static bool TryParse(
            string csvContent,
            out List<PlantPhotoRelayPlantDefinition> plantDefinitions,
            out string errorMessage)
        {
            plantDefinitions = new List<PlantPhotoRelayPlantDefinition>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV del catalogo esta vacio.";
                return false;
            }

            var rows = ParseRows(csvContent);
            if (rows.Count <= 1)
            {
                errorMessage = "El CSV necesita cabecera y al menos una fila.";
                return false;
            }

            var headerMap = BuildHeaderMap(rows[0]);
            var requiredColumns = new[]
            {
                "commonNameCanonical",
                "displayCommonName",
                "acceptedCommonNameVariants",
                "plantType",
                "surfaceTexture",
                "hasThorns",
                "hasFruit",
                "leafType",
                "sizeCategory"
            };

            for (var columnIndex = 0; columnIndex < requiredColumns.Length; columnIndex++)
            {
                if (!headerMap.ContainsKey(requiredColumns[columnIndex]))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumns[columnIndex]}'.";
                    return false;
                }
            }

            var seenCanonicals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                var canonical = GetValue(row, headerMap, "commonNameCanonical").Trim();
                var display = GetValue(row, headerMap, "displayCommonName").Trim();
                var variants = SplitVariants(GetValue(row, headerMap, "acceptedCommonNameVariants"));
                var plantType = GetValue(row, headerMap, "plantType").Trim();
                var surfaceTexture = GetValue(row, headerMap, "surfaceTexture").Trim();
                var leafType = GetValue(row, headerMap, "leafType").Trim();
                var sizeCategory = GetValue(row, headerMap, "sizeCategory").Trim();

                if (string.IsNullOrWhiteSpace(canonical) ||
                    string.IsNullOrWhiteSpace(display) ||
                    string.IsNullOrWhiteSpace(plantType) ||
                    string.IsNullOrWhiteSpace(surfaceTexture) ||
                    string.IsNullOrWhiteSpace(leafType) ||
                    string.IsNullOrWhiteSpace(sizeCategory))
                {
                    errorMessage = $"Fila {rowIndex + 1}: faltan campos obligatorios.";
                    plantDefinitions.Clear();
                    return false;
                }

                if (!TryParseBoolean(GetValue(row, headerMap, "hasThorns"), out var hasThorns) ||
                    !TryParseBoolean(GetValue(row, headerMap, "hasFruit"), out var hasFruit))
                {
                    errorMessage = $"Fila {rowIndex + 1}: los campos booleanos deben ser true/false, si/no o 1/0.";
                    plantDefinitions.Clear();
                    return false;
                }

                if (!seenCanonicals.Add(canonical))
                {
                    errorMessage = $"Fila {rowIndex + 1}: commonNameCanonical repetido '{canonical}'.";
                    plantDefinitions.Clear();
                    return false;
                }

                plantDefinitions.Add(new PlantPhotoRelayPlantDefinition(
                    canonical,
                    display,
                    variants,
                    plantType,
                    surfaceTexture,
                    hasThorns,
                    hasFruit,
                    leafType,
                    sizeCategory));
            }

            plantDefinitions.Sort((left, right) => string.Compare(left.DisplayCommonName, right.DisplayCommonName, StringComparison.OrdinalIgnoreCase));
            if (plantDefinitions.Count < 2)
            {
                errorMessage = "El catalogo necesita al menos dos plantas validas.";
                return false;
            }

            return true;
        }

        private static bool TryParseBoolean(string rawValue, out bool parsedValue)
        {
            var normalized = rawValue == null ? string.Empty : rawValue.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "true":
                case "1":
                case "si":
                case "sí":
                case "yes":
                    parsedValue = true;
                    return true;
                case "false":
                case "0":
                case "no":
                    parsedValue = false;
                    return true;
                default:
                    parsedValue = false;
                    return false;
            }
        }

        private static string[] SplitVariants(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Array.Empty<string>();
            }

            return rawValue
                .Split('|')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
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
            var currentField = string.Empty;
            var insideQuotes = false;

            for (var index = 0; index < csvContent.Length; index++)
            {
                var character = csvContent[index];
                if (insideQuotes)
                {
                    if (character == '"')
                    {
                        if (index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                        {
                            currentField += '"';
                            index++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        currentField += character;
                    }

                    continue;
                }

                switch (character)
                {
                    case '"':
                        insideQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentField);
                        currentField = string.Empty;
                        break;
                    case '\n':
                        currentRow.Add(currentField);
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        currentField = string.Empty;
                        break;
                    case '\r':
                        break;
                    default:
                        currentField += character;
                        break;
                }
            }

            currentRow.Add(currentField);
            rows.Add(currentRow);
            return rows;
        }
    }
}
