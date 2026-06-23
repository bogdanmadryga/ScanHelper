using System;
using System.Threading.Tasks;
using System.Windows;
using Squirrel;

namespace ScanHelper
{
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Запускаємо перевірку оновлень у фоновому потоці
            _ = Task.Run(async () =>
            {
                try
                {
                    using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/ВашАкаунт/ScanHelper"))
                    {
                        await mgr.UpdateApp();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка оновлення: {ex.Message}");
                }
            });
        }
    }
}