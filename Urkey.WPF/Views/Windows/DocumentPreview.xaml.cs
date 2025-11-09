using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Urkey.Core.Models;
using Urkey.Core.Repository;
using Urkey.Core.Services;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Windows
{
    public partial class DocumentPreview : Window
    {
        private readonly string _encryptedImagePath;
        private readonly DocumentEntry? _document;
        private string? _tempExtractedPath;
        private DocumentPreviewViewModel? _viewModel;

        public DocumentPreview(string encryptedImagePath, DocumentEntry? document = null)
        {
            InitializeComponent();
            _encryptedImagePath = encryptedImagePath;
            _document = document;
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
                bitmap.Freeze();

                // Calculate container size (accounting for margins and padding)
                // Use default size initially, will be updated when window is loaded
                double containerWidth = 760; // 800 - 40 (margins/padding)
                double containerHeight = 520; // 600 - 80 (header + margins/padding)

                _viewModel = new DocumentPreviewViewModel(
                    bitmap, 
                    _document?.Name ?? "Document Preview",
                    _document?.Type ?? string.Empty,
                    containerWidth,
                    containerHeight);
                DataContext = _viewModel;
                
                // Update zoom when window is loaded with actual size
                Loaded += (s, e) =>
                {
                    if (_viewModel != null && bitmap != null)
                    {
                        double actualWidth = ActualWidth - 60;
                        double actualHeight = ActualHeight - 100;
                        if (actualWidth > 0 && actualHeight > 0 && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                        {
                            double widthRatio = actualWidth / bitmap.PixelWidth;
                            double heightRatio = actualHeight / bitmap.PixelHeight;
                            double fitZoom = Math.Min(widthRatio, heightRatio) * 0.95;
                            _viewModel.ZoomLevel = fitZoom;
                        }
                    }
                };
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

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_document == null) return;

            try
            {
                var editWindow = new AddDocument(_document)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() == true && editWindow.Document != null)
                {
                    // Update document properties directly
                    _document.Name = editWindow.Document.Name;
                    _document.Type = editWindow.Document.Type;
                    _document.Number = editWindow.Document.Number;
                    _document.Issuer = editWindow.Document.Issuer;
                    _document.Notes = editWindow.Document.Notes;
                    _document.ExpiryDate = editWindow.Document.ExpiryDate;

                    var repo = new VaultRepository();
                    
                    // If new image selected, save it
                    if (!string.IsNullOrWhiteSpace(editWindow.Document.ExternalImagePath) && 
                        File.Exists(editWindow.Document.ExternalImagePath) &&
                        editWindow.Document.ExternalImagePath != _document.ExternalImagePath)
                    {
                        string originalImagePath = editWindow.Document.ExternalImagePath;
                        string vaultDirectory = repo.GetVaultDirectory();
                        string encryptedImagePath = FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);
                        _document.ExternalImagePath = encryptedImagePath;
                        
                        // Reload the image
                        LoadImage();
                    }
                    else
                    {
                        // Update the view model with new name/type
                        if (_viewModel != null)
                        {
                            _viewModel.DocumentName = _document.Name;
                            _viewModel.DocumentType = _document.Type;
                        }
                    }

                    // Save the vault
                    var vault = repo.LoadVault();
                    repo.SaveVault(vault);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error editing document: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, 
                    MessageBoxOptions.None);
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_document == null) return;

            var result = MessageBox.Show(
                this,
                $"Are you sure you want to delete '{_document.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.None);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete using DocumentsViewModel
                    var vm = new DocumentsViewModel();
                    vm.DeleteCommand.Execute(_document);
                    
                    // Close the preview window after deletion
                    OnCloseClick(sender, e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error deleting document: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, 
                        MessageBoxOptions.None);
                }
            }
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            if (_document == null || string.IsNullOrEmpty(_document.ExternalImagePath)) return;

            try
            {
                if (_tempExtractedPath == null || !File.Exists(_tempExtractedPath))
                {
                    MessageBox.Show(this, "Image not loaded yet.", "Warning", 
                        MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, 
                        MessageBoxOptions.None);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    FileName = $"{_document.Name ?? "Document"}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(_tempExtractedPath, saveDialog.FileName, overwrite: true);
                    MessageBox.Show(this, "Document downloaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, 
                        MessageBoxOptions.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error downloading document: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, 
                    MessageBoxOptions.None);
            }
        }
    }
}
