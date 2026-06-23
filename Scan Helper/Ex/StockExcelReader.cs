using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScanHelper
{
    public static class StockExcelReader
    {
        private static readonly string[] NameHeaders =
        {
            "назва товару", "название товара", "наименование", "товар", "название"
        };

        private static readonly string[] BarcodeHeaders =
        {
            "штрих-код", "штрихкод", "barcode", "ean"
        };

        private static readonly string[] QtyHeaders =
        {
            "кількість", "количество", "qty", "остаток", "остатки"
        };

        private static readonly string[] LocationHeaders =
        {
            "місце зберігання", "место хранения", "локация", "location"
        };

        public static List<StockRow> ReadStockRows(string path)
        {
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            var headerRowNumber = FindHeaderRow(ws);
            if (headerRowNumber == -1)
                throw new Exception("Не знайшов рядок заголовків. Переконайся, що є колонки типу 'Штрих-код' та 'Місце зберігання'.");

            var headerRow = ws.Row(headerRowNumber);

            int colName = FindColumn(headerRow, NameHeaders);
            int colBarcode = FindColumn(headerRow, BarcodeHeaders);
            int colQty = FindColumn(headerRow, QtyHeaders);
            int colLocation = FindColumn(headerRow, LocationHeaders);

            if (colBarcode == -1) throw new Exception("Не знайшов колонку 'Штрих-код/Штрихкод'.");
            if (colQty == -1) throw new Exception("Не знайшов колонку 'Кількість/Количество'.");
            if (colLocation == -1) throw new Exception("Не знайшов колонку 'Місце зберігання/Место хранения'.");

            var results = new List<StockRow>();
            var lastRow = ws.LastRowUsed().RowNumber();

            for (int r = headerRowNumber + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                var barcode = GetCellString(row.Cell(colBarcode));
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                var qty = GetCellInt(row.Cell(colQty));
                if (qty <= 0) continue; // ✅ НЕ показываем нули (и минусы тоже)

                var locRaw = GetCellString(row.Cell(colLocation));
                var name = colName != -1 ? GetCellString(row.Cell(colName)) : "";

                var locations = SplitLocations(locRaw);

                results.Add(new StockRow
                {
                    ProductName = name?.Trim() ?? "",
                    Barcode = barcode.Trim(),
                    Qty = qty,
                    Locations = locations
                });
            }

            return results;
        }

        private static int FindHeaderRow(IXLWorksheet ws)
        {
            for (int r = 1; r <= 30; r++)
            {
                var cells = ws.Row(r).CellsUsed().Select(c => (c.GetString() ?? "").Trim().ToLowerInvariant());
                var text = string.Join(" | ", cells);

                if (text.Contains("штрих") && (text.Contains("місце") || text.Contains("место")))
                    return r;
            }
            return -1;
        }

        private static int FindColumn(IXLRow headerRow, string[] expected)
        {
            foreach (var cell in headerRow.CellsUsed())
            {
                var h = (cell.GetString() ?? "").Trim().ToLowerInvariant();
                if (expected.Any(e => h.Contains(e)))
                    return cell.Address.ColumnNumber;
            }
            return -1;
        }

        private static string GetCellString(IXLCell cell)
        {
            var s = cell.GetFormattedString();
            return s?.Trim() ?? "";
        }

        private static int GetCellInt(IXLCell cell)
        {
            var s = GetCellString(cell);
            if (string.IsNullOrWhiteSpace(s)) return 0;

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i;

            s = s.Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return (int)Math.Round(d);

            return 0;
        }

        private static List<string> SplitLocations(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

            return raw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
