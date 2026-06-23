using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScanHelper
{
    public partial class PrintLabelsWindow : Window
    {
        private List<LabelItem> _database = new();
        private List<LabelItem> _scannedItems = new();
        private List<LabelItem> _lastPrintedItems = new();
        private bool _lastBrandPrint = false;

        public PrintLabelsWindow()
        {
            InitializeComponent();
        }

        // НАЗАД
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new MenuWindow().Show();
            Close();
        }

        // ЗАГРУЗКА EXCEL
        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                _database = LabelExcelReader.Read(dlg.FileName);

                FilePathText.Text = dlg.FileName;
                InfoText.Text = $"Завантажено: {_database.Count} товарів";

                BarcodeBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // СКАН ШТРИХКОДА
        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            var barcode = BarcodeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(barcode))
                return;

            var item = _database.FirstOrDefault(x => x.Barcode == barcode);

            if (item == null)
            {
                SystemSounds.Hand.Play();

                MessageBox.Show(
                    $"Штрихкод не знайдено:\n{barcode}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BarcodeBox.Clear();
                BarcodeBox.Focus();
                return;
            }

            _scannedItems.Add(new LabelItem
            {
                Barcode = item.Barcode,
                Article = TrimArticle(item.Article),
                Size = item.Size
            });

            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = _scannedItems;

            CountText.Text = $"У черзі: {_scannedItems.Count}";

            BarcodeBox.Clear();
            BarcodeBox.Focus();
        }

        // УДАЛИТЬ ПОСЛЕДНИЙ
        private void RemoveLast_Click(object sender, RoutedEventArgs e)
        {
            if (_scannedItems.Count == 0)
                return;

            _scannedItems.RemoveAt(_scannedItems.Count - 1);

            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = _scannedItems;

            CountText.Text = $"У черзі: {_scannedItems.Count}";
        }

        // ОЧИСТИТЬ
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _scannedItems.Clear();

            ResultGrid.ItemsSource = null;

            CountText.Text = "У черзі: 0";
        }

        // ОБЫЧНАЯ ПЕЧАТЬ
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_scannedItems.Count == 0)
            {
                MessageBox.Show("Немає етикеток для друку");
                return;
            }

            var pd = new PrintDialog();

            if (pd.ShowDialog() != true)
                return;
            _lastPrintedItems =
    _scannedItems
    .Select(x => new LabelItem
    {
        Barcode = x.Barcode,
        Article = x.Article,
        Size = x.Size
    })
    .ToList();

            _lastBrandPrint = false;

            foreach (var item in _scannedItems)
            {
                PrintLabel(pd, item);
            }

            MessageBox.Show(
                $"Надруковано етикеток: {_scannedItems.Count}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _scannedItems.Clear();

            ResultGrid.ItemsSource = null;

            CountText.Text = "У черзі: 0";

            BarcodeBox.Focus();
        }

        // ПЕЧАТЬ С ЛОГО
        private void PrintBrand_Click(object sender, RoutedEventArgs e)
        {
            if (_scannedItems.Count == 0)
            {
                MessageBox.Show("Немає етикеток для друку");
                return;
            }

            var pd = new PrintDialog();

            if (pd.ShowDialog() != true)
                return;
            _lastPrintedItems =
    _scannedItems
    .Select(x => new LabelItem
    {
        Barcode = x.Barcode,
        Article = x.Article,
        Size = x.Size
    })
    .ToList();

            _lastBrandPrint = true;

            foreach (var item in _scannedItems)
            {
                PrintBrandLabel(pd, item);
            }

            MessageBox.Show(
                $"Надруковано етикеток: {_scannedItems.Count}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _scannedItems.Clear();

            ResultGrid.ItemsSource = null;

            CountText.Text = "У черзі: 0";

            BarcodeBox.Focus();
        }

        // ОБЫЧНАЯ ЭТИКЕТКА
        private void PrintLabel(PrintDialog pd, LabelItem item)
        {
            var article = TrimArticle(item.Article);

            double articleFontSize = GetArticleFontSize(article.Length);

            var stack = new StackPanel
            {
                Width = 210,
                Height = 130,
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var articleText = new TextBlock
            {
                Text = article,
                FontSize = articleFontSize,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 18, 0, 8)
            };

            var sizeText = new TextBlock
            {
                Text = item.Size,
                FontSize = 48,
                FontWeight = FontWeights.Black,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 4, 0, 0)
            };

            stack.Children.Add(articleText);
            stack.Children.Add(sizeText);

            var border = new Border
            {
                Width = 210,
                Height = 130,
                Background = Brushes.White,
                Child = stack
            };

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            border.Arrange(new Rect(new Point(0, 0), border.DesiredSize));

            pd.PrintVisual(border, "Label");
        }

        // ЭТИКЕТКА С ЛОГО
        private void PrintBrandLabel(PrintDialog pd, LabelItem item)
        {
            var article = TrimArticle(item.Article);

            double articleFontSize =
                GetArticleFontSize(article.Length);

            var root = new Grid
            {
                Width = 210,
                Height = 130,
                Background = Brushes.White
            };

            // ЛОГО (сначала рисуем его)
            var logo = new Image
            {
                Width = 55,
                Height = 55,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,

                // максимально вправо и вниз
                Margin = new Thickness(0, 0, 0, 0),

                // немного прозрачный
                Opacity = 0.9
            };

            logo.Source = new BitmapImage(
                new Uri("pack://application:,,,/Assets/бажане.jpg"));

            root.Children.Add(logo);

            // ТЕКСТ
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var articleText = new TextBlock
            {
                Text = article,
                FontSize = articleFontSize,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var sizeText = new TextBlock
            {
                Text = item.Size,
                FontSize = 48,
                FontWeight = FontWeights.Black,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 4, 0, 0)
            };

            stack.Children.Add(articleText);
            stack.Children.Add(sizeText);

            // текст рисуется поверх логотипа
            root.Children.Add(stack);

            root.Measure(
                new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity));

            root.Arrange(
                new Rect(
                    new Point(0, 0),
                    root.DesiredSize));

            pd.PrintVisual(root, "Brand Label");
        }

        // АВТО РАЗМЕР ШРИФТА
        private double GetArticleFontSize(int length)
        {
            if (length <= 8) return 42;

            if (length <= 10) return 38;

            if (length <= 12) return 34;

            if (length <= 14) return 30;

            return 26;
        }

        // ОБРЕЗКА АРТИКУЛА
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
        
        public void ReceiveBarcode(string barcode)
        {
            var item = _database.FirstOrDefault(x => x.Barcode == barcode);

            if (item == null)
            {
                SystemSounds.Hand.Play();

                MessageBox.Show(
                    $"Штрихкод не знайдено:\n{barcode}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _scannedItems.Add(new LabelItem
            {
                Barcode = item.Barcode,
                Article = TrimArticle(item.Article),
                Size = item.Size
            });

            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = _scannedItems;

            CountText.Text = $"У черзі: {_scannedItems.Count}";
        }

        private void RepeatLast_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_lastPrintedItems.Count == 0)
            {
                MessageBox.Show(
                    "Ще нічого не друкувалось");

                return;
            }

            var pd = new PrintDialog();

            if (pd.ShowDialog() != true)
                return;

            foreach (var item in _lastPrintedItems)
            {
                if (_lastBrandPrint)
                    PrintBrandLabel(pd, item);
                else
                    PrintLabel(pd, item);
            }

            MessageBox.Show(
                $"Повторно надруковано: {_lastPrintedItems.Count}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}