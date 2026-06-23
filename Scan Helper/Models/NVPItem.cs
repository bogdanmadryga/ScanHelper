using System;
using System.ComponentModel;

namespace ScanHelper.Models
{
    public enum NvpRowStatus
    {
        Under = 0,  // не хватает
        Match = 1,  // сошлось
        Over = 2    // перебор
    }

    public class NVPItem : INotifyPropertyChanged
    {
        private string _barcode = "";
        private int _expectedCount;
        private int _scannedCount;
        private bool _isLastScanned;
        private NvpRowStatus _status;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Barcode
        {
            get => _barcode;
            set
            {
                if (_barcode == value) return;
                _barcode = value;
                OnChanged(nameof(Barcode));
            }
        }

        public int ExpectedCount
        {
            get => _expectedCount;
            set
            {
                if (_expectedCount == value) return;
                _expectedCount = value < 0 ? 0 : value;
                OnChanged(nameof(ExpectedCount));
                OnChanged(nameof(Left));
                OnChanged(nameof(Over));
            }
        }

        public int ScannedCount
        {
            get => _scannedCount;
            set
            {
                int v = value < 0 ? 0 : value;
                if (_scannedCount == v) return;
                _scannedCount = v;
                OnChanged(nameof(ScannedCount));
                OnChanged(nameof(Left));
                OnChanged(nameof(Over));
            }
        }

        public int Left => Math.Max(0, ExpectedCount - ScannedCount);
        public int Over => Math.Max(0, ScannedCount - ExpectedCount);

        // статус строки (для цветов)
        public NvpRowStatus Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnChanged(nameof(Status));
            }
        }

        // подсветка “последний отсканированный”
        public bool IsLastScanned
        {
            get => _isLastScanned;
            set
            {
                if (_isLastScanned == value) return;
                _isLastScanned = value;
                OnChanged(nameof(IsLastScanned));
            }
        }

        private void OnChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}


