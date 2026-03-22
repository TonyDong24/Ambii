using System;
using System.Windows;
using System.Windows.Controls;

namespace Ambii.Views
{
    public partial class FrameSelectionView : UserControl
    {
        private string _selectedFrame = "";

        public FrameSelectionView()
        {
            InitializeComponent();
        }

        private void FrameSelected_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            // 1. Reset tất cả Icon về ẩn
            IconClassic.Visibility = Visibility.Collapsed;
            IconPostcard.Visibility = Visibility.Collapsed;
            IconSolo.Visibility = Visibility.Collapsed;

            // 2. Lấy Tag để biết đang chọn Frame nào
            _selectedFrame = btn.Tag.ToString();

            // 3. Hiển thị Icon tương ứng với Tag
            switch (_selectedFrame)
            {
                case "ClassicStrip":
                    IconClassic.Visibility = Visibility.Visible;
                    break;
                case "Postcard4x6":
                    IconPostcard.Visibility = Visibility.Visible;
                    break;
                case "Single":
                    IconSolo.Visibility = Visibility.Visible;
                    break;
            }

            // 4. Animation Focus (nếu ông vẫn muốn giữ hiệu ứng Scale của Style cũ)
            btn.Focus();

            // 5. Kích hoạt nút Next
            BtnNext.IsEnabled = true;
            BtnNext.Opacity = 1;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra Instance để tránh lỗi NullReference
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.MainTransitioner.SelectedIndex = 0;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedFrame))
            {
                // Chuyển sang màn hình chụp hoặc xử lý tiếp theo
            }
        }
    }
}