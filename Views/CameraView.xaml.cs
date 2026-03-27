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
        public CameraView()
        {
            InitializeComponent();
        }

        public void Setup(FrameConfig config)
        {
            // Ép kích thước theo JSON ông đã định nghĩa
            CameraArea.Width = config.CameraWidth;
            CameraArea.Height = config.CameraHeight;

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
                txtCurrentPhoto.Text = i.ToString();

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
                var currentFrame = ImgLivePreview.Source as BitmapSource;
                if (currentFrame != null)
                {
                    // Đóng băng để luồng khác (Task.Run) có thể đọc được dữ liệu
                    if (currentFrame.CanFreeze) currentFrame.Freeze();

                    // Truyền thêm 'currentConfig' vào để nó biết tỉ lệ mà Crop
                    var currentConfig = FrameSelectionView.SelectedFrameData;
                    if (currentConfig == null)
                    {
                        // Nếu mất config, lấy đại kích thước ảnh gốc để không bị chia cho 0
                        currentConfig = new FrameConfig { CameraWidth = currentFrame.PixelWidth, CameraHeight = currentFrame.PixelHeight };
                    }
                    string filePath = await SavePhoto(currentFrame, i, saveDir, currentConfig);

                    if (!string.IsNullOrEmpty(filePath)) capturedPaths.Add(filePath);
                }

                // Nghỉ một chút để khách kịp đổi dáng (pose)
                await Task.Delay(500);
            }

            // --- BƯỚC 4: BÁO CÁO KẾT QUẢ ---
            // Gửi danh sách đường dẫn ảnh về cho MainWindow xử lý hiển thị ở Index 3
            OnCaptureFinished?.Invoke(capturedPaths);
        }

        private void TriggerFlash()
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            FlashOverlay.BeginAnimation(OpacityProperty, anim);
        }

        private async Task<string> SavePhoto(BitmapSource originalFrame, int index, string folderName, FrameConfig config)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. TÍNH TOÁN TỈ LỆ CROP (Giữ nguyên độ nét gốc)
                    // Giả sử Camera trả về 1920x1080, nhưng khung mong muốn là 1200x1800
                    double targetAspect = (double)config.CameraWidth / config.CameraHeight;
                    double frameAspect = (double)originalFrame.PixelWidth / originalFrame.PixelHeight;

                    int cropWidth = originalFrame.PixelWidth;
                    int cropHeight = originalFrame.PixelHeight;
                    int x = 0;
                    int y = 0;

                    if (frameAspect > targetAspect) // Ảnh gốc rộng hơn khung mục tiêu (Crop 2 bên)
                    {
                        cropWidth = (int)(originalFrame.PixelHeight * targetAspect);
                        x = (originalFrame.PixelWidth - cropWidth) / 2;
                    }
                    else // Ảnh gốc cao hơn khung mục tiêu (Crop trên dưới)
                    {
                        cropHeight = (int)(originalFrame.PixelWidth / targetAspect);
                        y = (originalFrame.PixelHeight - cropHeight) / 2;
                    }

                    // 2. THỰC HIỆN CROP TRÊN BITMAP GỐC
                    var croppedBitmap = new CroppedBitmap(originalFrame, new Int32Rect(x, y, cropWidth, cropHeight));

                    // 3. LƯU ẢNH (Chất lượng 95-100)
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string fileName = $"Ambii_{DateTime.Now:yyyyMMdd_HHmmss}_{index}.jpg";
                    string path = Path.Combine(dir, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        var encoder = new JpegBitmapEncoder { QualityLevel = 95 }; // 95 là mức cân bằng nhất
                        encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
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
