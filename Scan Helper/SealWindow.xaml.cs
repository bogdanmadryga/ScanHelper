using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScanHelper
{
    public partial class SealWindow : Window
    {
        private List<SealItem> _sealItems = new();

        private List<StockPrintItem> _database = new();

        public SealWindow()
        {
            InitializeComponent();

            ItemsGrid.ItemsSource = _sealItems;
        }

        // =====================================================
        // НАЗАД
        // =====================================================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // =====================================================
        // ЗАГРУЗКА EXCEL
        // =====================================================
        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Excel (*.xlsx)|*.xlsx";

            if (dialog.ShowDialog() != true)
                return;

            _database = StockPrintExcelReader.Read(dialog.FileName);

            MessageBox.Show(
                $"Завантажено {_database.Count} рядків",
                "Готово");

            BarcodeInput.Focus();
        }

        // =====================================================
        // СКАНИРОВАНИЕ
        // =====================================================
        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string barcode = BarcodeInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(barcode))
                return;

            if (_database.Count == 0)
            {
                MessageBox.Show(
                    "Спочатку завантажте Excel базу");

                BarcodeInput.Clear();
                return;
            }

            var product = _database
                .FirstOrDefault(x => x.Barcode == barcode);

            if (product == null)
            {
                MessageBox.Show(
                    $"Штрихкод не знайдено:\n{barcode}");

                BarcodeInput.Clear();
                return;
            }

            string article =
                TrimArticle(product.Article);

            var existing = _sealItems
                .FirstOrDefault(x =>
                    x.Article == article &&
                    x.Size == product.Size);

            if (existing == null)
            {
                _sealItems.Add(new SealItem
                {
                    Barcode = product.Barcode,
                    Article = article,
                    Size = product.Size,
                    Qty = 1
                });
            }
            else
            {
                existing.Qty++;
            }

            ItemsGrid.Items.Refresh();

            LogList.Items.Insert(
                0,
                $"+ {barcode}");

            UpdateTotalCount();

            BarcodeInput.Clear();
            BarcodeInput.Focus();
        }

        // =====================================================
        // ПЕРЕСЧЕТ ОБЩЕГО КОЛИЧЕСТВА
        // =====================================================
        private void UpdateTotalCount()
        {
            int total =
                _sealItems.Sum(x => x.Qty);

            TotalCountText.Text =
                $"Загальна кількість: {total}";
        }

        // =====================================================
        // ЕСЛИ МЕНЯЮТ КОЛИЧЕСТВО ВРУЧНУЮ
        // =====================================================
        private void ItemsGrid_CellEditEnding(
            object sender,
            DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateTotalCount();
            }));
        }

        // =====================================================
        // ОЧИСТИТИ
        // =====================================================
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Очистити всі дані?",
                "Підтвердження",
                MessageBoxButton.YesNo)
                != MessageBoxResult.Yes)
                return;

            _sealItems.Clear();

            ItemsGrid.Items.Refresh();

            LogList.Items.Clear();

            TotalCountText.Text =
                "Загальна кількість: 0";

            BarcodeInput.Focus();
        }

        // =====================================================
        // ПЛОМБУВАТИ
        // =====================================================
        private void Seal_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (_sealItems.Count == 0)
            {
                MessageBox.Show(
                    "Немає даних для друку");

                return;
            }

            try
            {
                var window =
    new PrintOptionsWindow(
        _sealItems,
        this);

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message);
            }
        }

        // =====================================================
        // ОБРЕЗКА АРТИКУЛА
        // =====================================================
        private string TrimArticle(string article)
        {
            if (string.IsNullOrWhiteSpace(article))
                return "";

            for (int i = 0; i < article.Length; i++)
            {
                if (char.IsDigit(article[i]))
                    return article.Substring(i);
            }

            return article;
        }
        private void AutoPrint_Click(
     object sender,
     RoutedEventArgs e)
        {
            new AutoPrintWindow(_sealItems)
                .ShowDialog();
        }
        public void ReceiveBarcode(string barcode)
        {
            if (_database.Count == 0)
                return;

            var product = _database
                .FirstOrDefault(x => x.Barcode == barcode);

            if (product == null)
                return;

            string article =
                TrimArticle(product.Article);

            var existing = _sealItems
                .FirstOrDefault(x =>
                    x.Article == article &&
                    x.Size == product.Size);

            if (existing == null)
            {
                _sealItems.Add(new SealItem
                {
                    Barcode = product.Barcode,
                    Article = article,
                    Size = product.Size,
                    Qty = 1
                });
            }
            else
            {
                existing.Qty++;
            }

            ItemsGrid.Items.Refresh();

            LogList.Items.Insert(
                0,
                $"+ {barcode}");

            UpdateTotalCount();
        }
    }
}