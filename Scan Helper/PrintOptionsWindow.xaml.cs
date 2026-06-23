using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;

namespace ScanHelper
{
    public partial class PrintOptionsWindow : Window
    {
        private readonly List<SealItem> _items;

        private readonly Window? _sourceWindow;

        public PrintOptionsWindow(
    List<SealItem> items,
    Window? sourceWindow = null)
        {
            InitializeComponent();

            _items = items;
            _sourceWindow = sourceWindow;

            LoadPrinters();
        }

        private void LoadPrinters()
        {
            LocalPrintServer server =
                new LocalPrintServer();

            foreach (var printer in server.GetPrintQueues())
            {
                TtnPrinterBox.Items.Add(printer.Name);
                A4PrinterBox.Items.Add(printer.Name);
            }

            foreach (var item in TtnPrinterBox.Items)
            {
                string name = item?.ToString() ?? "";

                if (name.Contains("480"))
                    TtnPrinterBox.SelectedItem = item;

                if (name.Contains("ECOSYS"))
                    A4PrinterBox.SelectedItem = item;
            }
        }

        private void PrintTtn_Click(
            object sender,
            RoutedEventArgs e)
        {
            int.TryParse(
                TtnCopiesBox.Text,
                out int copies);

            TtnPrinter.Print(
                _items,
                TtnPrinterBox.Text,
                copies);
        }

        private void PrintA4_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (_sourceWindow == null)
            {
                MessageBox.Show(
                    "Вікно для скріншота не передано");

                return;
            }

            try
            {
                A4Printer.PrintWindow(
                    _sourceWindow,
                    A4PrinterBox.Text);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    ex.Message);
            }
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}