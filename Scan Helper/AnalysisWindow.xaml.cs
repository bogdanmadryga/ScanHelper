using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ScanHelper
{
    public partial class AnalysisWindow : Window
    {
        private List<StockRow> _rows = new();

        private List<MixedZoneResult> _mixed = new();
        private List<MixedZoneResult> _plomb = new();
        private List<MixedZoneResult> _hanging = new();
        private List<MixedZoneResult> _lying = new();
        private List<MixedZoneResult> _unbound = new();

        private const int PlombThreshold = 100;

        public AnalysisWindow()
        {
            InitializeComponent();
            UpdateTotalQty(null);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new MenuWindow().Show();
            Close();
        }

        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Оберіть Excel файл зі станом складу"
            };

            if (dlg.ShowDialog() != true) return;

            FilePathText.Text = dlg.FileName;
            InfoText.Text = "Читаю файл...";

            try
            {
                _rows = StockExcelReader.ReadStockRows(dlg.FileName);

                _mixed = StockAnalyzer.FindMixedZoneItems(_rows);
                _plomb = BuildPlombList(_rows, PlombThreshold);
                _hanging = BuildHangingList(_rows);
                _lying = BuildLyingList(_rows);
                _unbound = BuildUnboundList(_rows);

                var (unboundPercent, unboundQty, totalQty) = CalcUnboundPercent(_rows);

                // по умолчанию: різні зони
                ResultGrid.ItemsSource = _mixed;
                UpdateTotalQty(_mixed);

                InfoText.Text =
                    $"Рядків (без нулів): {_rows.Count} | " +
                    $"Різні зони: {_mixed.Count} | " +
                    $"Пломбування > {PlombThreshold}: {_plomb.Count} | " +
                    $"Не прив'язані: {_unbound.Count} | " +
                    $"% не прив'язаних: {unboundPercent:0.00}% (без місця: {unboundQty}) | " +
                    $"Всього к-ть: {totalQty}";
            }
            catch (Exception ex)
            {
                InfoText.Text = "";
                MessageBox.Show("Помилка читання/аналізу Excel:\n" + ex.Message);
            }
        }

        private void ShowMixed_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = _mixed;
            UpdateTotalQty(_mixed);
        }

        private void ShowPlomb_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = _plomb;
            UpdateTotalQty(_plomb);
        }

        private void ShowHanging_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = _hanging;
            UpdateTotalQty(_hanging);
        }

        private void ShowLying_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = _lying;
            UpdateTotalQty(_lying);
        }

        private void ShowUnbound_Click(object sender, RoutedEventArgs e)
        {
            ResultGrid.ItemsSource = _unbound;
            UpdateTotalQty(_unbound);
        }

        // ---------- UI helpers ----------

        private void UpdateTotalQty(IEnumerable<MixedZoneResult> list)
        {
            if (list == null)
            {
                TotalQtyText.Text = "Сума кількості: 0";
                return;
            }

            var sum = list.Sum(x => x.TotalQty);
            TotalQtyText.Text = $"Сума кількості: {sum}";
        }

        private static (double percent, int unboundQty, int totalQty) CalcUnboundPercent(List<StockRow> rows)
        {
            int totalQty = rows.Sum(r => r.Qty);

            int unboundQty = rows
                .Where(r => r.Qty > 0)
                .Where(r => r.Locations == null || r.Locations.Count == 0 || r.Locations.All(s => string.IsNullOrWhiteSpace(s)))
                .Sum(r => r.Qty);

            double percent = totalQty <= 0 ? 0 : (unboundQty * 100.0 / totalQty);
            return (percent, unboundQty, totalQty);
        }

        // ---------- Lists builders ----------

        private static List<MixedZoneResult> BuildUnboundList(List<StockRow> rows)
        {
            var groups = rows
                .GroupBy(r => (r.Barcode ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var res = new List<MixedZoneResult>();

            foreach (var g in groups)
            {
                var totalQty = g.Sum(x => x.Qty);
                if (totalQty <= 0) continue;

                bool allUnbound = g.All(x =>
                    x.Locations == null ||
                    x.Locations.Count == 0 ||
                    x.Locations.All(s => string.IsNullOrWhiteSpace(s)));

                if (!allUnbound) continue;

                var name = g.Select(x => x.ProductName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";

                res.Add(new MixedZoneResult
                {
                    ProductName = name,
                    Barcode = g.Key,
                    TotalQty = totalQty,
                    Zones = "",
                    Locations = ""
                });
            }

            return res
                .OrderByDescending(x => x.TotalQty)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.Barcode)
                .ToList();
        }

        private static List<MixedZoneResult> BuildPlombList(List<StockRow> rows, int threshold)
        {
            var groups = rows
                .GroupBy(r => (r.Barcode ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var res = new List<MixedZoneResult>();

            foreach (var g in groups)
            {
                int totalQty = g.Sum(x => x.Qty);
                if (totalQty <= threshold) continue;

                var name = g.Select(x => x.ProductName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";

                var allLocations = g
                    .SelectMany(x => x.Locations ?? new List<string>())
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var zones = allLocations
                    .Select(GetZoneKeyForUi) // ✅ внутри теперь нормализуем для логики
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                res.Add(new MixedZoneResult
                {
                    ProductName = name,
                    Barcode = g.Key,
                    TotalQty = totalQty,
                    Zones = string.Join(", ", zones),
                    Locations = string.Join(", ", allLocations)
                });
            }

            return res
                .OrderByDescending(x => x.TotalQty)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.Barcode)
                .ToList();
        }

        private static List<MixedZoneResult> BuildHangingList(List<StockRow> rows)
        {
            return BuildByPlacement(rows, wantHanging: true);
        }

        private static List<MixedZoneResult> BuildLyingList(List<StockRow> rows)
        {
            return BuildByPlacement(rows, wantHanging: false);
        }

        private static List<MixedZoneResult> BuildByPlacement(List<StockRow> rows, bool wantHanging)
        {
            var groups = rows
                .GroupBy(r => (r.Barcode ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var res = new List<MixedZoneResult>();

            foreach (var g in groups)
            {
                int totalQty = g.Sum(x => x.Qty);
                if (totalQty <= 0) continue;

                var allLocations = g
                    .SelectMany(x => x.Locations ?? new List<string>())
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (allLocations.Count == 0) continue;

                bool matches = allLocations.Any(loc =>
                {
                    var kind = ClassifyLocation(loc); // ✅ fix inside
                    return wantHanging ? (kind == LocationKind.Hanging) : (kind == LocationKind.Lying);
                });

                if (!matches) continue;

                var name = g.Select(x => x.ProductName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "";

                var zones = allLocations
                    .Select(GetZoneKeyForUi) // ✅ fix inside
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                res.Add(new MixedZoneResult
                {
                    ProductName = name,
                    Barcode = g.Key,
                    TotalQty = totalQty,
                    Zones = string.Join(", ", zones),
                    Locations = string.Join(", ", allLocations)
                });
            }

            return res
                .OrderByDescending(x => x.TotalQty)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.Barcode)
                .ToList();
        }

        // ---------- LOCATION RULES (FIX HERE) ----------

        private enum LocationKind
        {
            Unknown = 0,
            Hanging = 1,
            Lying = 2
        }

        // ✅ НОВОЕ: берём только "базу" места хранения для логики (до пробела)
        // "11-03-00 інстаграм" -> "11-03-00"
        private static string NormalizeForRules(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return "";
            var s = location.Trim().ToUpperInvariant();

            // обрезаем всё после первого пробела
            var space = s.IndexOf(' ');
            if (space > 0) s = s.Substring(0, space).Trim();

            return s;
        }

        private static LocationKind ClassifyLocation(string location)
        {
            var s = NormalizeForRules(location);
            if (string.IsNullOrWhiteSpace(s)) return LocationKind.Unknown;

            // стелажи/МС/Ш = лежачий
            if (s.StartsWith("МС") || s.StartsWith("MC") || s.StartsWith("С") || s.StartsWith("Ш"))
                return LocationKind.Lying;

            // улицы вида 11-03-00 / 08-02-01
            int dash1 = s.IndexOf('-');
            if (dash1 > 0)
            {
                var prefix = s.Substring(0, dash1).Trim();
                if (prefix.All(char.IsDigit))
                {
                    // ✅ если этаж "00" => лежачий
                    if (s.EndsWith("-00"))
                        return LocationKind.Lying;

                    return LocationKind.Hanging;
                }
            }

            return LocationKind.Unknown;
        }

        private static string GetZoneKeyForUi(string location)
        {
            var s = NormalizeForRules(location);
            if (string.IsNullOrWhiteSpace(s)) return "";

            var dash = s.IndexOf('-');
            if (dash > 0)
            {
                var prefix = s.Substring(0, dash).Trim();
                if (prefix.All(char.IsDigit))
                    return $"УЛИЦА {prefix}";
            }

            if (s.StartsWith("МС") || s.StartsWith("MC"))
            {
                var tail = new string(s.Skip(2).TakeWhile(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(tail) ? "МС" : $"МС{tail}";
            }

            if (s.StartsWith("С"))
            {
                var tail = new string(s.Skip(1).TakeWhile(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(tail) ? "С" : $"С{tail}";
            }

            if (s.StartsWith("Ш"))
            {
                var tail = new string(s.Skip(1).TakeWhile(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(tail) ? "Ш" : $"Ш{tail}";
            }

            return s;
        }
    }

    public class MixedZoneResult
    {
        public string ProductName { get; set; } = "";
        public string Barcode { get; set; } = "";
        public int TotalQty { get; set; }
        public string Zones { get; set; } = "";
        public string Locations { get; set; } = "";
    }

    public class StockRow
    {
        public string ProductName { get; set; } = "";
        public string Barcode { get; set; } = "";
        public int Qty { get; set; }
        public List<string> Locations { get; set; } = new();
    }
}
