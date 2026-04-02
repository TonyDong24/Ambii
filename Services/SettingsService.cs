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
        public List<FrameConfig> Frames { get; private set; } = new List<FrameConfig>();

        public void LoadConfigs()
        {
            try
            {
                // 1. Dùng list tạm để tránh việc UI bị trống hình khi đang load
                var tempFrames = new List<FrameConfig>();
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configsRoot = Path.Combine(baseDir, "Configs");

                // Quét folder Generic (Phổ thông)
                ScanSubFolder(Path.Combine(configsRoot, "Generic"), true, tempFrames);

                // Quét folder Special (Đặc biệt)
                ScanSubFolder(Path.Combine(configsRoot, "Special"), false, tempFrames);

                // 2. Chỉ cập nhật danh sách chính khi đã quét xong xuôi
                Frames = tempFrames;
                System.Diagnostics.Debug.WriteLine($"[Config] Live Update thành công: {Frames.Count} frames.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] Lỗi tổng khi quét folder: {ex.Message}");
            }
        }

        private void ScanSubFolder(string folderPath, bool isGeneric, List<FrameConfig> targetList)
        {
            if (!Directory.Exists(folderPath)) return;

            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    // SỬ DỤNG FILESHARE.READWRITE ĐỂ ADMIN VỪA SAVE APP VẪN ĐỌC ĐƯỢC
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();

                        // KIỂM TRA 1: Nếu file đang trống (do Admin vừa xóa trắng để gõ lại) thì bỏ qua
                        if (string.IsNullOrWhiteSpace(json)) continue;

                        // KIỂM TRA 2: Deserialize an toàn
                        var config = System.Text.Json.JsonSerializer.Deserialize<FrameConfig>(json);

                        if (config != null)
                        {
                            config.IsGeneric = isGeneric;

                            // TỰ ĐỘNG GÁN PATH NẾU TRONG JSON ĐANG ĐỂ TRỐNG (Cho tiện quản lý)
                            if (isGeneric && string.IsNullOrEmpty(config.StylesFolder))
                                config.StylesFolder = Path.Combine("Assets", "Frames", config.Id);

                            if (!isGeneric && string.IsNullOrEmpty(config.FramePath))
                                config.FramePath = Path.Combine("Assets", "Frames", "Special", $"{config.Id}.png");

                            targetList.Add(config);
                        }
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Nếu Admin gõ sai cú pháp JSON (thiếu dấu phẩy, ngoặc...), App chỉ bỏ qua file này
                    System.Diagnostics.Debug.WriteLine($"[JSON ERROR] File {Path.GetFileName(file)} sai định dạng, đang đợi Admin sửa xong...");
                }
                catch (Exception ex)
                {
                    // Các lỗi truy cập file khác
                    System.Diagnostics.Debug.WriteLine($"[FILE ERROR] Không thể đọc {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
    }
}