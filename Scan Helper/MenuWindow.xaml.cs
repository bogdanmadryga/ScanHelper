using EdgeTTS;
using Microsoft.Web.WebView2.Core;
using Scan_Helper;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ScanHelper
{
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
        }

        private void ScanCount_Click(object sender, RoutedEventArgs e)
        {
            new ScanCountWindow().Show();
            Close();
        }

        private void Simulator_Click(object sender, RoutedEventArgs e)
        {
            new NVPManagerWindow().Show();
            Close();
        }

        private void DatabaseCheck_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.google.com/spreadsheets/d/1OOa3ENNW31b8dzMQ_Xu30-F_TRqBJdH3Z0jQ5AgwWFY/edit?usp=sharing",
                UseShellExecute = true
            });
        }
        private void OfficeInstagram_Click(object sender, RoutedEventArgs e)
{
    new OfficeInstagramWindow().Show();
    Close();
}

        private void StockPrint_Click(object sender, RoutedEventArgs e)
        {
            new StockPrintWindow().Show();
            Close();
        }
        // ✅ Аналіз Стану Складу
        private void Analysis_Click(object sender, RoutedEventArgs e)
        {
            new AnalysisWindow().Show();
            Close();
        }
        private void PrintLabels_Click(object sender, RoutedEventArgs e)
        {
            new PrintLabelsWindow().Show();

            Close();
        }
        private void Seal_Click(object sender, RoutedEventArgs e)
        {
            new SealWindow().Show();
        }
        private void BarcodeHub_Click(object sender, RoutedEventArgs e)
        {
            new BarcodeHubWindow().Show();
        }
        private void OpenInstruction_Click(
    object sender,
    RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.google.com/document/d/1-gRheWylmphoPWnUZxa19cEbMOHHB3mwfC9ZJqu7vJ8/edit?usp=sharing",
                UseShellExecute = true
            });
        }
        private void ScanHelperHub_Click(
    object sender,
    RoutedEventArgs e)
        {
            new ScanHelperHubWindow().Show();
        }


        private MediaPlayer _player = new MediaPlayer();
        private void VoiceHub_Click(
    object sender,
    RoutedEventArgs e)
        {
            new VoiceHubWindow().Show();
        }
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
    IntPtr hWnd,
    IntPtr hWndInsertAfter,
    int X,
    int Y,
    int cx,
    int cy,
    uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private void VoiceAssistant_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        // 1) Открываем VoiceHub
        new VoiceHubWindow().Show();

        // 2) Полный путь к Barcode Voice
        string htmlPath =
            @"C:\Users\HP EliteBook\OneDrive\Desktop\Scan Helper разработка\Scan Helper\BarcodeVoice\index25.html";

        if (!File.Exists(htmlPath))
        {
            MessageBox.Show(
                "Не знайдено файл:\n" + htmlPath,
                "Помилка");
            return;
        }

        // 3) Размеры экрана
        int screenWidth = (int)SystemParameters.WorkArea.Width;
        int screenHeight = (int)SystemParameters.WorkArea.Height;

        int browserWidth = screenWidth / 2;
        int browserHeight = screenHeight;
        int browserX = screenWidth / 2;
        int browserY = 0;

        // 4) Пытаемся открыть в Edge новым окном справа
        string edgePath =
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

        if (!File.Exists(edgePath))
        {
            edgePath =
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
        }

        if (File.Exists(edgePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = edgePath,
                Arguments =
                    $"--new-window " +
                    $"--window-position={browserX},{browserY} " +
                    $"--window-size={browserWidth},{browserHeight} " +
                    $"\"{htmlPath}\"",
                UseShellExecute = true
            });
        }
        else
        {
            // Если Edge не найден — просто открываем как есть
            Process.Start(new ProcessStartInfo
            {
                FileName = htmlPath,
                UseShellExecute = true
            });
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            ex.ToString(),
            "Помилка відкриття голосового помічника");
    }
}

    }
}
