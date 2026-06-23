using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using ScanHelper.Models;

namespace ScanHelper.Data
{
    public static class NVPStorage
    {
        public static ObservableCollection<NVP> NVPList { get; private set; } = new();

        // ✅ путь НЕ в bin, а в AppData (чтобы не затирался при запуске/сборке)
        public static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScanHelper", "nvp.json");

        public static void Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                if (!File.Exists(FilePath))
                {
                    NVPList = new ObservableCollection<NVP>();
                    Save(); // создаём файл сразу
                    return;
                }

                var json = File.ReadAllText(FilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    NVPList = new ObservableCollection<NVP>();
                    Save();
                    return;
                }

                var list = JsonSerializer.Deserialize<ObservableCollection<NVP>>(json)
                           ?? new ObservableCollection<NVP>();

                // страховка от null Items
                foreach (var nvp in list)
                    nvp.Items ??= new ObservableCollection<NVPItem>();

                NVPList = list;
            }
            catch
            {
                NVPList = new ObservableCollection<NVP>();
            }
        }

        public static void Save()
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var json = JsonSerializer.Serialize(NVPList, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }

        public static int NextId()
        {
            return NVPList.Any() ? NVPList.Max(x => x.Id) + 1 : 1;
        }
    }
}





