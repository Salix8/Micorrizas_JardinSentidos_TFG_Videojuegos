using System;
using System.Collections.Generic;
using System.Text;
using SmartCampus.Shared.Csv;

namespace SmartCampus.Dialogue
{
    public static class DialogueCsvService
    {
        private static readonly Dictionary<string, string> LocaleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "spanish", "es-ES" },
            { "espanol", "es-ES" },
            { "es", "es-ES" },
            { "english", "en-US" },
            { "en", "en-US" },
            { "catalan", "ca-CA" },
            { "catala", "ca-CA" },
            { "ca", "ca-CA" }
        };

        public static bool TryParse(string csvContent, out List<DialogueLine> lines, out string errorMessage)
        {
            lines = new List<DialogueLine>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                errorMessage = "El CSV de dialogos esta vacio.";
                return false;
            }

            var table = CsvTableParser.Parse(csvContent);
            if (table.RowCount <= 1)
            {
                errorMessage = "El CSV de dialogos necesita cabecera y al menos una fila.";
                return false;
            }

            var headers = table.Rows[0];
            var headerMap = BuildHeaderMap(headers);
            if (!TryGetHeaderIndex(headerMap, out var idColumnIndex, "stringid", "id", "key"))
            {
                errorMessage = "Falta la columna requerida 'String ID'.";
                return false;
            }

            if (!TryGetHeaderIndex(headerMap, out var characterColumnIndex, "character", "speaker", "personaje"))
            {
                errorMessage = "Falta la columna requerida 'Character'.";
                return false;
            }

            TryGetHeaderIndex(headerMap, out var actOrLocationColumnIndex, "actlocation", "act", "location", "ubicacion");
            TryGetHeaderIndex(headerMap, out var contextNotesColumnIndex, "contextnotes", "notes", "context", "notas");

            var localeColumns = FindLocaleColumns(headers);
            if (localeColumns.Count == 0)
            {
                errorMessage = "El CSV necesita al menos una columna de idioma, por ejemplo 'Spanish (es-ES)'.";
                return false;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var rowIndex = 1; rowIndex < table.RowCount; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                if (CsvTableParser.IsEmptyRow(row))
                {
                    continue;
                }

                var stringId = GetValue(row, idColumnIndex).Trim();
                if (string.IsNullOrWhiteSpace(stringId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: falta String ID.";
                    lines.Clear();
                    return false;
                }

                if (!seenIds.Add(stringId))
                {
                    errorMessage = $"Fila {rowIndex + 1}: String ID repetido '{stringId}'.";
                    lines.Clear();
                    return false;
                }

                var character = GetValue(row, characterColumnIndex).Trim();
                if (string.IsNullOrWhiteSpace(character))
                {
                    errorMessage = $"Fila {rowIndex + 1}: falta Character.";
                    lines.Clear();
                    return false;
                }

                var localizedTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var localeColumn in localeColumns)
                {
                    var localizedText = GetValue(row, localeColumn.Value).Trim();
                    if (!string.IsNullOrWhiteSpace(localizedText))
                    {
                        localizedTexts[localeColumn.Key] = localizedText;
                    }
                }

                if (localizedTexts.Count == 0)
                {
                    errorMessage = $"Fila {rowIndex + 1}: no contiene texto en ningun idioma.";
                    lines.Clear();
                    return false;
                }

                lines.Add(new DialogueLine(
                    stringId,
                    character,
                    GetValue(row, actOrLocationColumnIndex),
                    GetValue(row, contextNotesColumnIndex),
                    localizedTexts));
            }

            if (lines.Count == 0)
            {
                errorMessage = "El CSV no contiene dialogos validos.";
                return false;
            }

            return true;
        }

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headerRow.Count; index++)
            {
                var normalizedHeader = NormalizeHeader(headerRow[index]);
                if (!string.IsNullOrWhiteSpace(normalizedHeader) && !map.ContainsKey(normalizedHeader))
                {
                    map.Add(normalizedHeader, index);
                }
            }

            return map;
        }

        private static Dictionary<string, int> FindLocaleColumns(IReadOnlyList<string> headers)
        {
            var localeColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                var header = CleanHeader(headers[index]);
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                var locale = ExtractLocaleFromHeader(header);
                if (string.IsNullOrWhiteSpace(locale))
                {
                    LocaleAliases.TryGetValue(NormalizeHeader(header), out locale);
                }

                if (!string.IsNullOrWhiteSpace(locale) && !localeColumns.ContainsKey(locale))
                {
                    localeColumns.Add(locale.Trim(), index);
                }
            }

            return localeColumns;
        }

        private static bool TryGetHeaderIndex(IReadOnlyDictionary<string, int> headerMap, out int index, params string[] candidateKeys)
        {
            foreach (var candidateKey in candidateKeys)
            {
                if (headerMap.TryGetValue(candidateKey, out index))
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static string ExtractLocaleFromHeader(string header)
        {
            var openParenthesisIndex = header.LastIndexOf('(');
            var closeParenthesisIndex = header.LastIndexOf(')');
            if (openParenthesisIndex < 0 || closeParenthesisIndex <= openParenthesisIndex)
            {
                return string.Empty;
            }

            return header.Substring(openParenthesisIndex + 1, closeParenthesisIndex - openParenthesisIndex - 1).Trim();
        }

        private static string NormalizeHeader(string header)
        {
            var cleanHeader = CleanHeader(header);
            if (string.IsNullOrWhiteSpace(cleanHeader))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(cleanHeader.Length);
            for (var index = 0; index < cleanHeader.Length; index++)
            {
                var character = cleanHeader[index];
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static string CleanHeader(string header)
        {
            return string.IsNullOrEmpty(header) ? string.Empty : header.Trim().TrimStart('\uFEFF');
        }

        private static string GetValue(IReadOnlyList<string> row, int index)
        {
            return index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
        }
    }
}
