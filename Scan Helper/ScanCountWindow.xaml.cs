using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ScanHelper
{
    public partial class ScanCountWindow : Window
    {
        private readonly ObservableCollection<BarcodeRow> _rows = new();

        public ScanCountWindow()
        {
            InitializeComponent();

            BarcodeGrid.ItemsSource = _rows;

            Loaded += (_, __) =>
            {
                BarcodeInput.Focus();
                BarcodeInput.SelectAll();
                UpdateTotal();
            };
        }

        // ENTER в поле сканера
        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var code = (BarcodeInput.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
                return;

            AddOrInc(code);

            BarcodeInput.Clear();
            BarcodeInput.Focus();
            e.Handled = true;
        }

        private void AddOrInc(string code)
        {
            var row = _rows.FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                row = new BarcodeRow { Code = code, Quantity = 1 };
                _rows.Add(row);
                AddLog($"+ {code} (1)");
            }
            else
            {
                row.Quantity += 1;
                AddLog($"+ {code} ({row.Quantity})");
            }

            BarcodeGrid.SelectedItem = row;
            BarcodeGrid.ScrollIntoView(row);

            UpdateTotal();
        }

        private void AddLog(string text)
        {
            var line = $"{DateTime.Now:HH:mm:ss}  {text}";
            LogList.Items.Insert(0, line);
        }

        private void UpdateTotal()
        {
            var total = _rows.Sum(r => r.Quantity);
            TotalCountText.Text = $"Общее количество: {total}";
        }

        // Обнулить
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0) return;

            _rows.Clear();
            LogList.Items.Clear();
            UpdateTotal();

            BarcodeInput.Focus();
            AddLog("СБРОС (обнулено)");
        }

        // Назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся в меню (без закрытия всего приложения)
            if (Owner != null) Owner.Show();
            else new MenuWindow().Show();

            Close();
        }
    }

    // Строка таблицы
    public class BarcodeRow : INotifyPropertyChanged
    {
        private string _code = "";
        private int _quantity;

        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                // защита от минусов и мусора
                if (value < 0) value = 0;
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
