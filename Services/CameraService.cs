using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using Ambii.Models;

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
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (_videoSource == null) return;

                // Dùng Clone để tránh xung đột dữ liệu giữa các luồng
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    // --- PHẦN 1: LƯU ẢNH RAW (DÙNG ĐỂ CHỤP) ---
                    // Ảnh này giữ nguyên bản gốc, không lật gương để khi chụp ảnh không bị ngược chữ
                    var rawSource = ConvertToBitmapSource(bitmap);
                    rawSource.Freeze(); // Quan trọng: Phải Freeze để dùng được ở thread khác
                    _latestRawFrame = rawSource;

                    // --- PHẦN 2: LƯU ẢNH PREVIEW (DÙNG ĐỂ HIỂN THỊ) ---
                    // Chỉ lật gương cho ảnh hiển thị trên màn hình để khách soi gương cho tự nhiên
                    if (_currentSettings != null && _currentSettings.MirrorPreview)
                    {
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }

                    var previewSource = ConvertToBitmapSource(bitmap);
                    previewSource.Freeze();

                    // Đẩy ảnh đã lật gương ra UI
                    NewFrameAvailable?.Invoke(this, previewSource);
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
                // 1. Hủy đăng ký sự kiện ngay để không nhận thêm frame nào nữa
                _videoSource.NewFrame -= VideoSource_NewFrame;

                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    // 2. Chờ tối đa 1 giây để camera kịp đóng (tránh treo máy)
                    _videoSource.WaitForStop();
                }
                _videoSource = null;
            }
        }

        // Hàm bổ trợ convert để WPF hiển thị được
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96,
                System.Windows.Media.PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }
        public BitmapSource GetLatestRawFrame()
        {
            return _latestRawFrame;
        }
    }
}