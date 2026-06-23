using System.Windows;
using ScanHelper.Data;
using ScanHelper.Models;

namespace ScanHelper
{
    public partial class AllNVPWindow : Window
    {
        public AllNVPWindow()
        {
            InitializeComponent();
            NVPStorage.Load();
            NVPList.ItemsSource = NVPStorage.NVPList;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP nvp)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            var w = new ScanNVPWindow(nvp);
            w.ShowDialog();

            NVPStorage.Save();
            NVPList.Items.Refresh();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP nvp)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            var w = new CreateNVPWindow(nvp);
            w.ShowDialog();

            NVPStorage.Save();
            NVPList.Items.Refresh();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (NVPList.SelectedItem is not NVP nvp)
            {
                MessageBox.Show("Выберите НВП");
                return;
            }

            if (MessageBox.Show($"Удалить \"{nvp.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            NVPStorage.NVPList.Remove(nvp);
            NVPStorage.Save();
            NVPList.Items.Refresh();
        }
    }
}
