namespace YishilNotaOCR.Models
{
    public class PdfPageItem : System.ComponentModel.INotifyPropertyChanged
    {
        private System.Windows.Media.ImageSource _thumbnail;
        public int PageNumber { get; set; }
        public bool IsSelected { get; set; }

        public System.Windows.Media.ImageSource Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(nameof(Thumbnail)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
