using System.Windows;
using System.Windows.Controls;

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
            // 1. Kiểm tra phần cứng (Giữ nguyên logic của bạn)
            if (MainWindow.Instance != null && !MainWindow.Instance.IsCameraReady)
            {
                DarkMsg.Show("Lỗi thiết bị", "Camera chưa sẵn sàng. Vui lòng kiểm tra kết nối!");
                return;
            }

            // 2. Kiểm tra bản quyền (Giữ nguyên)
            if (!CheckSessionPermission())
            {
                DarkMsg.Show("Thông báo", "Vui lòng thanh toán tại quầy để bắt đầu lượt chụp.");
                return;
            }

            // 3. ĐIỀU HƯỚNG SANG FRAME SELECTION (Sửa tại đây)
            if (MainWindow.Instance != null)
            {
                // CHỈ CẦN dòng này để ra lệnh cho cái "máy trượt" nhảy sang slide tiếp theo
                // Slide 0 là StartView, Slide 1 là FrameSelectionView (như mình đã đặt trong XAML)
                MainWindow.Instance.MainTransitioner.SelectedIndex = 1;

                // Xóa bỏ hoàn toàn việc tạo 'new FrameSelectionView' 
                // và xóa bỏ việc gán 'Content = ...'
            }
        }
        private bool CheckSessionPermission()
        {
            // HIỆN TẠI: Luôn trả về true để bạn test app mượt mà
            // SAU NÀY: Bạn chỉ cần sửa chỗ này thành: return _settings.RemainingSessions > 0;
            return true;
        }
        private void BtnAdminSetting_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow
            {
                Owner = Window.GetWindow(this)
            };
            settingsWindow.ShowDialog();
        }
    }
}