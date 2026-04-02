using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ambii.Views
{
    public partial class PhotoSelectionView : UserControl
    {
        // Danh sách data-binding cho 8 tấm ảnh
        public ObservableCollection<PhotoItem> CapturedPhotos { get; set; } = new ObservableCollection<PhotoItem>();

        public PhotoSelectionView()
        {
            InitializeComponent();
            ListCapturedPhotos.ItemsSource = CapturedPhotos;
        }

        public void ReceivePhotos(List<string> photoPaths)
        {
            CapturedPhotos.Clear();
            foreach (var path in photoPaths)
            {
                CapturedPhotos.Add(new PhotoItem
                {
                    FullSizePath = path,
                    thumbnail = CreateThumbnail(path)
                });
            }

        }

        private BitmapImage CreateThumbnail(string path)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path);
            bi.DecodePixelWidth = 300;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private void Photo_Click(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as PhotoItem;
            if (item == null) return;

            // Logic chọn ảnh (Tạm thời để đây, Bước sau mình sẽ viết logic giới hạn 4 tấm)
            item.IsSelected = !item.IsSelected;
        }
    }

    // Class bổ trợ để lưu trạng thái từng tấm ảnh
    public class PhotoItem : INotifyPropertyChanged
    {
        public string FullSizePath { get; set; }
        public BitmapImage thumbnail { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(IsSelectedVisibility));
            }
        }

        public int SelectionOrder { get; set; } = 1;

        public Brush BorderColor => IsSelected ? new SolidColorBrush(Color.FromRgb(244, 116, 126)) : Brushes.Transparent;
        public Visibility IsSelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}