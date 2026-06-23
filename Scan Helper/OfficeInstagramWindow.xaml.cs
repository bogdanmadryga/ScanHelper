using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ScanHelper
{
    public partial class OfficeInstagramWindow : Window
    {
        private List<SimpleStockRow> _officeRows = new();
        private List<SimpleStockRow> _instagramRows = new();

        public OfficeInstagramWindow()
        {
            InitializeComponent();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся в меню (без закрытия всего приложения)
            if (Owner != null) Owner.Show();
            else new MenuWindow().Show();

            Close();
        }

        private void LoadOffice_Click(object sender, RoutedEventArgs e)
        {
            var path = PickExcel("Оберіть Excel файл (ОФІС)");
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                OfficePathText.Text = "Офіс: " + path;
                _officeRows = SimpleExcelReader.ReadRows(path);
                TryBuild();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка читання Excel (ОФІС):\n" + ex.Message);
            }
        }

        private void LoadInstagram_Click(object sender, RoutedEventArgs e)
        {
            var path = PickExcel("Оберіть Excel файл (ІНСТАГРАМ)");
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                InstagramPathText.Text = "Інстаграм: " + path;
                _instagramRows = SimpleExcelReader.ReadRows(path);
                TryBuild();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка читання Excel (ІНСТАГРАМ):\n" + ex.Message);
            }
        }

        private static string PickExcel(string title)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = title
            };
            return dlg.ShowDialog() == true ? dlg.FileName : "";
        }

        private void TryBuild()
        {
            ResultGrid.ItemsSource = null;

            if (_officeRows.Count == 0 || _instagramRows.Count == 0)
            {
                InfoText.Text = "";
                return;
            }

            // Инстаграм: штрихкод существует, "нет места" если ВСЕ места пустые
            var instaMap = _instagramRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Barcode))
                .GroupBy(r => r.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var hasLocation = g.Any(x => !string.IsNullOrWhiteSpace(x.Location));
                        return new InstaInfo { HasLocation = hasLocation };
                    },
                    StringComparer.OrdinalIgnoreCase);

            // Офис: агрегируем количество, места, и берём имя товара (первое непустое)
            var officeAgg = _officeRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Barcode))
                .GroupBy(r => r.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var qty = g.Sum(x => x.Qty);

                    var locs = g.Select(x => (x.Location ?? "").Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                    var name = g.Select(x => (x.ProductName ?? "").Trim())
                                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";

                    return new
                    {
                        Barcode = g.Key,
                        Qty = qty,
                        Locations = locs,
                        ProductName = name
                    };
                })
                .ToList();

            // Условия:
            // - штрихкод есть в инсте
            // - в инсте НЕТ места
            // - в офисе qty > 0
            // - в офисе есть место
            var result = officeAgg
                .Where(o =>
                    instaMap.ContainsKey(o.Barcode) &&
                    instaMap[o.Barcode].HasLocation == false &&
                    o.Qty > 0 &&
                    o.Locations.Count > 0)
                .Select(o => new OfficeInstagramResult
                {
                    Barcode = o.Barcode,
                    OfficeQty = o.Qty,
                    OfficeLocations = string.Join(", ", o.Locations),
                    ProductName = o.ProductName
                })
                .OrderBy(r => r.Barcode)
                .ToList();

            ResultGrid.ItemsSource = result;
            InfoText.Text = $"Офіс рядків: {_officeRows.Count} | Інста рядків: {_instagramRows.Count} | Знайдено: {result.Count}";
        }

        private class InstaInfo
        {
            public bool HasLocation { get; set; }
        }
    }

    public class OfficeInstagramResult
    {
        public string Barcode { get; set; } = "";
        public int OfficeQty { get; set; }
        public string OfficeLocations { get; set; } = "";
        public string ProductName { get; set; } = "";
    }
}