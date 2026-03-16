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
            // 1. Kiểm tra phần cứng (Camera)
            if (MainWindow.Instance != null && !MainWindow.Instance.IsCameraReady)
            {
                // Thay vì MessageBox trắng xóa, dùng hàm Show của bạn hoặc Snackbar
                DarkMsg.Show("Lỗi thiết bị", "Camera chưa sẵn sàng. Vui lòng kiểm tra kết nối!");
                return;
            }

            // 2. Kiểm tra bản quyền/Thanh toán (Chỗ này để dành cho Remote Control sau này)
            if (!CheckSessionPermission())
            {
                DarkMsg.Show("Thông báo", "Vui lòng thanh toán tại quầy để bắt đầu lượt chụp.");
                return;
            }

            // 3. Nếu mọi thứ OK -> Chuyển sang màn hình chụp
            // MainWindow.Instance.MainContentHolder.Content = new CaptureView();
            //DarkMsg.Show("Thành công", "Bắt đầu lượt chụp của bạn!");
            FrameSelectionView frameWindow = new FrameSelectionView();
            frameWindow.Show();

            // 2. Tìm cửa sổ cha (Window) đang chứa UserControl này và đóng nó lại
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
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