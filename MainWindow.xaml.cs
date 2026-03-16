using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Ambii.Views;
using Ambii.Services; // Thêm để dùng SettingsService

namespace Ambii
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public bool IsCameraReady { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            // 1. Chuyển sang StartView ngay khi mở
            this.Navigate(new StartView());

            // 2. Khởi tạo Camera ngầm
            StartCameraInitialization();

            // 3. Đăng ký bắt sự kiện phím nhấn xuống cho toàn bộ cửa sổ này
            this.KeyDown += Window_KeyDown;
        }

        private async void StartCameraInitialization()
        {
            await Task.Delay(3000); // Giả lập load camera
            IsCameraReady = true;
        }

        public void Navigate(UserControl nextView)
        {
            MainContentHolder.Content = nextView;
        }

        // 4. Xử lý ESC để thoát toàn bộ ứng dụng
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // Chỉ cho thoát bằng ESC khi đang bật Debug Mode (để khách không phá được)
                var settings = SettingsService.Load();
                if (settings != null && settings.IsDebugMode)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        // Bạn có thể xóa hẳn hàm ExitApp_Click này đi vì không dùng nút nữa
    }
}