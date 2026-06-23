using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace ScanHelper
{
    public partial class AutoPrintWindow : Window
    {
        private List<SealItem> _items;

        private POINT _searchPoint;
        private POINT _printerPoint;
        private POINT _printSinglePoint;
        private POINT _productPoint;
        private POINT _printPoint;
        private POINT _closePoint;

        private string _captureMode = "";
        private const string SettingsFile = "autoprint_settings.txt";
        private bool _settingsVisible = true;
        private bool _stopRequested = false;

        public AutoPrintWindow(List<SealItem> items)
        {
            InitializeComponent();

            _items = items;

            LoadSettings();

            LogBox.Items.Add(
                $"Штрихкодів: {items.Count}");
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern void mouse_event(
            uint dwFlags,
            uint dx,
            uint dy,
            uint dwData,
            UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(
            int X,
            int Y);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public struct POINT
        {
            public int X;
            public int Y;
        }

        private void BtnSearch_Click(
            object sender,
            RoutedEventArgs e)
        {
            _captureMode = "search";

            LogBox.Items.Add(
                "Наведи мишку на ПОШУК і натисни F8");
        }

        private void BtnPrinter_Click(
            object sender,
            RoutedEventArgs e)
        {
            _captureMode = "printer";

            LogBox.Items.Add(
                "Наведи мишку на ПРИНТЕР і натисни F8");
        }

        private void BtnPrintSingle_Click(
            object sender,
            RoutedEventArgs e)
        {
            _captureMode = "single";

            LogBox.Items.Add(
                "Наведи мишку на ДРУК ПОШТУЧНО і натисни F8");
        }

        protected override void OnKeyDown(
    System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.F8)
                return;

            GetCursorPos(out POINT p);

            switch (_captureMode)
            {
                case "search":
                    _searchPoint = p;
                    LogBox.Items.Add(
                        $"Пошук: {p.X} {p.Y}");
                    break;

                case "printer":
                    _printerPoint = p;
                    LogBox.Items.Add(
                        $"Принтер: {p.X} {p.Y}");
                    break;

                case "single":
                    _printSinglePoint = p;
                    LogBox.Items.Add(
                        $"Поштучно: {p.X} {p.Y}");
                    break;
                case "product":
                    _productPoint = p;
                    LogBox.Items.Add(
                        $"Товар: {p.X} {p.Y}");
                    break;

                case "print":
                    _printPoint = p;
                    LogBox.Items.Add(
                        $"Друк: {p.X} {p.Y}");
                    break;

                case "close":
                    _closePoint = p;
                    LogBox.Items.Add(
                        $"Закрити: {p.X} {p.Y}");
                    break;
            }

            _captureMode = "";
        }

        private async void BtnTest_Click(
    object sender,
    RoutedEventArgs e)
        {
            await Task.Delay(250);

            // Поиск
            ClickPoint(_searchPoint);

            await Task.Delay(250);

            ClickPoint(_searchPoint);

            await Task.Delay(250);

            System.Windows.Forms.SendKeys.SendWait("2916540316212");

            await Task.Delay(250);

            // Принтер
            ClickPoint(_printerPoint);

            await Task.Delay(250);

            // Друк поштучно
            ClickPoint(_printSinglePoint);

            await Task.Delay(250);

            // Кнопка Друк
            ClickPoint(_printPoint);

            await Task.Delay(250);

            // Закрыть окно
            ClickPoint(_closePoint);

            LogBox.Items.Add(
                "Тест друку завершено");
        }

        private void ClickPoint(
            POINT point)
        {
            SetCursorPos(
                point.X,
                point.Y);

            mouse_event(
                MOUSEEVENTF_LEFTDOWN,
                0,
                0,
                0,
                UIntPtr.Zero);

            mouse_event(
                MOUSEEVENTF_LEFTUP,
                0,
                0,
                0,
                UIntPtr.Zero);
        }
        private void DoubleClickPoint(
    POINT point)
        {
            ClickPoint(point);

            Thread.Sleep(100);

            ClickPoint(point);
        }
        private void BtnProduct_Click(
    object sender,
    RoutedEventArgs e)
        {
            _captureMode = "product";

            LogBox.Items.Add(
                "Наведи мишку на ТОВАР і натисни F8");
        }

        private void BtnPrint_Click(
            object sender,
            RoutedEventArgs e)
        {
            _captureMode = "print";

            LogBox.Items.Add(
                "Наведи мишку на кнопку ДРУК і натисни F8");
        }

        private void BtnClose_Click(
            object sender,
            RoutedEventArgs e)
        {
            _captureMode = "close";

            LogBox.Items.Add(
                "Наведи мишку на кнопку ЗАКРИТИ і натисни F8");
        }
        private async Task PrintBarcode(
    string barcode)
        {
            DoubleClickPoint(_searchPoint);

            await Task.Delay(500);

            System.Windows.Forms.SendKeys.SendWait(barcode);

            await Task.Delay(1500);

            ClickPoint(_printerPoint);

            await Task.Delay(800);

            ClickPoint(_printSinglePoint);

            await Task.Delay(1500);

            ClickPoint(_printPoint);

            await Task.Delay(5000);

            ClickPoint(_closePoint);

            await Task.Delay(1000);
        }

        private async void BtnStart_Click(
    object sender,
    RoutedEventArgs e)
        {
            _stopRequested = false;

            var barcodes = _items
                .Select(x => x.Barcode)
                .Distinct()
                .ToList();

            int total = barcodes.Count;
            int printed = 0;

            ProgressText.Text = $"0 / {total}";

            LogBox.Items.Add(
                $"Запуск друку: {total}");

            await Task.Delay(3000);

            foreach (string barcode in barcodes)
            {
                if (_stopRequested)
                {
                    LogBox.Items.Add(
                        "⛔ Друк зупинено");

                    ProgressText.Text = "0 / 0";

                    return;
                }

                LogBox.Items.Add(
                    $"Друк: {barcode}");

                await PrintBarcode(barcode);

                printed++;

                ProgressText.Text =
                    $"{printed} / {total}";
            }

            LogBox.Items.Add("Готово");

            ProgressText.Text =
                $"✅ {total} / {total}";
        }
        private void SaveSettings()
        {
            File.WriteAllLines(
                SettingsFile,
                new[]
                {
            $"{_searchPoint.X};{_searchPoint.Y}",
            $"{_printerPoint.X};{_printerPoint.Y}",
            $"{_printSinglePoint.X};{_printSinglePoint.Y}",
            $"{_printPoint.X};{_printPoint.Y}",
            $"{_closePoint.X};{_closePoint.Y}"
                });
        }
        private void LoadSettings()
        {
            if (!File.Exists(SettingsFile))
                return;

            var lines = File.ReadAllLines(SettingsFile);

            if (lines.Length < 5)
                return;

            _searchPoint = ParsePoint(lines[0]);
            _printerPoint = ParsePoint(lines[1]);
            _printSinglePoint = ParsePoint(lines[2]);
            _printPoint = ParsePoint(lines[3]);
            _closePoint = ParsePoint(lines[4]);

            LogBox.Items.Add("Налаштування завантажено");
        }
        private POINT ParsePoint(string line)
        {
            var parts = line.Split(';');

            return new POINT
            {
                X = int.Parse(parts[0]),
                Y = int.Parse(parts[1])
            };
        }
        private void BtnSave_Click(
    object sender,
    RoutedEventArgs e)
        {
            SaveSettings();

            LogBox.Items.Add(
                "Налаштування збережено");
        }
        private void BtnToggleSettings_Click(
    object sender,
    RoutedEventArgs e)
        {
            _settingsVisible = !_settingsVisible;

            SettingsPanel.Visibility =
                _settingsVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            BtnToggleSettings.Content =
                _settingsVisible
                ? "⚙ Налаштування ▼"
                : "⚙ Налаштування ►";
        }
        private void BtnReset_Click(
    object sender,
    RoutedEventArgs e)
        {
            // Остановить цикл
            _stopRequested = true;

            // Сбросить координаты
            _searchPoint = new POINT();
            _printerPoint = new POINT();
            _printSinglePoint = new POINT();
            _productPoint = new POINT();
            _printPoint = new POINT();
            _closePoint = new POINT();

            // Удалить файл настроек
            if (File.Exists(SettingsFile))
                File.Delete(SettingsFile);

            // Очистить лог
            LogBox.Items.Clear();

            ProgressText.Text = "0 / 0";

            LogBox.Items.Add(
                "Налаштування скинуто");

            LogBox.Items.Add(
                "Друк зупинено");

            LogBox.Items.Add(
                "Запиши координати заново через F8");
        }
    }
}