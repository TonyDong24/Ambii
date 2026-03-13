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
            // Khi ấn nút này, nó sẽ thông báo cho MainWindow chuyển sang trang tiếp theo
            // Tạm thời chưa có trang tiếp theo nên ta sẽ để đây.
            MessageBox.Show("Đã nhấn Start! Sẽ chuyển sang chọn Frame.");
        }
        private void BtnAdminSetting_Click(object sender, RoutedEventArgs e)
        {
            // Tạm thời hiển thị thông báo để test
            MessageBox.Show("Chào Admin! Chức năng cấu hình hệ thống đang được phát triển.");
        }
    }
}