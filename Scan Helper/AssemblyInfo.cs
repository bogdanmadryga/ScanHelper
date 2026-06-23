using System.Windows;
using ScanHelper.Data;

namespace ScanHelper
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NVPStorage.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            NVPStorage.Save();
            base.OnExit(e);
        }
    }
}
