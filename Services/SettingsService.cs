using System.IO;
using System.Text.Json;
using Ambii.Models;

namespace Ambii.Services
{
    public static class SettingsService
    {
        private static readonly string FilePath = "appsettings.json";

        public static AppSettings Load()
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }
    }

    public class ConfigService
    {
        public List<FrameConfig> Frames { get; private set; }

        public void LoadConfigs()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "frames_config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Frames = JsonSerializer.Deserialize<List<FrameConfig>>(json);
            }
        }
    }
}