using System.Collections.Generic;

namespace Ambii.Models
{
    public class FrameConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }

        // --- THÊM MỚI ĐOẠN NÀY ---
        public bool IsGeneric { get; set; } // Để Code biết là quét folder hay lấy 1 file duy nhất
        public string StylesFolder { get; set; } // Path tới folder chứa nhiều skin (Dùng cho Generic)
        public string FramePath { get; set; } // Path tới file .png cụ thể (Dùng cho Special)
        // -------------------------

        public double CameraWidth { get; set; }
        public double CameraHeight { get; set; }
        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }
        public int DPI { get; set; }

        public List<PhotoSlot> Slots { get; set; } = new List<PhotoSlot>();
        public FooterAreaConfig FooterArea { get; set; }
    }

    public class PhotoSlot
    {
        public int Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    // Class con tương ứng với cái ngoặc nhọn "FooterArea" trong JSON
    public class FooterAreaConfig
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}