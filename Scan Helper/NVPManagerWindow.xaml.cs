using System.Collections.ObjectModel;
using System.Windows;
using ScanHelper.Data;
using ScanHelper.Models;

namespace ScanHelper
{
    public partial class NVPManagerWindow : Window
    {
        private readonly ObservableCollection<NVPItem> _createItems = new ObservableCollection<NVPItem>();

        public NVPManagerWindow()
        {
            InitializeComponent();

            // ВАЖНО: при открытии менеджера — подгружаем НВП с диска
            NVPStorage.Load();

            // Привязки
            ItemsGrid.ItemsSource = _createItems;
            NVPList.ItemsSource = NVPStorage.NVPList;

            BarcodeBox.Focus();
        }

        // ====== ЛЕВОЕ МЕНЮ ======
        private void CreateNVPMenu_Click(object sender, RoutedEventArgs e)
        {
            CreatePanel.Visibility = Visibility.Visible;
            ViewAllPanel.Visibility = Visibility.Collapsed;
            BarcodeBox.Focus();
        }

        private void ViewAllMenu_Click(object sender, RoutedEventArgs e)
        {
            // обновить список (на всякий)
            NVPList.ItemsSource = null;
            NVPList.ItemsSource = NVPStorage.NVPList;

            CreatePanel.Visibility = Visibility.Collapsed;
            ViewAllPanel.Visibility = Visibility.Visible;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Открываем меню
            var menu = new MenuWindow();
            menu.Show();

            // Закрываем менеджер НВП
            Close();
        }

        // ====== СОЗДАНИЕ НВП ======
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
                MessageBox.Show("Введите корректное количество (>0)");
                return;
            }

            // если штрихкод уже есть — суммируем план
            foreach (var it in _createItems)
            {
                if (it.Barcode == barcode)
                {
                    it.ExpectedCount += expected;
                    ItemsGrid.Items.Refresh();
                    BarcodeBox.Clear();
                    CountBox.Clear();
                    BarcodeBox.Focus();
                    return;
                }
            }

            _createItems.Add(new NVPItem
            {
                Barcode = barcode,
                ExpectedCount = expected,
                ScannedCount = 0
            });

            BarcodeBox.Clear();
            CountBox.Clear();
            BarcodeBox.Focus();
        }

        private void SaveNVP_Click(object sender, RoutedEventArgs e)
        {
            string name = NVPNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название НВП");
                return;
            }

            if (_createItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну позицию");
                return;
            }

            var nvp = new NVP
            {
                Name = name,
                Items = new ObservableCollection<NVPItem>()
            };

            foreach (var it in _createItems)
            {
                nvp.Items.Add(new NVPItem
                {
                    Barcode = it.Barcode,
                    ExpectedCount = it.ExpectedCount,
                    ScannedCount = 0
                });
            }

            NVPStorage.NVPList.Add(nvp);

            // сохраняем на диск
            NVPStorage.Save();

            MessageBox.Show("НВП сохранено!");
            Clear_Click(null, null);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _createItems.Clear();
            NVPNameBox.Clear();
            BarcodeBox.Clear();
            CountBox.Clear();
            BarcodeBox.Focus();
        }

        // ====== ВСЕ НВП ======
        private void OpenSelected_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP selected)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            // Открыть окно сканирования НВП
            // ВАЖНО: ниже конструктор должен быть ScanNVPWindow(NVP nvp)
            var scan = new ScanNVPWindow(selected);
            scan.ShowDialog();

            // после сканирования сохраняем факт
            NVPStorage.Save();
        }

        private void EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP selected)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            // ВАЖНО: CreateNVPWindow должен иметь конструктор CreateNVPWindow(NVP nvpToEdit)
            var wnd = new CreateNVPWindow(selected);
            wnd.ShowDialog();

            NVPStorage.Save();

            // обновить список
            NVPList.ItemsSource = null;
            NVPList.ItemsSource = NVPStorage.NVPList;
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP selected)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            if (MessageBox.Show($"Удалить НВП \"{selected.Name}\"?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            NVPStorage.NVPList.Remove(selected);
            NVPStorage.Save();
        }
    }
}
