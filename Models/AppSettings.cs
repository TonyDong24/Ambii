namespace Ambii.Models
{
    public class AppSettings
    {
        public string CameraName { get; set; } = "";
        public int CountdownSeconds { get; set; } = 3;
        public int PhotoCount { get; set; } = 4;
        public bool AutoPrint { get; set; } = false;
        public string SaveFolder { get; set; } = "Photos";
        public bool MirrorPreview { get; set; } = true;
    }
}