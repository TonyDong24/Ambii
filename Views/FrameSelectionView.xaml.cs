using System.Windows;
using System.Windows.Controls;

namespace Ambii.Views
{
    public partial class FrameSelectionView : Window
    {
        public FrameSelectionView()
        {
            InitializeComponent();
            LoadConfiguration();
        }
        // Biến này dùng để lưu trữ loại Frame mà khách đã chọn
        private string _selectedFrame = "";

        // Sự kiện khi nhấn vào một loại Frame
        private void FrameSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                // 2. Gán giá trị vào biến đã khai báo ở trên

                _selectedFrame = btn.Tag.ToString();

                // (Tùy chọn) Làm nút Next sáng lên để báo hiệu đã chọn xong
                BtnNext.Opacity = 1.0;
            }
        }

        private void LoadConfiguration()
        {
            // Vì SettingsService là static, ta gọi trực tiếp .Load()
            var settings = Ambii.Services.SettingsService.Load();

            if (settings != null)
            {
                // Gán trạng thái ẩn/hiện dựa trên biến IsDebugMode
                if (settings.IsDebugMode)
                {
                    BtnBack.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnBack.Visibility = Visibility.Collapsed;
                }
            }
        }

        // Quay lại màn hình chính (MainWindow)
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
        }
        // ĐÂY MỚI LÀ NÚT LET'S GO
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem khách đã chọn Frame nào chưa
            if (string.IsNullOrEmpty(_selectedFrame))
            {
                MessageBox.Show("Vui lòng chọn một kiểu ảnh trước khi tiếp tục!");
                return;
            }

            // Thông báo Start thành công như bạn muốn
            DarkMsg.Show("Ambii Photobooth", "Khởi động Camera thành công!");

            // Chuyển sang màn hình chụp (Khi bạn đã tạo CaptureView)
            // var captureWindow = new CaptureView(_selectedFrame);
            // captureWindow.Show();

            // Đóng cửa sổ hiện tại
            this.Close();
        }
    }
}