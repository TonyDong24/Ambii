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
        // HÀM NAVIGATE TỐI ƯU DUY NHẤT
        public void Navigate(int index)
        {
            if (MainTransitioner == null || index < 0 || index >= MainTransitioner.Items.Count) return;

            var targetSlide = MainTransitioner.Items[index] as TransitionerSlide;

            // 1. TỐI ƯU RAM: Chỉ khởi tạo View khi thực sự chuyển tới (Lazy Loading)
            if (targetSlide != null && targetSlide.Content == null)
            {
                if (index == 1) // Giả định Slide 1 là FrameSelectionView
                {
                    targetSlide.Content = new FrameSelectionView();
                }
                // Thêm các index khác ở đây nếu ông có thêm màn hình (vđ: index == 2 là ResultView)
            }

            // 2. GIẢI PHÓNG RAM: Nếu quay về trang chủ (index 0), hãy xóa View trang chọn Frame
            if (index == 0)
            {
                var frameSlide = MainTransitioner.Items[1] as TransitionerSlide;
                if (frameSlide != null)
                {
                    frameSlide.Content = null; // Xóa view để giải phóng ~500MB RAM
                }
            }

            // 3. THỰC HIỆN CHUYỂN TRANG
            MainTransitioner.SelectedIndex = index;

            // 4. CẬP NHẬT UI DEBUG & ÉP DỌN RÁC
            UpdateDebugUI();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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

        // Hàm cập nhật UI dùng chung
        public void UpdateDebugUI()
        {
            var settings = SettingsService.Load();
            bool isDebug = settings?.IsDebugMode ?? false;

            // 1. Cập nhật cho Slide HIỆN TẠI (vừa trượt tới)
            var currentSlide = MainTransitioner.Items[MainTransitioner.SelectedIndex] as TransitionerSlide;
            UpdateViewButton(currentSlide?.Content, isDebug);

            // 2. (Cẩn thận hơn) Cập nhật cho TẤT CẢ các slide để đảm bảo không cái nào bị sót
            foreach (TransitionerSlide slide in MainTransitioner.Items)
            {
                if (slide.Content != null)
                {
                    UpdateViewButton(slide.Content, isDebug);
                }
                    
            }
        }
        private void UpdateViewButton(object content, bool isDebug)
        {
            if (content is UserControl view)
            {
                var btnBack = view.FindName("BtnBack") as Button;
                if (btnBack != null)
                {
                    btnBack.Visibility = isDebug ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }
        public void ShowSettings()
        {
            SettingsWindow settingsWin = new SettingsWindow();

            // Đăng ký: "Khi mày lưu xong thì tao (MainWindow) sẽ chạy hàm này"
            settingsWin.OnSettingsSaved = () => {
                UpdateDebugUI();
            };

            settingsWin.ShowDialog();
        }
    }
}