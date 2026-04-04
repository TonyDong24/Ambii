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
    public partial class PhotoSelectionView : UserControl, INotifyPropertyChanged
    {
        // Danh sách data-binding cho 8 tấm ảnh
        public ObservableCollection<PhotoItem> CapturedPhotos { get; set; } = new ObservableCollection<PhotoItem>();
        public ObservableCollection<SlotPreviewItem> PreviewSlots { get; set; } = new ObservableCollection<SlotPreviewItem>();
        private BitmapImage _finalFramePath; // Đổi từ string sang BitmapImage
        public BitmapImage FinalFramePath
        {
            get => _finalFramePath;
            set { _finalFramePath = value; OnPropertyChanged(nameof(FinalFramePath)); }
        }

        public PhotoSelectionView()
        {
            InitializeComponent();
            this.DataContext = this;
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
            UpdatePhotoSelection(item);

            // Logic chọn ảnh (Tạm thời để đây, Bước sau mình sẽ viết logic giới hạn 4 tấm)
            
        }
        private void UpdatePhotoSelection(PhotoItem item)
        {
            // 1. Lấy danh sách những tấm đang được chọn (đã sắp xếp)
            var selectedList = CapturedPhotos.Where(p => p.IsSelected)
                                             .OrderBy(p => p.SelectionOrder)
                                             .ToList();

            if (item.IsSelected) // Trường hợp: Click vào tấm đã chọn -> Bỏ chọn
            {
                item.IsSelected = false;
                item.SelectionOrder = 0;

                // Quan trọng: Sắp xếp lại số thứ tự những tấm còn lại (để không bị hổng số)
                var remaining = CapturedPhotos.Where(p => p.IsSelected)
                                              .OrderBy(p => p.SelectionOrder)
                                              .ToList();
                for (int i = 0; i < remaining.Count; i++)
                {
                    remaining[i].SelectionOrder = i + 1;
                }
            }
            else // Trường hợp: Click vào tấm chưa chọn -> Chọn mới
            {
                if (CapturedPhotos.Count(p => p.IsSelected) < PreviewSlots.Count)
                {
                    item.IsSelected = true;
                    item.SelectionOrder = CapturedPhotos.Count(p => p.IsSelected) + 1;
                }

            }
            RefreshLivePreview();
        }
        private void RefreshLivePreview()
        {
            var selected = CapturedPhotos.Where(p => p.IsSelected).ToList();

            foreach (var slot in PreviewSlots)
            {
                // Tìm ảnh có số thứ tự khớp với số Index của ô trong JSON
                var match = selected.FirstOrDefault(p => p.SelectionOrder == slot.Config.Index);
                slot.ImageSource = match?.thumbnail; // Đổ thumbnail vào ô preview
            }
        }
        public void LoadFrameConfig(Ambii.Models.FrameConfig config, string framePath)
        {
            System.Windows.MessageBox.Show("Đang nạp Frame: " + framePath);
            if (!System.IO.File.Exists(framePath))
            {
                System.Windows.MessageBox.Show("FILE KHÔNG TỒN TẠI!");
                return;
            }

            try
            {
                // Chuyển đường dẫn string thành Object BitmapImage mà WPF yêu thích
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(framePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Giúp load ảnh nhanh và không bị khóa file
                bitmap.EndInit();
                bitmap.Freeze(); // Quan trọng: Giúp dùng Bitmap trên UI thread mượt hơn

                this.FinalFramePath = bitmap; // Gán Object vào Property
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load ảnh: " + ex.Message);
            }

            // --- Giữ nguyên phần tạo PreviewSlots bên dưới ---
            PreviewSlots.Clear();
            foreach (var slot in config.Slots)
            {
                PreviewSlots.Add(new SlotPreviewItem { Config = slot });
            }
            RefreshLivePreview();
        }
        // Sự kiện bắt buộc của interface INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Hàm bổ trợ để thông báo cập nhật UI
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

        // Trong class PhotoItem (cuối file) sửa lại SelectionOrder:
        private int _selectionOrder;
        public int SelectionOrder
        {
            get => _selectionOrder;
            set
            {
                _selectionOrder = value;
                OnPropertyChanged(nameof(SelectionOrder)); // Phải có dòng này UI mới nhảy số
            }
        }

        public Brush BorderColor => IsSelected ? new SolidColorBrush(Color.FromRgb(244, 116, 126)) : Brushes.Transparent;
        public Visibility IsSelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class SlotPreviewItem : INotifyPropertyChanged
    {
        public Ambii.Models.PhotoSlot Config { get; set; } // Chứa X, Y, Width, Height từ JSON

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(nameof(ImageSource)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}