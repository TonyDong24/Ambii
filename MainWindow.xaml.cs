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
        private CameraService _cameraService = new();
        // color text count down
        private readonly SolidColorBrush _pinkAmbii = new SolidColorBrush(Color.FromRgb(244, 116, 126));

        public MainWindow()
        {
            InitializeComponent();
            MainTransitioner.SelectedIndex = 0;
            Instance = this;
            _cameraService.NewFrameAvailable += (s, frameSource) =>
            {
                Dispatcher.Invoke(() => {
                    // Chỉ cập nhật ảnh khi đang ở trang Camera (Index 2)
                    if (MainTransitioner.SelectedIndex == 2)
                    {
                        var cameraSlide = MainTransitioner.Items[2] as TransitionerSlide;
                        if (cameraSlide?.Content is CameraView cv)
                        {
                            cv.UpdatePreview(frameSource);
                        }
                    }
                });
            };

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

            // --- PHẦN 1: QUẢN LÝ TIMER & CAMERA (PHÒNG THỦ) ---
            if (_inactivityTimer != null) _inactivityTimer.Stop();

            // KIỂM TRA: Nếu đang ở Camera (Index 2) mà đi chỗ khác thì phải TẮT
            if (MainTransitioner.SelectedIndex == 2 && index != 2)
            {
                _cameraService?.Stop();
            }

            // --- PHẦN 2: TỐI ƯU RAM & KHỞI TẠO VIEW ---
            var targetSlide = MainTransitioner.Items[index] as TransitionerSlide;

            // Logic Lazy Loading cho từng màn hình
            if (targetSlide != null && targetSlide.Content == null)
            {
                if (index == 1) targetSlide.Content = new FrameSelectionView();

                // KHỞI TẠO CAMERAVIEW (Index 2)
                if (index == 2)
                {
                    // 1. [MỚI] Load settings mới nhất từ file (Để lấy MirrorPreview, Brightness...)
                    var settings = SettingsService.Load();

                    // 2. [MỚI] "Bơm" settings vào Service ngay lập tức
                    // Việc này giúp CameraService biết có phải lật ảnh (Mirror) hay không trước khi Start
                    _cameraService.UpdateSettings(settings);

                    var cameraView = new CameraView();

                    // Lấy cấu hình khung hình (Dài/Rộng) từ bước trước
                    var currentConfig = FrameSelectionView.SelectedFrameData;
                    if (currentConfig != null)
                    {
                        cameraView.Setup(currentConfig);
                    }

                    // 3. [TỐI ƯU] Gán instance này vào Content của Slide
                    targetSlide.Content = cameraView;

                    // Lưu ý: Đừng gọi _cameraService.Start() ở đây. 
                    // Hãy để nó ở switch(index) bên dưới như code cũ của ông để đảm bảo UI đã render xong.
                }
            }

            // Giải phóng RAM khi về trang chủ (Index 0)
            if (index == 0)
            {
                var frameSlide = MainTransitioner.Items[1] as TransitionerSlide;
                if (frameSlide != null) frameSlide.Content = null;

                var cameraSlide = MainTransitioner.Items[2] as TransitionerSlide;
                if (cameraSlide != null) cameraSlide.Content = null; // Dọn luôn cả CameraView

                InactivityPanel.Visibility = Visibility.Collapsed;
            }

            // --- PHẦN 3: KÍCH HOẠT TIMER & BẬT PHẦN CỨNG ---
            switch (index)
            {
                case 1:
                    StartCountdown(frame_cd);
                    break;

                case 2:
                    // BẬT CAMERA KHI VÀO MÀN HÌNH CHỤP
                    var settings = SettingsService.Load();
                    _cameraService.Start(settings.CameraName);

                    StartCountdown(30);
                    break;
            }

            // --- PHẦN 4: THỰC HIỆN CHUYỂN TRANG ---
            MainTransitioner.SelectedIndex = index;

            UpdateDebugUI();
            // Giữ nguyên phần GC.Collect() của ông để dọn RAM
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


        // XÓA HOẶC SỬA hàm Start_Click: Không dùng this.Content = ...
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            var latestSettings = SettingsService.Load();
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
        private async void ExecuteTimeoutAction()
        {
            int currentIndex = MainTransitioner.SelectedIndex;
            if (currentIndex != 0)
            {
                var settings = SettingsService.Load();
                // Chỉ khóa khi không ở chế độ Debug (để ông dễ test)
                if (settings != null && !settings.IsDebugMode)
                {
                    settings.CheckSessionPermission = false;
                    SettingsService.Save(settings);
                }
            }

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
                    await TriggerCameraCapture();
                    break;

                case 3:
                    Navigate(0);
                    break;
            }
        }

        private async Task TriggerCameraCapture()
        {
            var cameraSlide = MainTransitioner.Items[2] as TransitionerSlide;
            if (cameraSlide?.Content is CameraView cv)
            {
                // 1. Tắt Timer đếm ngược 30s của MainWindow để không bị quấy rầy
                _inactivityTimer?.Stop();
                InactivityPanel.Visibility = Visibility.Collapsed;

                // 2. Tạo một cơ chế đợi (TaskCompletionSource)
                var tcs = new TaskCompletionSource<List<string>>();

                // 3. Đăng ký sự kiện: "Khi nào chụp xong, hãy báo cho tôi"
                Action<List<string>> handler = null;
                handler = (photoPaths) =>
                {
                    cv.OnCaptureFinished -= handler; // Gỡ bỏ sự kiện sau khi xong để tránh rác RAM
                    tcs.SetResult(photoPaths);       // Giải phóng lệnh đợi bên dưới
                };
                cv.OnCaptureFinished += handler;

                // 4. RA LỆNH CHO CAMERA BẮT ĐẦU CHỤP (Cái này sẽ chạy 3..2..1.. Flash..)
                cv.StartCapture();

                // 5. ĐỨNG ĐỢI tại đây cho đến khi có danh sách ảnh trả về
                var finalPhotos = await tcs.Task;

                // 6. CHỤP XONG RỒI MỚI NHẢY TRANG
                Navigate(3);

                // Gợi ý: Sau này ông có thể truyền 'finalPhotos' vào View ở Index 3 tại đây
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
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Gọi hàm Stop của Service để dọn dẹp Camera
            _cameraService?.Stop();

            // Dừng luôn timer đếm ngược cho chắc
            _inactivityTimer?.Stop();

            base.OnClosing(e);
        }

    }
}