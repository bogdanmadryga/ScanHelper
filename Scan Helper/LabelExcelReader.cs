using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;

namespace ScanHelper
{
    public static class LabelExcelReader
    {
        public static List<LabelItem> Read(string path)
        {
            using var wb = new XLWorkbook(path);

            var ws = wb.Worksheets.First();

            var result = new List<LabelItem>();

            int lastRow =
                ws.LastRowUsed().RowNumber();

            for (int r = 4; r <= lastRow; r++)
            {
                // B = Назва товару
                var name =
                    ws.Cell(r, 2)
                      .GetString()
                      .Trim();

                // C = Штрихкод
                var barcode =
                    ws.Cell(r, 3)
                      .GetString()
                      .Trim();

                if (string.IsNullOrWhiteSpace(barcode))
                    continue;

                // E = Артикул
                var article =
                    ws.Cell(r, 5)
                      .GetString()
                      .Trim();

                // F = Розмір
                var size =
                    ws.Cell(r, 6)
                      .GetString()
                      .Trim();

                result.Add(new LabelItem
                {
                    Name = name,
                    Barcode = barcode,
                    Article = article,
                    Size = size
                });
            }

            return result;
        }
    }
}