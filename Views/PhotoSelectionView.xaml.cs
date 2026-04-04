using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ambii.Models;
using Ambii.Services;

namespace Ambii.Views
{
    public partial class PhotoSelectionView : UserControl
    {
        private PhotoSelectionService _service = new PhotoSelectionService();

        public PhotoSelectionView()
        {
            InitializeComponent();
            // DataContext là Service để XAML có thể Bind tới PreviewSlots, AvailablePhotos, v.v.
            this.DataContext = _service;
            ListCapturedPhotos.ItemsSource = _service.AvailablePhotos;
        }

        // TRONG FILE: PhotoSelectionView.xaml.cs
        public void ReceivePhotos(List<string> photoPaths)
        {
            _service.LoadPhotos(photoPaths);

            // Chỉ cần gọi hàm, không truyền tham số
            _service.LoadFramesFromFolder();

            if (_service.AllFrames.Count > 0)
            {
                _service.SetCurrentFrame(_service.AllFrames[0]);
            }
        }

        private void Photo_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedPhoto = (sender as FrameworkElement)?.DataContext as PhotoItem;
            if (clickedPhoto != null)
            {
                _service.ToggleSelect(clickedPhoto);
                // Service bây giờ tự động chạy UpdatePreviewLayout bên trong ToggleSelect rồi
            }
        }

        private void ListFrames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListFrames.SelectedItem is FrameConfig selected)
            {
                _service.SetCurrentFrame(selected);
            }
        }
    }
}