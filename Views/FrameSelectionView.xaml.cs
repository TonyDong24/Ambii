using System;
using System.Windows;
using System.Windows.Controls;
using Ambii.Models;
using Ambii.Services;

namespace Ambii.Views
{
    public partial class FrameSelectionView : UserControl
    {
        private string _selectedFrame = "";
        private ConfigService _configService;
        public static FrameConfig SelectedFrameData { get; private set; }

        public FrameSelectionView()
        {
            InitializeComponent();
            _configService = new ConfigService();
            _configService.LoadConfigs();
            this.Unloaded += (s, e) => {
                this.DataContext = null;
            };
            this.IsVisibleChanged += (s, e) => {
                if ((bool)e.NewValue) // Nếu IsVisible == true
                {
                    var settings = SettingsService.Load();
                    bool isDebug = settings?.IsDebugMode ?? false;
                    BtnBack.Visibility = isDebug ? Visibility.Visible : Visibility.Collapsed;
                }
            };

        }

        private void FrameSelected_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            // 1. Reset các Icon (Giữ nguyên code cũ của ông)
            IconClassic.Visibility = Visibility.Collapsed;
            IconPostcard.Visibility = Visibility.Collapsed;
            IconSolo.Visibility = Visibility.Collapsed;

            // 2. Lấy Tag
            _selectedFrame = btn.Tag.ToString();

            // --- BƯỚC MỚI: Tra cứu thông tin từ JSON ---
            if (_configService.Frames != null)
            {
                // Tìm frame trong danh sách có Id khớp với Tag của Button
                SelectedFrameData = _configService.Frames.Find(f => f.Id == _selectedFrame);
            }

            // 3. Hiển thị Icon tương ứng (Giữ nguyên switch case của ông)
            switch (_selectedFrame)
            {
                case "ClassicStrip": IconClassic.Visibility = Visibility.Visible; break;
                case "Postcard4x6": IconPostcard.Visibility = Visibility.Visible; break;
                case "Single": IconSolo.Visibility = Visibility.Visible; break;
            }

            btn.Focus();
            BtnNext.IsEnabled = true;
            BtnNext.Opacity = 1;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            //if (!App.IsDebugMode) return; // Nếu không phải debug thì bấm cũng không chạy
            _selectedFrame = "";
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
                // Ví dụ: Lấy kích thước để debug hoặc chuẩn bị cho Camera
                double w = SelectedFrameData.CameraWidth;
                double h = SelectedFrameData.CameraHeight;

                // Chuyển sang màn hình chụp (ví dụ Index 2)
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.MainTransitioner.SelectedIndex = 2;
                }
            }
        }

        
    }
}