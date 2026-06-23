using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScanHelper
{
    public static class A4Printer
    {
        public static void PrintWindow(
            Window window,
            string printerName)
        {
            LocalPrintServer server = new();

            var printer =
                server.GetPrintQueues()
                .FirstOrDefault(x =>
                    x.Name == printerName);

            if (printer == null)
            {
                MessageBox.Show(
                    "Принтер не знайдено");

                return;
            }

            RenderTargetBitmap bitmap =
                new(
                    (int)window.ActualWidth,
                    (int)window.ActualHeight,
                    96,
                    96,
                    PixelFormats.Pbgra32);

            bitmap.Render(window);

            PrintDialog dialog = new();

            dialog.PrintQueue = printer;

            System.Windows.Controls.Image image =
                new()
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform
                };

            image.Measure(
                new Size(794, 1123));

            image.Arrange(
                new Rect(
                    0,
                    0,
                    794,
                    1123));

            dialog.PrintVisual(
                image,
                "Seal Screenshot");
        }
    }
}