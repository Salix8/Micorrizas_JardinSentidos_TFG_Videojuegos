using System.Collections.Generic;
using System.Text;

namespace SmartCampus.Shared.Csv
{
    public sealed class CsvTable
    {
        public CsvTable(IReadOnlyList<IReadOnlyList<string>> rows)
        {
            Rows = rows;
        }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
        public int RowCount => Rows.Count;
    }

    public static class CsvTableParser
    {
        public static CsvTable Parse(string csvContent, char delimiter = ',')
        {
            var rows = new List<IReadOnlyList<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var insideQuotes = false;

            if (string.IsNullOrEmpty(csvContent))
            {
                return new CsvTable(rows);
            }

            for (var index = 0; index < csvContent.Length; index++)
            {
                var character = csvContent[index];

                if (character == '"')
                {
                    if (insideQuotes && index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                    {
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }

                    continue;
                }

                if (!insideQuotes && character == delimiter)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if (!insideQuotes && (character == '\n' || character == '\r'))
                {
                    if (character == '\r' && index + 1 < csvContent.Length && csvContent[index + 1] == '\n')
                    {
                        index++;
                    }

                    currentRow.Add(currentCell.ToString());
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    currentCell.Clear();
                    continue;
                }

                currentCell.Append(character);
            }

            currentRow.Add(currentCell.ToString());
            rows.Add(currentRow);

            return new CsvTable(rows);
        }

        public static bool IsEmptyRow(IReadOnlyList<string> row)
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
    }
}
