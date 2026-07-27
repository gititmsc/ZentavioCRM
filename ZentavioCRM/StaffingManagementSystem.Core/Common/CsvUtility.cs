using System.Text;

namespace ZentavioCRM.Core.Common
{
    /// <summary>
    /// Minimal hand-rolled RFC4180-ish CSV reader/writer — no external package dependency.
    /// Handles quoted fields (commas, quotes, and newlines inside a field), CRLF/LF line endings,
    /// and doubled-quote escaping ("" -> "). Good enough for CRM export/import of simple tabular
    /// data; not a general-purpose CSV library (no streaming, whole file is read into memory,
    /// which is fine at SMB data volumes).
    /// </summary>
    public static class CsvUtility
    {
        public static string Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(Escape)));
            }

            return sb.ToString();
        }

        private static string Escape(string? value)
        {
            value ??= string.Empty;
            var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!needsQuoting)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        /// <summary>Parses CSV content into rows of raw string fields. The first row is assumed to be a header row and is returned separately.</summary>
        public static (string[] Headers, List<string[]> Rows) Parse(string content)
        {
            var records = ParseRecords(content);
            if (records.Count == 0)
            {
                return ([], []);
            }

            var headers = records[0];
            var rows = records.Skip(1).Where(r => r.Length > 1 || !string.IsNullOrWhiteSpace(r.FirstOrDefault())).ToList();
            return (headers, rows);
        }

        private static List<string[]> ParseRecords(string content)
        {
            var records = new List<string[]>();
            var currentRecord = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;
            var i = 0;

            while (i < content.Length)
            {
                var c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    currentField.Append(c);
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        i++;
                        break;
                    case ',':
                        currentRecord.Add(currentField.ToString());
                        currentField.Clear();
                        i++;
                        break;
                    case '\r':
                        i++;
                        break;
                    case '\n':
                        currentRecord.Add(currentField.ToString());
                        currentField.Clear();
                        records.Add([.. currentRecord]);
                        currentRecord.Clear();
                        i++;
                        break;
                    default:
                        currentField.Append(c);
                        i++;
                        break;
                }
            }

            // Final field/record if the content didn't end with a newline.
            if (currentField.Length > 0 || currentRecord.Count > 0)
            {
                currentRecord.Add(currentField.ToString());
                records.Add([.. currentRecord]);
            }

            return records;
        }
    }
}
