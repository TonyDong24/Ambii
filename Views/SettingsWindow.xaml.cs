using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ambii.Models;
using Ambii.Services;
using Microsoft.Win32;
using System.IO;
using AForge.Video.DirectShow;

namespace Ambii.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings = new();
        private FilterInfoCollection videoDevices;
        public Action OnSettingsSaved { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCameraOptions();
            LoadSettings();
        }
        // Trong AppSettings.cs bạn thêm:
        // public string AdminPassword { get; set; } = "1234";

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Lấy mật khẩu từ Settings hoặc đặt cứng (Hard-coded) nếu muốn nhanh
            string correctPassword = _settings.AdminPassword ?? "admin123";

            if (TxtAdminPassword.Password == correctPassword)
            {
                // Nếu đúng: Ẩn lớp phủ, hiện nội dung setting
                LoginOverlay.Visibility = Visibility.Collapsed;
                MainSettingsContent.Visibility = Visibility.Visible;
            }
            else
            {
                // Nếu sai: Dùng DarkMsg bạn vừa tạo để báo lỗi
                DarkMsg.Show("LỖI", "Mật khẩu không chính xác!");
                TxtAdminPassword.Clear();
                TxtAdminPassword.Focus();
            }
        }

        // Cho phép nhấn Enter để đăng nhập nhanh
        private void TxtAdminPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) BtnLogin_Click(null, null);
        }

        // Thoát nếu không biết mật khẩu
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadCameraOptions()
        {
            CmbCamera.Items.Clear();

            try
            {
                // 2. Quét danh sách camera thực tế từ phần cứng
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    CmbCamera.Items.Add("No camera detected");
                    CmbCamera.IsEnabled = false;
                    return;
                }
                // 3. Đưa tên các camera vào ComboBox
                foreach (FilterInfo device in videoDevices)
                {
                    CmbCamera.Items.Add(device.Name);
                }

                CmbCamera.IsEnabled = true;
                CmbCamera.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning cameras: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            _settings = SettingsService.Load();

            if (!string.IsNullOrWhiteSpace(_settings.CameraName))
            {
                CmbCamera.SelectedItem = CmbCamera.Items
                    .Cast<object>()
                    .FirstOrDefault(x => x?.ToString() == _settings.CameraName);
            }

            SelectComboItemByText(CmbCountdown, _settings.CountdownSeconds.ToString());
            SelectComboItemByText(CmbPhotoCount, _settings.PhotoCount.ToString());

            TxtSaveFolder.Text = _settings.SaveFolder;
            ChkAutoPrint.IsChecked = _settings.AutoPrint;
            ChkMirrorPreview.IsChecked = _settings.MirrorPreview;
            ChkDebugMode.IsChecked = _settings.IsDebugMode;
        }

        private void SelectComboItemByText(ComboBox comboBox, string text)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content?.ToString() == text)
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settings.CameraName = CmbCamera.SelectedItem?.ToString() ?? "Default Camera";
            _settings.CountdownSeconds = int.Parse(((ComboBoxItem)CmbCountdown.SelectedItem).Content.ToString()!);
            _settings.PhotoCount = int.Parse(((ComboBoxItem)CmbPhotoCount.SelectedItem).Content.ToString()!);
            _settings.SaveFolder = TxtSaveFolder.Text.Trim();
            _settings.AutoPrint = ChkAutoPrint.IsChecked == true;
            _settings.MirrorPreview = ChkMirrorPreview.IsChecked == true;

            // THÊM DÒNG NÀY ĐỂ LƯU TRẠNG THÁI DEBUG MODE
            _settings.IsDebugMode = ChkDebugMode.IsChecked == true;
            SettingsService.Save(_settings);
            OnSettingsSaved?.Invoke();


            MessageBox.Show("Settings saved successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();

            if (dialog.ShowDialog() == true)
            {
                TxtSaveFolder.Text = dialog.FolderName;
            }
        }
    }
}