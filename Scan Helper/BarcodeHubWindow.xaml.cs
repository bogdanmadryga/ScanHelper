using DocumentFormat.OpenXml.Wordprocessing;
using ScanHelper;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScanHelper
{
    public partial class BarcodeHubWindow : Window
    {
        private IntPtr _window1 = IntPtr.Zero;
        private IntPtr _window2 = IntPtr.Zero;

        public BarcodeHubWindow()
        {
            InitializeComponent();

            Topmost = true;

            Loaded += (s, e) =>
            {
                BarcodeBox.Focus();
            };

            Activated += (s, e) =>
            {
                BarcodeBox.Focus();
            };
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private async void BtnWindow1_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text =
                "Через 3 секунди вибери вікно 1";

            await Task.Delay(3000);

            _window1 = GetForegroundWindow();

            StatusText.Text =
                "Вікно 1 збережено";

            Activate();
            BarcodeBox.Focus();
        }

        private async void BtnWindow2_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text =
                "Через 3 секунди вибери вікно 2";

            await Task.Delay(3000);

            _window2 = GetForegroundWindow();

            StatusText.Text =
                "Вікно 2 збережено";

            Activate();
            BarcodeBox.Focus();
        }

        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string barcode = BarcodeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(barcode))
                return;

            // Отправка напрямую в окно печати
            foreach (Window window in Application.Current.Windows)
            {
                if (window is PrintLabelsWindow printWindow)
                {
                    printWindow.ReceiveBarcode(barcode);
                }
            }

            // Отправка в ТоргСофт
            SendToWindow(_window2, barcode);

            BarcodeBox.Clear();

            StatusText.Text =
                $"Надіслано: {barcode}";

            BarcodeBox.Focus();

            e.Handled = true;
        }

        private void SendToWindow(
    IntPtr hwnd,
    string barcode)
        {
            if (hwnd == IntPtr.Zero)
                return;

            SetForegroundWindow(hwnd);

            Thread.Sleep(20);

            System.Windows.Forms.SendKeys.SendWait(barcode);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

            Activate();
            BarcodeBox.Focus();
            Keyboard.Focus(BarcodeBox);
            Activate();
            BarcodeBox.Focus();
            Keyboard.Focus(BarcodeBox);
        }

    }
}