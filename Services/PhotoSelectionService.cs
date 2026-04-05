using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Ambii.Models;
using Ambii.Views;

namespace Ambii.Services
{
    public class PhotoSelectionService : INotifyPropertyChanged
    {
        public ObservableCollection<PhotoItem> AvailablePhotos { get; set; } = new ObservableCollection<PhotoItem>();
        // Danh sách các khung để chọn ở Cột 2
        public ObservableCollection<FrameConfig> AllFrames { get; set; } = new ObservableCollection<FrameConfig>();

        // Danh sách "ảnh thực tế" sẽ hiện lên Canvas ở Cột 1
        public ObservableCollection<object> PreviewSlots { get; set; } = new ObservableCollection<object>();
        private FrameConfig _currentFrame;
        public void LoadFrames(List<FrameConfig> frames)
        {
            AllFrames.Clear();
            foreach (var f in frames) AllFrames.Add(f);
        }

        // Thêm vào PhotoSelectionService.cs
        // ĐỔI 2 DÒNG NÀY ĐỂ VỪA CHỐNG NULL, VỪA CHỐNG SỐ 0
        public double CurrentDisplayWidth;
        public double CurrentDisplayHeight;

        public void SetCurrentFrame(FrameConfig config)
        {
            if (config == null) return;
            _currentFrame = config;
            UpdatePreviewLayout();

            OnPropertyChanged(nameof(SelectedFramePath));
            OnPropertyChanged(nameof(CurrentDisplayWidth));
            OnPropertyChanged(nameof(CurrentDisplayHeight));

            // In ra cửa sổ Output xem kích thước có bị 0 nữa không
            System.Diagnostics.Debug.WriteLine($"[UI UPDATE] Ảnh: {SelectedFramePath} | Kích thước: {CurrentDisplayWidth}x{CurrentDisplayHeight}");
        }



        public void LoadPhotos(List<string> paths)
        {
            AvailablePhotos.Clear();
            foreach (var path in paths)
            {
                // Gọi hàm nạp ảnh để hiển thị thumbnail cho nhẹ app
                AvailablePhotos.Add(new PhotoItem
                {
                    FilePath = path,
                    Thumbnail = LoadBitmap(path)
                });
            }
        }

        public void ToggleSelect(PhotoItem photo)
        {
            if (photo.Order > 0)
            {
                int oldOrder = photo.Order;
                photo.Order = 0;
                // Logic dồn số: Nếu bỏ chọn số 2, tấm số 3 thành 2, số 4 thành 3
                foreach (var p in AvailablePhotos.Where(x => x.Order > oldOrder))
                {
                    p.Order--;
                }
            }
            else
            {
                int currentCount = AvailablePhotos.Count(p => p.Order > 0);
                // Giới hạn chọn tối đa 4 ảnh cho Photobooth
                if (currentCount < 4)
                {
                    photo.Order = currentCount + 1;
                }
            }
            UpdatePreviewLayout();
        }

        // Hàm nạp ảnh tối ưu (không giữ file, tránh lỗi bị khóa file khi chụp/lưu)
        private BitmapImage LoadBitmap(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Nạp hẳn vào RAM để tránh khóa file
            bitmap.DecodePixelWidth = 300; // Giảm độ phân giải cho thumbnail để mượt UI
            bitmap.EndInit();
            bitmap.Freeze(); // Đóng băng để dùng được trên nhiều luồng UI
            return bitmap;
        }

        public void UpdatePreviewLayout()
        {
            if (_currentFrame == null || _currentFrame.Slots == null) return;

            PreviewSlots.Clear();
            var selectedPhotos = AvailablePhotos.Where(p => p.Order > 0)
                                                 .OrderBy(p => p.Order).ToList();

            // Duyệt theo số lượng Slot của khung MỚI
            for (int i = 0; i < _currentFrame.Slots.Count; i++)
            {
                if (i < selectedPhotos.Count)
                {
                    PreviewSlots.Add(new
                    {
                        ImageSource = selectedPhotos[i].Thumbnail,
                        Config = _currentFrame.Slots[i] // Tọa độ X, Y của khung mới
                    });
                }
            }
        }
        public void LoadFramesFromFolder()
        {
            AllFrames.Clear();

            // Lấy thông số "khuôn" từ màn hình trước
            var baseConfig = FrameSelectionView.SelectedFrameData;
            if (baseConfig == null) return;

            if (baseConfig.IsGeneric)
            {
                // QUY TẮC: Folder = Id (Ví dụ: Assets/Frames/ClassicStrip/)
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Frames", baseConfig.Id);

                if (Directory.Exists(folderPath))
                {
                    // Quét tất cả file ảnh mẫu trong folder đó
                    string[] files = Directory.GetFiles(folderPath, "*.png");

                    foreach (string file in files)
                    {
                        // Tạo "bản sao" từ khuôn gốc, chỉ thay đường dẫn ảnh (FramePath)
                        // TÌM ĐẾN ĐOẠN NÀY TRONG HÀM LoadFramesFromFolder
                        AllFrames.Add(new FrameConfig
                        {
                            Id = baseConfig.Id,
                            Name = Path.GetFileNameWithoutExtension(file),
                            FramePath = new Uri(file).AbsoluteUri,

                            CameraWidth = baseConfig.CameraWidth,
                            CameraHeight = baseConfig.CameraHeight,

                            // 👇 THÊM 3 DÒNG NÀY VÀO CHỖ NÀY 👇
                            DisplayWidth = baseConfig.DisplayWidth,
                            DisplayHeight = baseConfig.DisplayHeight,
                            DPI = baseConfig.DPI,
                            // 👆 ---------------------------- 👆

                            Slots = baseConfig.Slots,
                            IsGeneric = true
                        });
                    }
                }
            }
            else
            {
                // Loại SPECIAL: Chỉ có 1 file ảnh duy nhất, lấy trực tiếp từ FramePath trong JSON
                // Lưu ý: FramePath lúc này nên trỏ thẳng tới file ảnh của Special đó
                AllFrames.Add(baseConfig);
            }

            // Mặc định chọn mẫu đầu tiên để hiện lên Cột 1
            if (AllFrames.Count > 0)
            {
                SetCurrentFrame(AllFrames[0]);
            }
        }
        

        // Cập nhật đường dẫn ảnh hiển thị ở Cột 1
        public string SelectedFramePath => _currentFrame?.FramePath;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }

    public class PhotoItem : INotifyPropertyChanged
    {
        public string FilePath { get; set; }

        // Khớp với {Binding thumbnail} trong XAML của ông (WPF phân biệt hoa thường)
        // Ảnh hiển thị ở cột 0
        private BitmapSource _thumbnail;
        public BitmapSource Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(nameof(Thumbnail)); }
        }

        private int _order;
        public int Order
        {
            get => _order;
            set
            {
                _order = value;
                OnPropertyChanged(nameof(Order));
                OnPropertyChanged(nameof(SelectionOrder));
                OnPropertyChanged(nameof(IsSelectedVisibility));
                OnPropertyChanged(nameof(BorderColor));
            }
        }

        // Các thuộc tính Helper cho XAML
        public string SelectionOrder => Order.ToString();
        public bool IsSelected => Order > 0;

        // Khớp với {Binding IsSelectedVisibility}
        public Visibility IsSelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

        // Khớp với {Binding BorderColor}
        public System.Windows.Media.Brush BorderColor => IsSelected
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 116, 126)) // Màu hồng Ambii
            : System.Windows.Media.Brushes.Transparent;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}