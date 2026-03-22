using Ambii.Services;
using Ambii.Views;
using MaterialDesignThemes.Wpf; // Cần thiết cho TransitionerSlide
using MaterialDesignThemes.Wpf.Transitions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ambii
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public bool IsCameraReady { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            MainTransitioner.SelectedIndex = 0;
            Instance = this;

            // KHÔNG gọi Navigate(new StartView()) ở đây nữa 
            // vì XAML đã đặt SelectedIndex="0" rồi.

            StartCameraInitialization();
        }

        private async void StartCameraInitialization()
        {
            await Task.Delay(3000); // Giả lập load camera
            IsCameraReady = true;
        }

        // HÀM NAVIGATE MỚI: Dùng để chuyển Slide trong Transitioner
        public void Navigate(int index)
        {
            if (MainTransitioner != null && index >= 0 && index < MainTransitioner.Items.Count)
            {
                MainTransitioner.SelectedIndex = index;
            }
        }

        // Cách Navigate theo Type (Sửa lỗi 'materialDesign' không tồn tại)
        public void NavigateToView<T>() where T : UserControl
        {
            if (MainTransitioner == null) return;

            // Tìm slide chứa nội dung có kiểu là T
            var slide = MainTransitioner.Items.OfType<TransitionerSlide>()
                            .FirstOrDefault(s => s.Content is T);

            if (slide != null)
            {
                MainTransitioner.SelectedIndex = MainTransitioner.Items.IndexOf(slide);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                var settings = SettingsService.Load();
                if (settings != null && settings.IsDebugMode)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        // XÓA HOẶC SỬA hàm Start_Click: Không dùng this.Content = ...
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Navigate(1); // Chuyển sang slide số 1 (FrameSelectionView)
        }
    }
}