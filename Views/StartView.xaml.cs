using System.Windows;
using System.Windows.Controls;
using Ambii.Services;

namespace Ambii.Views
{
    public partial class StartView : UserControl
    {
        public StartView()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra phần cứng (Giữ nguyên)
            if (MainWindow.Instance != null && !MainWindow.Instance.IsCameraReady)
            {
                DarkMsg.Show("Lỗi thiết bị", "Camera chưa sẵn sàng. Vui lòng kiểm tra kết nối!");
                return;
            }

            // 2. Kiểm tra quyền (Hàm này ông đã sửa để ưu tiên IsDebugMode rồi đúng không?)
            if (!CheckSessionPermission())
            {
                DarkMsg.Show("Thông báo", "Vui lòng thanh toán tại quầy để bắt đầu lượt chụp.");
                return;
            }

            // 3. KHÓA CỬA (Chỉ reset khi KHÔNG phải Debug)
            var settings = SettingsService.Load();
            if (settings != null && !settings.IsDebugMode)
            {
                // Nếu là khách bình thường thì mới khóa cửa sau khi bấm Start
                settings.CheckSessionPermission = false;
                SettingsService.Save(settings);
            }
            // Nếu là IsDebugMode = true, ta bỏ qua bước này để ông test thoải mái

            // 4. ĐIỀU HƯỚNG SANG FRAME SELECTION (Index 1)
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.Navigate(1);
            }
        }
        private bool CheckSessionPermission()
        {
            // 1. Load file appsettings.json mới nhất
            var settings = SettingsService.Load();

            if (settings == null) return false;

            // 2. LOGIC ƯU TIÊN:
            // Nếu đang bật IsDebugMode thì AUTO CHO QUA (true)
            // Nếu không thì mới check đến biến CheckSessionPermission (Thanh toán)
            if (settings.IsDebugMode)
            {
                return true;
            }

            return settings.CheckSessionPermission;
        }
        // Trong StartView.xaml.cs
        private void BtnAdminSetting_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow { Owner = Window.GetWindow(this) };

            // Tự đăng ký: Khi lưu xong thì gọi hàm UpdateDebugUI của MainWindow
            settingsWindow.OnSettingsSaved = () => {
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.UpdateDebugUI(); // Hàm này đang là public nên gọi được!
                }
            };

            settingsWindow.ShowDialog();
        }
    }
}