using Ambii.Services;
using Ambii.Views;
using MaterialDesignThemes.Wpf; // Cần thiết cho TransitionerSlide
using MaterialDesignThemes.Wpf.Transitions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ambii
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public bool IsCameraReady { get; set; } = false;
        private System.Windows.Threading.DispatcherTimer _inactivityTimer;
        private int frame_cd = 15; // Đây là cấu hình mặc định
        private int _currentCount = 0; // Biến này mới là biến bị trừ dần
        // color text count down
        private readonly SolidColorBrush _pinkAmbii = new SolidColorBrush(Color.FromRgb(244, 116, 126));

        public MainWindow()
        {
            InitializeComponent();
            MainTransitioner.SelectedIndex = 0;
            Instance = this;

            // KHÔNG gọi Navigate(new StartView()) ở đây nữa 
            // vì XAML đã đặt SelectedIndex="0" rồi.
            InitInactivityTimer();
            StartCameraInitialization();
        }

        private async void StartCameraInitialization()
        {
            await Task.Delay(500); // Giả lập load camera
            IsCameraReady = true;
        }

        // HÀM NAVIGATE MỚI: Dùng để chuyển Slide trong Transitioner
        // HÀM NAVIGATE TỐI ƯU DUY NHẤT
        public void Navigate(int index)
        {
            if (MainTransitioner == null || index < 0 || index >= MainTransitioner.Items.Count) return;

            // --- PHẦN 1: QUẢN LÝ TIMER (MỚI THÊM) ---
            // Trước khi chuyển trang, hãy dừng timer cũ để tránh xung đột
            if (_inactivityTimer != null) _inactivityTimer.Stop();

            // --- PHẦN 2: TỐI ƯU RAM (CŨ CỦA ÔNG) ---
            var targetSlide = MainTransitioner.Items[index] as TransitionerSlide;
            if (targetSlide != null && targetSlide.Content == null)
            {
                if (index == 1) targetSlide.Content = new FrameSelectionView();
                // Ví dụ: if (index == 2) targetSlide.Content = new CameraView();
            }

            // Giải phóng RAM khi về trang chủ (Index 0)
            if (index == 0)
            {
                var frameSlide = MainTransitioner.Items[1] as TransitionerSlide;
                if (frameSlide != null) frameSlide.Content = null;

                // Ẩn bảng đếm ngược khi ở màn hình Start
                InactivityPanel.Visibility = Visibility.Collapsed;
            }

            // --- PHẦN 3: KÍCH HOẠT TIMER CHO TRANG MỚI (MỚI THÊM) ---
            switch (index)
            {
                case 1: // FrameSelection: Đếm ngược 60s để quay lại Start
                    StartCountdown(frame_cd);
                    break;

                case 2: // CameraView/Filter: Đếm ngược 30s để tự động Next
                    StartCountdown(30);
                    break;

                    // Thêm các case khác nếu cần...
            }

            // --- PHẦN 4: THỰC HIỆN CHUYỂN TRANG ---
            MainTransitioner.SelectedIndex = index;

            // CẬP NHẬT UI DEBUG & ÉP DỌN RÁC
            UpdateDebugUI();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        private void StartCountdown(int seconds)
        {
            _currentCount = seconds;
            CountDownProgress.Maximum = seconds;
            CountDownProgress.Value = seconds;

            TxtCountdown.Text = _currentCount.ToString();

            // Sử dụng biến màu hồng đã khai báo
            TxtCountdown.Foreground = _pinkAmbii;
            CountDownProgress.Foreground = _pinkAmbii;

            TxtCountdown.FontSize = 32;
            InactivityPanel.Visibility = Visibility.Visible;
            _inactivityTimer.Start();
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
        private void InitInactivityTimer()
        {
            _inactivityTimer = new System.Windows.Threading.DispatcherTimer();
            _inactivityTimer.Interval = TimeSpan.FromSeconds(1);
            _inactivityTimer.Tick += InactivityTimer_Tick;
        }

        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            _currentCount--;

            // 1. Cập nhật con số
            TxtCountdown.Text = _currentCount.ToString();

            // 2. Cập nhật vòng tròn vơi dần
            CountDownProgress.Value = _currentCount;

            // 3. Hiệu ứng cảnh báo (Dưới 10 giây)
            if (_currentCount <= 10)
            {
                var alertColor = Brushes.Red;
                TxtCountdown.Foreground = alertColor;
                CountDownProgress.Foreground = alertColor;

                // Thêm một chút hiệu ứng phóng to nhẹ cho số nếu ông thích
                TxtCountdown.FontSize = 50;
            }
            else
            {
                // Màu hồng Pinky ban đầu
                TxtCountdown.Foreground = _pinkAmbii;
                CountDownProgress.Foreground = _pinkAmbii;
                TxtCountdown.FontSize = 32; // Giữ size cố định cho đẹp
            }

            if (_currentCount <= 0)
            {
                _inactivityTimer.Stop();
                ExecuteTimeoutAction();
            }
        }
        private void ExecuteTimeoutAction()
        {
            int currentIndex = MainTransitioner.SelectedIndex;

            switch (currentIndex)
            {
                case 1: // Hết giờ ở FrameSelection -> Quay về Start & Khóa Permission
                    var settings = SettingsService.Load();
                    if (settings != null && !settings.IsDebugMode)
                    {
                        settings.CheckSessionPermission = false;
                        SettingsService.Save(settings);
                    }
                    Navigate(0);
                    break;

                case 2: // Hết giờ ở FilterSelection -> Tự động đi tiếp
                        // Giả sử ông có logic lưu ảnh ở đây, rồi nhảy sang Index 3
                    Navigate(3);
                    break;
            }
        }

    }
}