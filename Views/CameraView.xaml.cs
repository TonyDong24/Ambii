using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation; // Cực kỳ quan trọng để chạy Flash
using Ambii.Models;
using Ambii.Services;

namespace Ambii.Views
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    public partial class CameraView : UserControl
    {
        // 1. Khai báo Delegate có kèm List<string> để gửi danh sách ảnh về MainWindow
        public event Action<List<string>> OnCaptureFinished;
        private CameraService _cameraService;
        public CameraView()
        {
            InitializeComponent();
        }

        public void Setup(FrameConfig config, CameraService service)
        {
            _cameraService = service;
            // Ép kích thước theo JSON ông đã định nghĩa
            CameraArea.Width = config.CameraWidth;
            CameraArea.Height = config.CameraHeight;

            // Báo cho Service biết tỉ lệ để nó bắt đầu Crop ảnh từ gốc
            _cameraService.SetActiveConfig(config);

            // CHỐT: Báo cho CameraService biết Config hiện tại để nó Crop từ gốc
            // Giả sử ông có tham chiếu đến cameraService
            // cameraService.SetActiveConfig(config); 

            var settings = SettingsService.Load();
            int totalPhotos = settings?.PhotoCount ?? 4;
            txtTotalPhotos.Text = totalPhotos.ToString();

            // Load Frame ảnh tương ứng
            string framePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Frames", $"{config.Id}.png");
            if (System.IO.File.Exists(framePath))
            {
                ImgFrameOverlay.Source = new BitmapImage(new Uri(framePath));
            }
        }

        // Hàm này để MainWindow gọi và đổ hình vào
        public void UpdatePreview(BitmapSource source)
        {
            // Thêm dòng này nếu ông thấy ảnh bị mờ hoặc răng cưa (tùy chọn)
            // RenderOptions.SetBitmapScalingMode(ImgLivePreview, BitmapScalingMode.HighQuality);
            ImgLivePreview.Source = source;
        }
        // 2. Hàm StartCapture "tự thân vận động" - Không cần truyền tham số từ ngoài
        public async void StartCapture()
        {
            // Đọc settings ngay tại chỗ để lấy thông số mới nhất từ file JSON
            var settings = SettingsService.Load();

            // Fallback (Phòng hờ file settings bị trống hoặc lỗi)
            int totalPhotos = settings?.PhotoCount ?? 4;
            int countdownSecs = settings?.CountdownSeconds ?? 3;
            string saveDir = settings?.SaveFolder ?? "Photos";

            // HIỂN THỊ TỔNG SỐ ẢNH LÊN UI
            txtTotalPhotos.Text = totalPhotos.ToString();

            List<string> capturedPaths = new List<string>();

            for (int i = 1; i <= totalPhotos; i++)
            {
                // CẬP NHẬT SỐ THỨ TỰ ẢNH ĐANG CHỤP
                txtCurrentPhoto.Text = (i-1).ToString();

                // --- BƯỚC 1: ĐẾM NGƯỢC ---
                txtCountdown.Visibility = Visibility.Visible;
                for (int s = countdownSecs; s > 0; s--)
                {
                    txtCountdown.Text = s.ToString();
                    await Task.Delay(1000); // 1 giây mỗi số
                }
                txtCountdown.Visibility = Visibility.Collapsed;

                // --- BƯỚC 2: NHÁY FLASH ---
                TriggerFlash();

                // --- BƯỚC 3: CHỤP & LƯU ---
                // CHỐT: Lấy trực tiếp frame RAW từ Service (Ảnh 1920x... đã được crop chuẩn trung tâm)
                var currentFrame = _cameraService?.GetLatestRawFrame();

                if (currentFrame != null)
                {
                    // Fix lỗi luồng khi lưu ảnh
                    if (currentFrame.CanFreeze) currentFrame.Freeze();

                    // Gọi hàm lưu đơn giản (tôi sẽ viết ở dưới), không cần truyền config vào tính lại nữa
                    string filePath = await SavePhotoSimple(currentFrame, i, saveDir);
                    if (!string.IsNullOrEmpty(filePath)) capturedPaths.Add(filePath);
                }

                // Nghỉ một chút để khách kịp đổi dáng (pose)
                await Task.Delay(500);
            }

            // --- BƯỚC 4: BÁO CÁO KẾT QUẢ ---
            // Gửi danh sách đường dẫn ảnh về cho MainWindow xử lý hiển thị ở Index 3
            await Task.Delay(200);
            OnCaptureFinished?.Invoke(capturedPaths);
        }

        private void TriggerFlash()
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            FlashOverlay.BeginAnimation(OpacityProperty, anim);
        }

        private async Task<string> SavePhotoSimple(BitmapSource frame, int index, string folderName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string fileName = $"Ambii_{DateTime.Now:yyyyMMdd_HHmmss}_{index}.jpg";
                    string path = Path.Combine(dir, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        var encoder = new JpegBitmapEncoder { QualityLevel = 98 }; // Chất lượng cực cao
                        encoder.Frames.Add(BitmapFrame.Create(frame));
                        encoder.Save(stream);
                    }
                    return path;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi lưu ảnh: {ex.Message}");
                    return string.Empty;
                }
            });
        }

    }
}
