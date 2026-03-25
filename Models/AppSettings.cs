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
        public double Brightness { get; set; } = 0;
        public double Contrast { get; set; } = 1;
        public double Saturation { get; set; } = 1;
        public double Sharpness { get; set; } = 0;
        public string AdminPassword { get; set; } = "phuongduy";
        public bool IsDebugMode { get; set; } = false;
        public bool CheckSessionPermission { get; set; } = false;
    }
}