using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Urkey.Core.Services;

namespace Urkey.WPF.Views.Windows
{
    public partial class DocumentPreview : Window
    {
        private readonly string _encryptedImagePath;
        private string? _tempExtractedPath;

        public DocumentPreview(string encryptedImagePath)
        {
            InitializeComponent();
            _encryptedImagePath = encryptedImagePath;
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                _tempExtractedPath = FileHelper.ExtractDocumentImage(_encryptedImagePath);

                if (!File.Exists(_tempExtractedPath))
                {
                    MessageBox.Show("Failed to extract document image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_tempExtractedPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                DocumentImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tempExtractedPath == null || !File.Exists(_tempExtractedPath))
                {
                    MessageBox.Show("Image not loaded yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                    FileName = "DocumentImage"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(_tempExtractedPath, saveDialog.FileName, overwrite: true);
                    MessageBox.Show("Image exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // نحذف الصورة المؤقتة إذا موجودة
                if (_tempExtractedPath != null && File.Exists(_tempExtractedPath))
                    File.Delete(_tempExtractedPath);
            }
            catch { /* تجاهل الخطأ */ }

            Close();
        }
    }
}
