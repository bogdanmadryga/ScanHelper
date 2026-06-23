using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScanHelper.Data;
using ScanHelper.Models;

namespace ScanHelper
{
    public partial class ScanNVPWindow : Window
    {
        private readonly NVP _nvp;
        private readonly ObservableCollection<NVPItem> _rows;

        private readonly MediaPlayer _errorPlayer = new MediaPlayer();

        public ScanNVPWindow(NVP nvp)
        {
            InitializeComponent();

            _nvp = nvp;

            TitleText.Text = $"Сканирование: {_nvp.Name}";

            // делаем “рабочую копию” (редактируем факт прямо тут)
            _rows = new ObservableCollection<NVPItem>(
                _nvp.Items.Select(i => new NVPItem
                {
                    Barcode = i.Barcode,
                    ExpectedCount = i.ExpectedCount,
                    ScannedCount = i.ScannedCount,
                    Status = NvpRowStatus.Under,
                    IsLastScanned = false
                })
            );

            ItemsGrid.ItemsSource = _rows;

            LoadErrorSound();
            RecalcAll();

            Loaded += (_, __) => BarcodeInput.Focus();
        }

        private void LoadErrorSound()
        {
            try
            {
                // Ищем mp3 в папке приложения: Assets/error.mp3
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "error.mp3");
                if (File.Exists(path))
                {
                    _errorPlayer.Open(new Uri(path, UriKind.Absolute));
                    _errorPlayer.Volume = 1.0; // максимум
                }
            }
            catch
            {
                // если не загрузился — просто без звука
            }
        }

        private void PlayError()
        {
            try
            {
                if (_errorPlayer.Source == null) return;
                _errorPlayer.Stop();
                _errorPlayer.Position = TimeSpan.Zero;
                _errorPlayer.Play();
            }
            catch { }
        }

        // ENTER в поле штрихкода
        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string code = BarcodeInput.Text.Trim();
            BarcodeInput.Clear();
            BarcodeInput.Focus();

            if (string.IsNullOrWhiteSpace(code))
                return;

            var item = _rows.FirstOrDefault(x => x.Barcode == code);

            if (item == null)
            {
                PlayError();
                MessageBox.Show($"Штрихкод не найден:\n{code}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ставим “последний”
            foreach (var r in _rows) r.IsLastScanned = false;
            item.IsLastScanned = true;

            // +1 к факту
            item.ScannedCount += 1;

            // пересчёт
            UpdateRowStatus(item);
            UpdateTotals();

            // выделение + скролл
            ItemsGrid.SelectedItem = item;
            ItemsGrid.ScrollIntoView(item);
            ItemsGrid.UpdateLayout();

            SaveBackToNvp();
        }

        // Разрешаем редактирование только колонки "Факт"
        private void ItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            // индекс колонок: 0 штрихкод, 1 план, 2 факт, 3 осталось, 4 перебор
            if (e.Column.DisplayIndex != 2)
            {
                e.Cancel = true;
                return;
            }

            if (e.Row?.Item is not NVPItem row)
                return;

            // берём текст из TextBox
            if (e.EditingElement is TextBox tb)
            {
                if (!int.TryParse(tb.Text.Trim(), out int val))
                    val = row.ScannedCount;

                if (val < 0) val = 0;

                // ставим новое значение
                row.ScannedCount = val;

                // помечаем как последняя изменённая строка
                foreach (var r in _rows) r.IsLastScanned = false;
                row.IsLastScanned = true;

                // пересчёт
                UpdateRowStatus(row);
                UpdateTotals();

                ItemsGrid.SelectedItem = row;
                ItemsGrid.ScrollIntoView(row);
                ItemsGrid.UpdateLayout();

                SaveBackToNvp();
            }
        }

        private void RecalcAll()
        {
            foreach (var r in _rows)
                UpdateRowStatus(r);

            UpdateTotals();
        }

        private void UpdateRowStatus(NVPItem r)
        {
            if (r.ScannedCount < r.ExpectedCount)
                r.Status = NvpRowStatus.Under;
            else if (r.ScannedCount == r.ExpectedCount)
                r.Status = NvpRowStatus.Match;
            else
                r.Status = NvpRowStatus.Over;
        }

        private void UpdateTotals()
        {
            int plan = _rows.Sum(x => x.ExpectedCount);
            int fact = _rows.Sum(x => x.ScannedCount);

            int left = _rows.Sum(x => Math.Max(0, x.ExpectedCount - x.ScannedCount));
            int over = _rows.Sum(x => Math.Max(0, x.ScannedCount - x.ExpectedCount));

            PlanText.Text = plan.ToString();
            ScannedText.Text = fact.ToString();
            LeftText.Text = left.ToString();
            OverText.Text = over.ToString();
        }

        // сохраняем обратно в _nvp и на диск
        private void SaveBackToNvp()
        {
            // копируем рабочие значения в исходный NVP
            _nvp.Items.Clear();
            foreach (var r in _rows)
            {
                _nvp.Items.Add(new NVPItem
                {
                    Barcode = r.Barcode,
                    ExpectedCount = r.ExpectedCount,
                    ScannedCount = r.ScannedCount,
                    Status = r.Status,
                    IsLastScanned = false
                });
            }

            // пишем в json
            NVPStorage.Save();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}





