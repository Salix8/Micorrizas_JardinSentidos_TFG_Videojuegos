using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public static class DialogueCsvService
    {
        private const string StringIdHeader = "String ID";
        private const string CharacterHeader = "Character";
        private const string SequenceHeader = "Act/Location";
        private const string ContextHeader = "Context/Notes";
        private const string SpanishHeader = "Spanish (es-ES)";
        private const string EnglishHeader = "English (en-US)";
        private const string ValencianHeader = "Catalan (ca-CA)";

        public static bool TryParse(string csvContent, out List<DialogueLineDefinition> definitions, out string errorMessage)
        {
            definitions = new List<DialogueLineDefinition>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV de dialogos esta vacio.";
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

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                var stringId = GetValue(row, headerMap, StringIdHeader).Trim();
                var characterId = GetValue(row, headerMap, CharacterHeader).Trim();
                var sequenceKey = GetValue(row, headerMap, SequenceHeader).Trim();
                var contextNotes = GetValue(row, headerMap, ContextHeader).Trim();
                var spanishText = GetValue(row, headerMap, SpanishHeader);
                var englishText = GetValue(row, headerMap, EnglishHeader);
                var valencianText = GetValue(row, headerMap, ValencianHeader);

                if (string.IsNullOrWhiteSpace(stringId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: String ID es obligatorio.";
                    definitions.Clear();
                    return false;
                }

                if (!seenIds.Add(stringId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: String ID repetido '{stringId}'.";
                    definitions.Clear();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(sequenceKey))
                {
                    errorMessage = $"Fila {rowIndex + 1}: Act/Location es obligatorio.";
                    definitions.Clear();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(spanishText) &&
                    string.IsNullOrWhiteSpace(englishText) &&
                    string.IsNullOrWhiteSpace(valencianText))
                {
                    errorMessage = $"Fila {rowIndex + 1}: la linea '{stringId}' no contiene texto localizable.";
                    definitions.Clear();
                    return false;
                }

                definitions.Add(new DialogueLineDefinition(
                    stringId,
                    characterId,
                    sequenceKey,
                    contextNotes,
                    spanishText,
                    englishText,
                    valencianText));
            }

            if (definitions.Count == 0)
            {
                errorMessage = "El CSV no contiene lineas de dialogo validas.";
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
            var requiredColumns = new[]
            {
                StringIdHeader,
                CharacterHeader,
                SequenceHeader,
                ContextHeader,
                SpanishHeader,
                EnglishHeader,
                ValencianHeader
            };

            for (var index = 0; index < requiredColumns.Length; index++)
            {
                if (!headerMap.ContainsKey(requiredColumns[index]))
                {
                    errorMessage = $"Falta la columna requerida '{requiredColumns[index]}' en el CSV.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
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

            if (insideQuotes)
            {
                currentCell = currentCell.Replace("\r", string.Empty);
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell);
                rows.Add(currentRow);
            }

            return rows;
        }
    }
}
