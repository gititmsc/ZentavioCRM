using ClosedXML.Excel;

namespace ZentavioCRM.Services.Common
{
    /// <summary>
    /// Minimal .xlsx reader/writer built on ClosedXML, deliberately shaped to mirror
    /// <see cref="ZentavioCRM.Core.Common.CsvUtility"/>'s Write(headers, rows) / Parse(...) -> (headers, rows)
    /// signatures, so callers (LeadService) can reuse the exact same row-building and
    /// row-parsing logic for both file formats. Whole workbook is built/read in memory,
    /// which is fine at SMB data volumes (same tradeoff CsvUtility makes).
    /// </summary>
    public static class ExcelUtility
    {
        private const string SheetName = "Data";

        public static byte[] Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SheetName);

            for (var c = 0; c < headers.Count; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
            }

            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < row.Count; c++)
                {
                    sheet.Cell(r, c + 1).Value = row[c] ?? string.Empty;
                }
                r++;
            }

            if (headers.Count > 0)
            {
                sheet.Columns(1, headers.Count).AdjustToContents();
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        /// <summary>Parses the first worksheet of an .xlsx file into rows of raw string fields. The first row is assumed to be a header row and is returned separately.</summary>
        public static (string[] Headers, List<string[]> Rows) Parse(Stream xlsxStream)
        {
            using var workbook = new XLWorkbook(xlsxStream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
            {
                return ([], []);
            }

            var rowsUsed = sheet.RowsUsed().ToList();
            if (rowsUsed.Count == 0)
            {
                return ([], []);
            }

            var colCount = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (colCount == 0)
            {
                return ([], []);
            }

            var headers = rowsUsed[0].Cells(1, colCount).Select(c => c.GetString().Trim()).ToArray();

            var rows = rowsUsed
                .Skip(1)
                .Select(row => row.Cells(1, colCount).Select(c => c.GetString()).ToArray())
                .Where(cells => cells.Any(v => !string.IsNullOrWhiteSpace(v)))
                .ToList();

            return (headers, rows);
        }
    }
}
