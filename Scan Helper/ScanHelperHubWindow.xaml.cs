using System.Windows;
using System.Windows.Input;

namespace ScanHelper
{
    public partial class ScanHelperHubWindow : Window
    {
        public ScanHelperHubWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                BarcodeBox.Focus();
            };

            Activated += (s, e) =>
            {
                BarcodeBox.Focus();
            };
        }

        private void BarcodeBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string barcode =
                BarcodeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(barcode))
                return;

            foreach (Window window in Application.Current.Windows)
            {
                if (ChkSeal.IsChecked == true)
                {
                    if (window is SealWindow sealWindow)
                    {
                        sealWindow.ReceiveBarcode(barcode);
                    }
                }

                if (ChkLabels.IsChecked == true)
                {
                    if (window is PrintLabelsWindow printWindow)
                    {
                        printWindow.ReceiveBarcode(barcode);
                    }
                }
            }

            StatusText.Text =
                $"Надіслано: {barcode}";

            BarcodeBox.Clear();

            BarcodeBox.Focus();

            e.Handled = true;
        }
    }
}