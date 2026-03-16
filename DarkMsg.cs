using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ambii
{
    public static class DarkMsg
    {
        public static void Show(string title, string message)
        {
            Window msgBox = new Window
            {
                Title = title,
                Width = 350,
                Height = 200,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Topmost = true
            };

            // Tạo bo góc và màu nền tối
            Border mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(25)
            };

            StackPanel stack = new StackPanel();

            // Tiêu đề trắng sáng
            stack.Children.Add(new TextBlock
            {
                Text = title.ToUpper(),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center
            });

            // Nội dung xám nhạt
            stack.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Brushes.LightGray,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 25),
                TextAlignment = TextAlignment.Center
            });

            // Nút bấm màu TikTok (Hồng đỏ)
            Button btn = new Button
            {
                Content = "ĐÃ HIỂU",
                Width = 120,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(254, 44, 85)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Hiệu ứng bo góc cho nút bấm (dùng Style của Material Design nếu có)
            btn.Click += (s, e) => msgBox.Close();
            stack.Children.Add(btn);

            mainBorder.Child = stack;
            msgBox.Content = mainBorder;

            msgBox.ShowDialog();
        }
    }
}