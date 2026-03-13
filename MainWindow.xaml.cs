using System.Windows;
using System.Windows.Controls;
using Ambii.Views; // Đảm bảo bạn đã tạo thư mục Views

namespace Ambii
{
    public partial class MainWindow : Window
    {
        // Tạo một biến static để các màn hình con có thể gọi MainWindow dễ dàng
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            this.Navigate(new StartView());

            // Tạm thời để trống, sau khi tạo xong StartView mình sẽ quay lại đây
        }

        // Hàm dùng để đổi màn hình
        public void Navigate(UserControl nextView)
        {
            MainContentHolder.Content = nextView;
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Kiểm tra nếu phím vừa nhấn là ESC
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // Đóng toàn bộ ứng dụng ngay lập tức
                System.Windows.Application.Current.Shutdown();
            }
        }

    }
}