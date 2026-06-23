using System.Collections.ObjectModel;

namespace ScanHelper.Models
{
    public class NVP
    {
        public int Id { get; set; }          // ✅ добавить
        public string Name { get; set; }
        public ObservableCollection<NVPItem> Items { get; set; } = new();
    }
}
