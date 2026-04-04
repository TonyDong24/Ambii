using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using Ambii.Models;
using System.Drawing.Drawing2D;

namespace Ambii.Services
{
    public class CameraService
    {
        private VideoCaptureDevice _videoSource;
        private AppSettings _currentSettings; // Lưu cấu hình đang chạy trong RAM

        // Event này sẽ đẩy hình ảnh đã được convert sang WPF ra ngoài
        public event EventHandler<BitmapSource> NewFrameAvailable;

        // BIẾN MỚI: Lưu frame sạch để chụp ảnh
        private BitmapSource _latestRawFrame;
        private FrameConfig _activeConfig;
        private readonly object _frameLock = new object(); // Thêm cái khóa này
        public List<string> CapturedPhotoPaths { get; set; } = new List<string>();
        public void UpdateSettings(AppSettings settings)
        {
            _currentSettings = settings;
        }


        public void Start(string cameraName)
        {
            Stop();
            // Khi Start, nếu chưa có settings thì tự load (phòng hờ)
            if (_currentSettings == null) _currentSettings = SettingsService.Load();

            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var device = devices.Cast<FilterInfo>().FirstOrDefault(d => d.Name == cameraName);

            if (device != null)
            {
                _videoSource = new VideoCaptureDevice(device.MonikerString);
                if (_videoSource.VideoCapabilities.Length > 0)
                {
                    // Chọn độ phân giải có chiều rộng (Width) lớn nhất
                    _videoSource.VideoResolution = _videoSource.VideoCapabilities
                        .OrderByDescending(v => v.FrameSize.Width)
                        .First();
                }
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (_videoSource == null || _activeConfig == null) return;

            try
            {
                using (var bitmapFrame = (Bitmap)eventArgs.Frame.Clone())
                {
                    // 1. TÍNH TOÁN CROP GỐC (ĐỂ ĐẢM BẢO CHÍNH GIỮA)
                    double targetAspect = (double)_activeConfig.CameraWidth / _activeConfig.CameraHeight;
                    double sourceAspect = (double)bitmapFrame.Width / bitmapFrame.Height;

                    int cropX = 0, cropY = 0, cropW = bitmapFrame.Width, cropH = bitmapFrame.Height;

                    if (sourceAspect > targetAspect) // Video gốc rộng hơn (Crop 2 bên)
                    {
                        cropW = (int)(bitmapFrame.Height * targetAspect);
                        cropX = (bitmapFrame.Width - cropW) / 2; // CHỐT: Đây là điểm căn giữa trục ngang
                    }
                    else // Video gốc cao hơn (Crop trên dưới)
                    {
                        cropH = (int)(bitmapFrame.Width / targetAspect);
                        cropY = (bitmapFrame.Height - cropH) / 2; // CHỐT: Đây là điểm căn giữa trục dọc
                    }

                    // Thực hiện Crop trên Bitmap Gốc
                    using (var croppedBitmap = bitmapFrame.Clone(new Rectangle(cropX, cropY, cropW, cropH), bitmapFrame.PixelFormat))
                    {
                        // 2. LƯU ẢNH RAW CHẤT LƯỢNG CAO (ĐỂ CHỤP)
                        // --- Bước 2: LƯU ẢNH RAW CHẤT LƯỢNG CAO (ĐỂ CHỤP) ---
                        // CHỐT: Lật gương cho ảnh RAW nếu cài đặt MirrorPreview được bật
                        if (_currentSettings != null && _currentSettings.MirrorPreview)
                        {
                            // Lật trục X (ngang) cho ảnh gốc chất lượng cao
                            croppedBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }

                        var rawSource = ConvertToBitmapSource(croppedBitmap);
                        rawSource.Freeze();
                        lock (_frameLock)
                        {
                            _latestRawFrame = rawSource;
                        }

                        // 3. TẠO ẢNH PREVIEW NHẸ & MƯỢT (ĐỂ HIỂN THỊ)
                        // Canon M50 gửi 1920x1080 về, ta hạ xuống 640x960 (hoặc tỉ lệ tương ứng) cho mượt
                        // Tui hạ xuống còn Width=640 để nhẹ máy, Height tính theo tỉ lệ
                        int previewW = 640;
                        int previewH = (int)(previewW / targetAspect);

                        using (var bitmapPreview = new Bitmap(previewW, previewH))
                        {
                            using (var g = Graphics.FromImage(bitmapPreview))
                            {
                                // Dùng thuật toán chất lượng cao để ảnh preview trông vẫn xịn
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.DrawImage(croppedBitmap, 0, 0, previewW, previewH);
                            }

                            // Lật gương cho Preview nếu cài đặt bật
                            

                            var previewSource = ConvertToBitmapSource(bitmapPreview);
                            previewSource.Freeze();

                            // Đẩy ảnh NHẸ, ĐÃ CROP CHUẨN sang CameraView.xaml.cs
                            NewFrameAvailable?.Invoke(this, previewSource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 1. Log lỗi ra Console để ông dễ debug trong Visual Studio
                System.Diagnostics.Debug.WriteLine($"[CameraService Error]: {ex.Message}");

                // 2. Tự động ngắt kết nối ngay lập tức để tránh tràn bộ nhớ hoặc lag luồng UI
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    // Dùng SignalToStop thay vì Stop() ở đây để tránh việc gọi đệ quy gây khóa luồng
                    _videoSource.SignalToStop();

                    // Thông báo cho người dùng hoặc hệ thống biết Camera đã "ngỏm"
                    DarkMsg.Show("Lỗi Camera", "Vui lòng liên hệ nhân viên để hỗ trợ");
                }
            }
        }

        public void Stop()
        {
            if (_videoSource != null)
            {
                _videoSource.NewFrame -= VideoSource_NewFrame;
                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                }
                _videoSource = null;
            }

            // CHỈ CẦN GÁN NULL LÀ XONG, KHÔNG DÙNG DISPOSE
            if (_latestRawFrame != null)
            {
                _latestRawFrame = null;
            }
        }
        public void ClearCache()
        {
            lock (_frameLock) // Thêm cái này
            {
                _latestRawFrame = null;
            }
        }

        // Hàm bổ trợ convert để WPF hiển thị được
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            try
            {
                // 1. Xác định định dạng màu chuẩn dựa trên Bitmap gốc để tránh mất màu (đen trắng)
                System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Bgr24;
                if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb || bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
                    pf = System.Windows.Media.PixelFormats.Bgra32;

                // 2. Chốt Stride: Đây là chỗ gây ra sọc màu nếu tính sai. 
                // bitmapData.Stride là giá trị chuẩn từ bộ nhớ của Bitmap.
                double finalDpi = (_activeConfig != null && _activeConfig.DPI > 0)
                          ? _activeConfig.DPI
                          : 96; // Mặc định 96 nếu config lỗi

                // 3. Tạo BitmapSource với DPI chuẩn
                var bitmapSource = BitmapSource.Create(
                    bitmapData.Width,
                    bitmapData.Height,
                    finalDpi,
                    finalDpi, // Dùng cùng 1 chỉ số cho cả X và Y
                    pf,
                    null,
                    bitmapData.Scan0,
                    bitmapData.Stride * bitmapData.Height,
                    bitmapData.Stride);

                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        public BitmapSource GetLatestRawFrame()
        {
            lock (_frameLock)
            {
                return _latestRawFrame;
            }
        }



        public void SetActiveConfig(FrameConfig config)
        {
            _activeConfig = config;
        }
    }
}