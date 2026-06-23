using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScanHelper
{
    public partial class StockPrintWindow : Window
    {
        private List<StockPrintItem> _items = new();

        public StockPrintWindow()
        {
            InitializeComponent();
        }

        // =====================================================
        // НАЗАД
        // =====================================================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new MenuWindow().Show();
            Close();
        }

        // =====================================================
        // ЗАГРУЗКА EXCEL
        // =====================================================
        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true)
                return;

            _items = StockPrintExcelReader.Read(dlg.FileName);

            InfoText.Text = $"Завантажено: {_items.Count} рядків";

            ArticlesGrid.ItemsSource = null;
            SizesGrid.ItemsSource = null;
        }

        // =====================================================
        // ПОИСК
        // =====================================================
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_items.Count == 0)
                return;

            var text = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(text))
            {
                ArticlesGrid.ItemsSource = null;
                SizesGrid.ItemsSource = null;
                return;
            }

            var mode = ((ComboBoxItem)SearchModeBox.SelectedItem)
                .Content
                .ToString();

            List<ArticleViewItem> results = new();

            // =====================================================
            // ПОИСК ПО АРТИКУЛУ
            // =====================================================
            if (mode == "Артикул")
            {
                results = _items
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Article) &&
                        x.Article.ToLower().Contains(text) &&
                        x.Qty > 0)
                    .GroupBy(x => x.Article)
                    .Select(g => new ArticleViewItem
                    {
                        Article = g.Key,
                        ProductName = g.First().ProductName
                    })
                    .OrderBy(x => x.Article)
                    .Take(100)
                    .ToList();
            }

            // =====================================================
            // ПОИСК ПО ШТРИХКОДУ
            // =====================================================
            else if (mode == "Штрихкод")
            {
                results = _items
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Barcode) &&
                        x.Barcode.Contains(text) &&
                        x.Qty > 0)
                    .Select(x => new ArticleViewItem
                    {
                        Barcode = x.Barcode,
                        Article = x.Article,
                        ProductName = x.ProductName,
                        Size = x.Size,
                        Qty = x.Qty
                    })
                    .GroupBy(x => $"{x.Article}_{x.Size}")
                    .Select(g => g.First())
                    .OrderBy(x => x.Article)
                    .Take(100)
                    .ToList();
            }

            // =====================================================
            // ПОИСК ПО НАЗВАНИЮ
            // =====================================================
            else
            {
                results = _items
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.ProductName) &&
                        x.ProductName.ToLower().Contains(text) &&
                        x.Qty > 0)
                    .GroupBy(x => x.Article)
                    .Select(g => new ArticleViewItem
                    {
                        Article = g.Key,
                        ProductName = g.First().ProductName
                    })
                    .OrderBy(x => x.Article)
                    .Take(100)
                    .ToList();
            }

            ArticlesGrid.ItemsSource = results;

            if (results.Count == 1)
            {
                ArticlesGrid.SelectedIndex = 0;
            }
        }

        // =====================================================
        // ВЫБОР ТОВАРА
        // =====================================================
        private void ArticlesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArticlesGrid.SelectedItem == null)
                return;

            var selected = (ArticleViewItem)ArticlesGrid.SelectedItem;

            var mode = ((ComboBoxItem)SearchModeBox.SelectedItem)
                .Content
                .ToString();

            // =====================================================
            // ШТРИХКОД
            // =====================================================
            if (mode == "Штрихкод")
            {
                var oneSize = new List<StockSizeItem>
                {
                    new StockSizeItem
                    {
                        IsSelected = true,
                        Size = selected.Size,
                        Qty = selected.Qty
                    }
                };

                SizesGrid.ItemsSource = oneSize;
            }

            // =====================================================
            // АРТИКУЛ / НАЗВАНИЕ
            // =====================================================
            else
            {
                var sizes = _items
                    .Where(x => x.Article == selected.Article)
                    .Where(x => x.Qty > 0)
                    .GroupBy(x => x.Size)
                    .Select(g => new StockSizeItem
                    {
                        IsSelected = true,
                        Size = g.Key,
                        Qty = g.Sum(x => x.Qty)
                    })
                    .OrderBy(x => SortSize(x.Size))
                    .ToList();

                SizesGrid.ItemsSource = sizes;
            }
        }

        // =====================================================
        // СОРТИРОВКА РАЗМЕРОВ
        // =====================================================
        private int SortSize(string size)
        {
            var order = new List<string>
            {
                "XXS",
                "XS",
                "XS/S",
                "S",
                "S/M",
                "M",
                "M/L",
                "L",
                "L/XL",
                "XL",
                "XXL"
            };

            var index = order.IndexOf(size.ToUpper());

            if (index >= 0)
                return index;

            return 999;
        }

        // =====================================================
        // ДРУК
        // =====================================================
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (SizesGrid.ItemsSource == null)
                return;

            var sizes = SizesGrid.ItemsSource as List<StockSizeItem>;

            if (sizes == null)
                return;

            var selectedArticle =
                ArticlesGrid.SelectedItem as ArticleViewItem;

            if (selectedArticle == null)
                return;

            var pd = new PrintDialog();

            if (pd.ShowDialog() != true)
                return;

            int printed = 0;

            foreach (var size in sizes.Where(x => x.IsSelected))
            {
                for (int i = 0; i < size.Qty; i++)
                {
                    PrintLabel(
                        pd,
                        selectedArticle.Article,
                        size.Size);

                    printed++;
                }
            }

            MessageBox.Show(
                $"Надруковано: {printed}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =====================================================
        // ДРУК ТОВ
        // =====================================================
        private void PrintBrand_Click(object sender, RoutedEventArgs e)
        {
            if (SizesGrid.ItemsSource == null)
                return;

            var sizes = SizesGrid.ItemsSource as List<StockSizeItem>;

            if (sizes == null)
                return;

            var selectedArticle =
                ArticlesGrid.SelectedItem as ArticleViewItem;

            if (selectedArticle == null)
                return;

            var pd = new PrintDialog();

            if (pd.ShowDialog() != true)
                return;

            int printed = 0;

            foreach (var size in sizes.Where(x => x.IsSelected))
            {
                for (int i = 0; i < size.Qty; i++)
                {
                    PrintBrandLabel(
                        pd,
                        selectedArticle.Article,
                        size.Size);

                    printed++;
                }
            }

            MessageBox.Show(
                $"Надруковано: {printed}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =====================================================
        // ОБЫЧНАЯ ЭТИКЕТКА
        // =====================================================
        private void PrintLabel(
            PrintDialog pd,
            string article,
            string size)
        {
            article = TrimArticle(article);

            double articleFontSize =
                GetArticleFontSize(article.Length);

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
                Text = size,
                FontSize = 48,
                FontWeight = FontWeights.Black,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 6, 0, 0)
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

        // =====================================================
        // ЭТИКЕТКА С ЛОГО
        // =====================================================
        private void PrintBrandLabel(
            PrintDialog pd,
            string article,
            string size)
        {
            article = TrimArticle(article);

            double articleFontSize =
                GetArticleFontSize(article.Length);

            var root = new Grid
            {
                Width = 210,
                Height = 130,
                Background = Brushes.White
            };

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
                Text = size,
                FontSize = 48,
                FontWeight = FontWeights.Black,
                FontFamily = new FontFamily("Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black
            };

            stack.Children.Add(articleText);
            stack.Children.Add(sizeText);

            root.Children.Add(stack);

            var logo = new Image
            {
                Width = 36,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 8, 6)
            };

            logo.Source = new BitmapImage(
                new Uri("pack://application:,,,/Assets/бажане.jpg"));

            root.Children.Add(logo);

            root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            root.Arrange(new Rect(new Point(0, 0), root.DesiredSize));

            pd.PrintVisual(root, "Brand Label");
        }

        // =====================================================
        // РАЗМЕР ШРИФТА
        // =====================================================
        private double GetArticleFontSize(int length)
        {
            if (length <= 8) return 42;

            if (length <= 10) return 38;

            if (length <= 12) return 34;

            if (length <= 14) return 30;

            return 26;
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
    }

    // =====================================================
    // СПИСОК ТОВАРОВ
    // =====================================================
    public class ArticleViewItem
    {
        public string Barcode { get; set; } = "";

        public string Article { get; set; } = "";

        public string ProductName { get; set; } = "";

        public string Size { get; set; } = "";

        public int Qty { get; set; }

        public override string ToString()
        {
            return $"{Article} | {ProductName}";
        }
    }

    // =====================================================
    // РАЗМЕРЫ
    // =====================================================
    public class StockSizeItem
    {
        public bool IsSelected { get; set; }

        public string Size { get; set; } = "";

        public int Qty { get; set; }
    }
}