using System.IO;
using System.Text.Json;
using Ambii.Models;

namespace Ambii.Services
{
    public static class SettingsService
    {
        // Chỉnh lại đường dẫn để nó tìm vào folder Configs
        private static readonly string ConfigFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
        private static readonly string FilePath = Path.Combine(ConfigFolder, "appsettings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new AppSettings();

                // Dùng FileStream + FileShare.ReadWrite để máy lễ tân sửa qua mạng app vẫn đọc được
                using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                // Tự tạo folder Configs nếu chưa có (để File.WriteAllText không bị crash)
                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu settings: {ex.Message}");
            }
        }
    }

    public class ConfigService
    {
        public List<FrameConfig> Frames { get; private set; }

        public void LoadConfigs()
        {
            try
            {
                // Lấy đường dẫn thư mục đang chạy (Debug hoặc thư mục cài đặt)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Kết hợp thành đường dẫn: Debug/Configs/frames_config.json
                string path = Path.Combine(baseDir, "Configs", "frames_config.json");

                if (File.Exists(path))
                {
                    // Dùng FileShare.ReadWrite để máy lễ tân có thể mở sửa qua mạng mà App không bị crash
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        Frames = JsonSerializer.Deserialize<List<FrameConfig>>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu có (ví dụ file JSON sai định dạng)
                System.Diagnostics.Debug.WriteLine($"Lỗi load config: {ex.Message}");
            }
        }
    }
}