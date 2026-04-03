using System;
using System.Windows;
using System.Windows.Controls;
using Ambii.Models;
using Ambii.Services;

namespace Ambii.Views
{
    public partial class FrameSelectionView : UserControl
    {
        private string _selectedFrame = "";
        private ConfigService _configService;
        public static FrameConfig SelectedFrameData { get; private set; }

        public FrameSelectionView()
        {
            InitializeComponent();

            // 1. Khởi tạo service (Nhưng đừng gọi LoadConfigs ở đây nữa)
            _configService = new ConfigService();

            this.Unloaded += (s, e) => {
                this.DataContext = null;
            };

            // 2. Chuyển toàn bộ logic Load vào IsVisibleChanged
            this.IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue) // Khi màn hình Index 1 này hiện lên
                {
                    // --- A. Cập nhật quyền Debug (Ẩn/Hiện nút Back) ---
                    var settings = SettingsService.Load();
                    bool isDebug = settings?.IsDebugMode ?? false;
                    BtnBack.Visibility = isDebug ? Visibility.Visible : Visibility.Collapsed;

                    // --- B. Cập nhật danh sách Frame (frames_config.json) ---
                    // Gọi lại hàm này để đọc file JSON mới nhất từ folder Configs
                    _configService.LoadConfigs();

                    // --- C. Cập nhật UI (Nếu ông dùng ItemsSource) ---
                    // Nếu ông đang dùng một ListBox hoặc ComboBox để hiện danh sách frame:
                    // ListFrames.ItemsSource = _configService.Frames; 
                    // (Hoặc nếu dùng DataContext thì gán lại DataContext)
                }
            };
        }

        private void FrameSelected_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            // 1. Reset các Icon (Giữ nguyên code cũ của ông)
            IconClassic.Visibility = Visibility.Collapsed;
            IconPostcard.Visibility = Visibility.Collapsed;
            IconSolo.Visibility = Visibility.Collapsed;

            // 2. Lấy Tag
            _selectedFrame = btn.Tag.ToString();

            // --- BƯỚC MỚI: Tra cứu thông tin từ JSON ---
            if (_configService.Frames != null)
            {
                // Tìm frame trong danh sách có Id khớp với Tag của Button
                SelectedFrameData = _configService.Frames.Find(f => f.Id == _selectedFrame);
            }

            // 3. Hiển thị Icon tương ứng (Giữ nguyên switch case của ông)
            switch (_selectedFrame)
            {
                case "ClassicStrip": IconClassic.Visibility = Visibility.Visible; break;
                case "Postcard4x6": IconPostcard.Visibility = Visibility.Visible; break;
                case "Single": IconSolo.Visibility = Visibility.Visible; break;
            }

            btn.Focus();
            BtnNext.IsEnabled = true;
            BtnNext.Opacity = 1;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            //if (!App.IsDebugMode) return; // Nếu không phải debug thì bấm cũng không chạy
            _selectedFrame = "";
            // Kiểm tra Instance để tránh lỗi NullReference
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.MainTransitioner.SelectedIndex = 0;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra xem đã chọn Frame chưa
            if (string.IsNullOrEmpty(_selectedFrame))
            {
                DarkMsg.Show("Thông báo", "Vui lòng chọn một khung ảnh bạn yêu thích!");
                return;
            }
            if (SelectedFrameData == null)
            {
                DarkMsg.Show("Lỗi dữ liệu", "Không tìm thấy cấu hình cho khung ảnh này. Vui lòng chọn lại!");
                return;
            }
            if (_selectedFrame == "Single")
            {
                DarkMsg.Show("Thông báo", "Frame này vẫn đang cập nhật, vui lòng chọn Frame khác");
                return;
            }

            // 2. CHECK CAMERA READY LẦN CUỐI (Tuyến phòng thủ cuối cùng)
            if (MainWindow.Instance == null || !MainWindow.Instance.IsCameraReady)
            {
                DarkMsg.Show("Lỗi thiết bị", "Tín hiệu Camera đã bị ngắt kết nối. Vui lòng kiểm tra lại thiết bị!");
                return; // Chặn đứng, không cho sang màn hình chụp
            }

            
            double w = SelectedFrameData.CameraWidth;
            double h = SelectedFrameData.CameraHeight;

            // Chuyển sang màn hình chụp (Index 2)
            if (MainWindow.Instance != null)
            {
                
                MainWindow.Instance.Navigate(2);
            }
        }


    }
}