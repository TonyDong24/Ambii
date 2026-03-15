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
            SettingsWindow settingsWindow = new SettingsWindow
            {
                Owner = Window.GetWindow(this)
            };

            settingsWindow.ShowDialog();
        }
    }
}