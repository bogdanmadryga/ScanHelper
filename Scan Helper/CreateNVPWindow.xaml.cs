using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ScanHelper.Data;
using ScanHelper.Models;

namespace ScanHelper
{
    public partial class CreateNVPWindow : Window
    {
        private readonly ObservableCollection<NVPItem> _items = new ObservableCollection<NVPItem>();
        private readonly NVP? _editingNvp; // null => создание

        // СОЗДАНИЕ
        public CreateNVPWindow()
        {
            InitializeComponent();
            ItemsGrid.ItemsSource = _items;
            BarcodeBox.Focus();
        }

        // РЕДАКТИРОВАНИЕ
        public CreateNVPWindow(NVP nvpToEdit) : this()
        {
            _editingNvp = nvpToEdit;

            NVPNameBox.Text = _editingNvp.Name;

            _items.Clear();
            foreach (var it in _editingNvp.Items)
            {
                _items.Add(new NVPItem
                {
                    Barcode = it.Barcode,
                    ExpectedCount = it.ExpectedCount,
                    ScannedCount = it.ScannedCount
                });
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            string barcode = BarcodeBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                MessageBox.Show("Введите штрихкод");
                return;
            }

            if (!int.TryParse(CountBox.Text.Trim(), out int expected) || expected <= 0)
            {
                MessageBox.Show("Введите корректный план (число > 0)");
                return;
            }

            // Если штрихкод уже есть — увеличиваем план
            var existing = _items.FirstOrDefault(x => x.Barcode == barcode);
            if (existing != null)
            {
                existing.ExpectedCount += expected;
            }
            else
            {
                _items.Add(new NVPItem
                {
                    Barcode = barcode,
                    ExpectedCount = expected,
                    ScannedCount = 0
                });
            }

            BarcodeBox.Clear();
            CountBox.Clear();
            BarcodeBox.Focus();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is not NVPItem selected)
            {
                MessageBox.Show("Выберите строку для удаления");
                return;
            }

            _items.Remove(selected);
        }

        private void SaveNVP_Click(object sender, RoutedEventArgs e)
        {
            string name = NVPNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название НВП");
                return;
            }

            if (_items.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну позицию");
                return;
            }

            // Чистим мусор/нули
            var itemsToSave = _items
                .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
                .Select(x => new NVPItem
                {
                    Barcode = x.Barcode.Trim(),
                    ExpectedCount = x.ExpectedCount < 0 ? 0 : x.ExpectedCount,
                    ScannedCount = x.ScannedCount < 0 ? 0 : x.ScannedCount
                })
                .ToList();

            if (itemsToSave.Count == 0)
            {
                MessageBox.Show("Нет корректных позиций для сохранения");
                return;
            }

            if (_editingNvp == null)
            {
                var nvp = new NVP
                {
                    Name = name,
                    Items = new ObservableCollection<NVPItem>()
                };

                foreach (var it in itemsToSave)
                    nvp.Items.Add(it);

                NVPStorage.NVPList.Add(nvp);
            }
            else
            {
                _editingNvp.Name = name;
                _editingNvp.Items.Clear();
                foreach (var it in itemsToSave)
                    _editingNvp.Items.Add(it);
            }

            NVPStorage.Save();
            MessageBox.Show("Сохранено");
            Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
            NVPNameBox.Clear();
            BarcodeBox.Clear();
            CountBox.Clear();
            BarcodeBox.Focus();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Close();
    }
}







