using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScanHelper
{
    public static class SimpleExcelReader
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
            "кількість", "количество", "qty", "остаток", "остатки", "к-ть"
        };

        private static readonly string[] LocationHeaders =
        {
            "місце зберігання", "место хранения", "місце", "место", "локация", "location", "привязка", "место привязки"
        };

        public static List<SimpleStockRow> ReadRows(string path)
        {
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            var headerRowNumber = FindHeaderRow(ws);
            if (headerRowNumber == -1)
                throw new Exception("Не знайшов рядок заголовків. Переконайся, що є колонки типу 'Штрихкод' та 'Місце зберігання/прив’язка'.");

            var headerRow = ws.Row(headerRowNumber);

            int colName = FindColumn(headerRow, NameHeaders); // може бути -1, тоді просто не буде назви
            int colBarcode = FindColumn(headerRow, BarcodeHeaders);
            int colQty = FindColumn(headerRow, QtyHeaders);
            int colLocation = FindColumn(headerRow, LocationHeaders);

            if (colBarcode == -1) throw new Exception("Не знайшов колонку 'Штрих-код/Штрихкод'.");
            if (colQty == -1) throw new Exception("Не знайшов колонку 'Кількість/Количество'.");
            if (colLocation == -1) throw new Exception("Не знайшов колонку 'Місце зберігання/Место хранения/Прив’язка'.");

            var results = new List<SimpleStockRow>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRowNumber;

            for (int r = headerRowNumber + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                var barcode = GetCellString(row.Cell(colBarcode));
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                var qty = GetCellInt(row.Cell(colQty));
                var location = GetCellString(row.Cell(colLocation)); // может быть пусто — важно для Инсты

                var name = "";
                if (colName != -1)
                    name = GetCellString(row.Cell(colName));

                results.Add(new SimpleStockRow
                {
                    ProductName = (name ?? "").Trim(),
                    Barcode = barcode.Trim(),
                    Qty = qty,
                    Location = (location ?? "").Trim()
                });
            }

            return results;
        }

        private static int FindHeaderRow(IXLWorksheet ws)
        {
            for (int r = 1; r <= 30; r++)
            {
                var cells = ws.Row(r).CellsUsed()
                    .Select(c => (c.GetString() ?? "").Trim().ToLowerInvariant());

                var text = string.Join(" | ", cells);

                if (text.Contains("штрих") && (text.Contains("місце") || text.Contains("место") || text.Contains("прив")))
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
    }

    public class SimpleStockRow
    {
        public string ProductName { get; set; } = "";
        public string Barcode { get; set; } = "";
        public int Qty { get; set; }
        public string Location { get; set; } = "";
    }
}