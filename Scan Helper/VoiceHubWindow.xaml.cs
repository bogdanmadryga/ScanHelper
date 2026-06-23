using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace ScanHelper
{
    public partial class VoiceHubWindow : Window
    {
        private IntPtr _siteWindow = IntPtr.Zero;
        private IntPtr _tsWindow = IntPtr.Zero;

        public VoiceHubWindow()
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

        private async void BtnSite_Click(
            object sender,
            RoutedEventArgs e)
        {
            StatusText.Text =
                "Через 3 секунди вибери сайт";

            await Task.Delay(3000);

            _siteWindow = GetForegroundWindow();

            StatusText.Text =
                "Сайт збережено";

            Activate();
            BarcodeBox.Focus();
        }

        private async void BtnTS_Click(
            object sender,
            RoutedEventArgs e)
        {
            StatusText.Text =
                "Через 3 секунди вибери TS";

            await Task.Delay(3000);

            _tsWindow = GetForegroundWindow();

            StatusText.Text =
                "TS збережено";

            Activate();
            BarcodeBox.Focus();
        }

        private async void BarcodeBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string barcode =
                BarcodeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(barcode))
                return;

            BarcodeBox.Clear();

            // Сайт
            await SendToWindow(
                _siteWindow,
                barcode);

            await Task.Delay(30);

            // ТоргСофт
            await SendToWindow(
                _tsWindow,
                barcode);

            StatusText.Text =
                $"Надіслано: {barcode}";

            Activate();
            BarcodeBox.Focus();

            e.Handled = true;
        }

        private async Task SendToWindow(
            IntPtr hwnd,
            string barcode)
        {
            if (hwnd == IntPtr.Zero)
                return;

            SetForegroundWindow(hwnd);

            await Task.Delay(50);

            Forms.SendKeys.SendWait(barcode);

            await Task.Delay(10);

            Forms.SendKeys.SendWait("{ENTER}");
        }
    }
}