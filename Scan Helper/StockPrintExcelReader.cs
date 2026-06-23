using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;

namespace ScanHelper
{
    public static class StockPrintExcelReader
    {
        public static List<StockPrintItem> Read(string path)
        {
            using var wb = new XLWorkbook(path);

            var ws = wb.Worksheets.First();

            var list = new List<StockPrintItem>();

            int headerRow = 0;

            for (int r = 1; r <= 20; r++)
            {
                var text = string.Join(" ",
                    ws.Row(r)
                      .CellsUsed()
                      .Select(x => x.GetString().ToLower()));

                if (text.Contains("артикул"))
                {
                    headerRow = r;
                    break;
                }
            }

            if (headerRow == 0)
                return list;

            int barcodeCol = 0;
            int qtyCol = 0;
            int articleCol = 0;
            int sizeCol = 0;
            int nameCol = 0;

            foreach (var cell in ws.Row(headerRow).CellsUsed())
            {
                var txt = cell.GetString().Trim().ToLower();

                if (txt.Contains("штрих"))
                    barcodeCol = cell.Address.ColumnNumber;

                if (txt.Contains("кількість") || txt.Contains("количество"))
                    qtyCol = cell.Address.ColumnNumber;

                if (txt.Contains("артикул"))
                    articleCol = cell.Address.ColumnNumber;

                if (txt.Contains("розмір") || txt.Contains("размер"))
                    sizeCol = cell.Address.ColumnNumber;

                if (txt.Contains("назва") ||
                    txt.Contains("наименование") ||
                    txt.Contains("название"))
                    nameCol = cell.Address.ColumnNumber;
            }

            int row = headerRow + 1;

            while (true)
            {
                var barcode = ws.Cell(row, barcodeCol).GetString().Trim();

                if (string.IsNullOrWhiteSpace(barcode))
                    break;

                int qty = 0;

                int.TryParse(
                    ws.Cell(row, qtyCol).GetString().Trim(),
                    out qty);

                list.Add(new StockPrintItem
                {
                    Barcode = barcode,
                    Article = ws.Cell(row, articleCol).GetString().Trim(),
                    ProductName = nameCol > 0
                        ? ws.Cell(row, nameCol).GetString().Trim()
                        : "",
                    Size = ws.Cell(row, sizeCol).GetString().Trim(),
                    Qty = qty
                });

                row++;
            }

            return list;
        }
    }
}