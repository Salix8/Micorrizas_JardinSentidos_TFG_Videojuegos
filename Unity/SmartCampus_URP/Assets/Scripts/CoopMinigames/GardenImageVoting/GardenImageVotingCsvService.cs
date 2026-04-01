using System;
using System.Collections.Generic;
using System.Globalization;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    public sealed class GardenImageVotingCardDefinition
    {
        public GardenImageVotingCardDefinition(int roundIndex, int deviceSlot, string topic, string title, string imagePath, bool isSeenInGarden)
        {
            RoundIndex = roundIndex;
            DeviceSlot = deviceSlot;
            Topic = string.IsNullOrWhiteSpace(topic) ? $"Ronda {roundIndex}" : topic.Trim();
            Title = string.IsNullOrWhiteSpace(title) ? $"Imagen {roundIndex}" : title.Trim();
            ImagePath = imagePath == null ? string.Empty : imagePath.Trim();
            IsSeenInGarden = isSeenInGarden;
        }

        public int RoundIndex { get; }
        public int DeviceSlot { get; }
        public string Topic { get; }
        public string Title { get; }
        public string ImagePath { get; }
        public bool IsSeenInGarden { get; }
    }

    public static class GardenImageVotingCsvService
    {
        public static bool TryParse(
            string csvContent,
            int maxSupportedDevices,
            int cardsPerDevice,
            bool allowRepeatedImagesAcrossDevices,
            out List<GardenImageVotingCardDefinition> definitions,
            out string errorMessage)
        {
            definitions = new List<GardenImageVotingCardDefinition>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV del minijuego esta vacio.";
                return false;
            }

            var rows = ParseRows(csvContent);
            if (rows.Count <= 1)
            {
                errorMessage = "El CSV necesita cabecera y al menos una fila de datos.";
                return false;
            }

            var headerMap = BuildHeaderMap(rows[0]);
            if (!ValidateRequiredColumns(headerMap, out errorMessage))
            {
                return false;
            }

            var imageKeysByRound = new Dictionary<int, HashSet<string>>();
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                if (!TryGetInt(row, headerMap, "roundIndex", out var roundIndex) ||
                    !TryGetInt(row, headerMap, "deviceSlot", out var deviceSlot))
                {
                    errorMessage = $"Fila {rowIndex + 1}: roundIndex y deviceSlot deben ser enteros positivos.";
                    definitions.Clear();
                    return false;
                }

                if (roundIndex <= 0 || roundIndex > cardsPerDevice)
                {
                    errorMessage = $"Fila {rowIndex + 1}: roundIndex debe estar entre 1 y {cardsPerDevice}.";
                    definitions.Clear();
                    return false;
                }

                if (deviceSlot <= 0 || deviceSlot > maxSupportedDevices)
                {
                    errorMessage = $"Fila {rowIndex + 1}: deviceSlot debe estar entre 1 y {maxSupportedDevices}.";
                    definitions.Clear();
                    return false;
                }

                var topic = GetValue(row, headerMap, "topic");
                var title = GetValue(row, headerMap, "title");
                var imagePath = GetValue(row, headerMap, "imagePath");

                if (!TryGetBool(row, headerMap, "isSeenInGarden", out var isSeenInGarden))
                {
                    errorMessage = $"Fila {rowIndex + 1}: isSeenInGarden debe ser true/false, yes/no o 1/0.";
                    definitions.Clear();
                    return false;
                }

                if (!allowRepeatedImagesAcrossDevices)
                {
                    if (!imageKeysByRound.TryGetValue(roundIndex, out var imageKeys))
                    {
                        imageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        imageKeysByRound.Add(roundIndex, imageKeys);
                    }

                    var duplicateKey = string.IsNullOrWhiteSpace(imagePath)
                        ? $"{topic.Trim().ToLowerInvariant()}|{title.Trim().ToLowerInvariant()}"
                        : imagePath.Trim();

                    if (!imageKeys.Add(duplicateKey))
                    {
                        errorMessage = $"Fila {rowIndex + 1}: hay una imagen repetida en la ronda {roundIndex} y la configuracion actual no permite repetirla entre dispositivos.";
                        definitions.Clear();
                        return false;
                    }
                }

                definitions.Add(new GardenImageVotingCardDefinition(roundIndex, deviceSlot, topic, title, imagePath, isSeenInGarden));
            }

            definitions.Sort((left, right) =>
            {
                var roundComparison = left.RoundIndex.CompareTo(right.RoundIndex);
                if (roundComparison != 0)
                {
                    return roundComparison;
                }

                var slotComparison = left.DeviceSlot.CompareTo(right.DeviceSlot);
                return slotComparison != 0 ? slotComparison : string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
            });

            if (definitions.Count == 0)
            {
                errorMessage = "El CSV no contiene cartas validas.";
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

        private static bool ValidateRequiredColumns(IReadOnlyDictionary<string, int> headerMap, out string errorMessage)
        {
            var requiredColumns = new[] { "roundIndex", "deviceSlot", "topic", "title", "imagePath", "isSeenInGarden" };
            foreach (var requiredColumn in requiredColumns)
            {
                if (!headerMap.ContainsKey(requiredColumn))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumn}' en el CSV.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryGetInt(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key, out int value)
        {
            value = 0;
            return int.TryParse(GetValue(row, headerMap, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetBool(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string key, out bool value)
        {
            var rawValue = GetValue(row, headerMap, key).Trim();
            if (bool.TryParse(rawValue, out value))
            {
                return true;
            }

            switch (rawValue.ToLowerInvariant())
            {
                case "1":
                case "si":
                case "sí":
                case "yes":
                case "y":
                    value = true;
                    return true;
                case "0":
                case "no":
                case "n":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
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
