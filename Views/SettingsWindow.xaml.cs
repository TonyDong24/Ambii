using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ambii.Models;
using Ambii.Services;
using Microsoft.Win32;
using System.IO;

namespace Ambii.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings = new();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCameraOptions();
            LoadSettings();
        }

        private void LoadCameraOptions()
        {
            CmbCamera.Items.Clear();

            // Tạm thời mock danh sách camera
            CmbCamera.Items.Add("Default Camera");
            CmbCamera.Items.Add("Sony A6400");
            CmbCamera.Items.Add("Canon EOS");
            CmbCamera.Items.Add("Webcam HD");

            CmbCamera.SelectedIndex = 0;
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

            SettingsService.Save(_settings);

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