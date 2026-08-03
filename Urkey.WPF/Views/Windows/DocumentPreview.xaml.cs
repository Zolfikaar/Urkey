using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class DocumentPreview : Window
    {
        private readonly string _encryptedImagePath;
        private readonly DocumentEntry? _document;
        private string? _tempExtractedPath;
        private DocumentPreviewViewModel? _viewModel;
        private readonly VaultService _vaultService = new VaultService();
        private EventHandler? _layoutHandler;

        public DocumentPreview(string encryptedImagePath, DocumentEntry? document = null)
        {
            InitializeComponent();
            _encryptedImagePath = encryptedImagePath;
            _document = document;
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadImage();
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cleanup: Remove layout handler to prevent leaks
            if (_layoutHandler != null && ImageScrollViewer != null)
            {
                ImageScrollViewer.LayoutUpdated -= _layoutHandler;
            }

            FileHelper.TryDeleteTempFile(_tempExtractedPath);
            _tempExtractedPath = null;
        }

        private void LoadImage()
        {
            try
            {
                // Validate encrypted image path before attempting extraction
                if (string.IsNullOrWhiteSpace(_encryptedImagePath))
                {
                    ToastService.Error("Document image path is not set. The document may not have been saved properly.");
                    return;
                }

                _tempExtractedPath = FileHelper.ExtractDocumentImage(_encryptedImagePath);

                if (string.IsNullOrWhiteSpace(_tempExtractedPath) || !File.Exists(_tempExtractedPath))
                {
                    ToastService.Error($"Failed to extract document image. The encrypted file may not exist or may be corrupted.\n\nPath: {_encryptedImagePath}");
                    return;
                }

                // Load bitmap with EXIF handling and downscaling
                var (bitmap, exifRotation) = LoadOptimizedBitmap(_tempExtractedPath);

                // Create ViewModel with document info
                _viewModel = new DocumentPreviewViewModel(
                 _vaultService,
                 bitmap,
                 _document?.Name ?? "Document Preview",
                 _document?.Type ?? string.Empty);

                // Apply EXIF rotation if present
                if (exifRotation != 0)
                {
                    _viewModel.RotationAngle = exifRotation;
                }

                DataContext = _viewModel;

                // Schedule fit-to-screen calculation after layout completes
                ScheduleFitToScreen();
            }
            catch (FileNotFoundException ex)
            {
                ToastService.Warning($"Document image file not found. The file may not have been saved yet or may have been moved.\n\nDetails: {ex.Message}\n\nPath: {_encryptedImagePath}");
            }
            catch (DirectoryNotFoundException ex)
            {
                ToastService.Warning($"Document images directory not found. The application will attempt to create it.\n\nDetails: {ex.Message}");
                // Try to reload after a brief delay to allow directory creation
                Thread.Sleep(100);
                try
                {
                    LoadImage();
                }
                catch
                {
                    // If retry fails, show error
                    ToastService.Error("Failed to load document image after retry. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ToastService.Error($"Error loading image: {ex.Message}\n\nPath: {_encryptedImagePath}");
            }
        }

        /// <summary>
        /// Loads and optimizes bitmap with EXIF orientation handling and smart downscaling.
        /// Returns the bitmap and any EXIF rotation angle (0-360).
        /// </summary>
        private (BitmapImage bitmap, double exifRotation) LoadOptimizedBitmap(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            double exifRotation = 0;
            int targetMaxDimension = 2400; // Target max dimension for downscaling

            using (var stream = File.OpenRead(path))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count == 0)
                    throw new InvalidOperationException("No frames in image");

                var frame = decoder.Frames[0];
                int originalWidth = frame.PixelWidth;
                int originalHeight = frame.PixelHeight;

                // Read EXIF orientation from metadata
                // Try multiple metadata query paths for compatibility
                var metadata = frame.Metadata as BitmapMetadata;
                if (metadata != null)
                {
                    try
                    {
                        object? orientation = null;
                        
                        // Try different EXIF orientation query paths
                        string[] orientationPaths = {
                            "System.Photo.Orientation",
                            "/app1/ifd/{ushort=274}",
                            "/ifd/{ushort=274}"
                        };

                        foreach (var queryPath in orientationPaths)
                        {
                            try
                            {
                                if (metadata.ContainsQuery(queryPath))
                                {
                                    orientation = metadata.GetQuery(queryPath);
                                    if (orientation != null) break;
                                }
                            }
                            catch { /* Try next query path */ }
                        }

                        if (orientation != null)
                        {
                            ushort orientationValue = Convert.ToUInt16(orientation);
                            // Map EXIF orientation to rotation angle
                            // 1 = Normal (0°), 3 = 180°, 6 = 90° CW, 8 = 270° CW (90° CCW)
                            exifRotation = orientationValue switch
                            {
                                3 => 180,  // Rotate 180
                                6 => 90,   // Rotate 90 CW
                                8 => 270,  // Rotate 270 CW (90 CCW)
                                _ => 0     // Normal or unknown
                            };
                        }
                    }
                    catch
                    {
                        // If EXIF reading fails, continue without rotation
                    }
                }

                // Calculate downscaling if needed
                int decodeWidth = originalWidth;
                int decodeHeight = originalHeight;

                if (originalWidth > targetMaxDimension || originalHeight > targetMaxDimension)
                {
                    double scale = Math.Min(
                        targetMaxDimension / (double)originalWidth,
                        targetMaxDimension / (double)originalHeight);
                    decodeWidth = (int)(originalWidth * scale);
                    decodeHeight = (int)(originalHeight * scale);
                }

                // Initialize bitmap with optimized dimensions
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.DecodePixelWidth = decodeWidth;
                bitmap.DecodePixelHeight = decodeHeight;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }

            bitmap.Freeze();
            return (bitmap, exifRotation);
        }

        /// <summary>
        /// Schedules fit-to-screen calculation after layout completes.
        /// Uses LayoutUpdated event (unsubscribes after first run) as fallback.
        /// </summary>
        private void ScheduleFitToScreen()
        {
            if (ImageScrollViewer == null || _viewModel == null)
                return;

            bool fitCalculated = false;

            _layoutHandler = (_, _) =>
            {
                if (!fitCalculated && ImageScrollViewer.ViewportWidth > 0 && ImageScrollViewer.ViewportHeight > 0)
                {
                    CalculateFitToScreen();
                    fitCalculated = true;
                    // Unsubscribe after first successful calculation
                    if (_layoutHandler != null)
                    {
                        ImageScrollViewer.LayoutUpdated -= _layoutHandler;
                        _layoutHandler = null;
                    }
                }
            };

            ImageScrollViewer.LayoutUpdated += _layoutHandler;

            // Also handle window resize
            SizeChanged += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_viewModel != null)
                        _viewModel.FitToScreen(ImageScrollViewer);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };

            // Multiple fallbacks to ensure fit calculation runs
            // Try after Loaded priority
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ImageScrollViewer != null && _viewModel != null)
                {
                    ImageScrollViewer.UpdateLayout();
                    if (ImageScrollViewer.ViewportWidth > 0 && ImageScrollViewer.ViewportHeight > 0)
                    {
                        CalculateFitToScreen();
                        if (_layoutHandler != null)
                        {
                            ImageScrollViewer.LayoutUpdated -= _layoutHandler;
                            _layoutHandler = null;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            // Additional fallback: Try after ContentRendered
            ContentRendered += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ImageScrollViewer != null && _viewModel != null)
                    {
                        ImageScrollViewer.UpdateLayout();
                        if (ImageScrollViewer.ViewportWidth > 0 && ImageScrollViewer.ViewportHeight > 0)
                        {
                            CalculateFitToScreen();
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        /// <summary>
        /// Calculates and applies fit-to-screen zoom using ScrollViewer viewport dimensions.
        /// </summary>
        private void CalculateFitToScreen()
        {
            if (_viewModel?.PreviewImage == null || ImageScrollViewer == null)
                return;

            // Force layout update to ensure viewport dimensions are accurate
            ImageScrollViewer.UpdateLayout();
            
            // Double-check viewport has valid dimensions
            if (ImageScrollViewer.ViewportWidth <= 0 || ImageScrollViewer.ViewportHeight <= 0)
            {
                // If viewport is still 0, use actual dimensions
                if (ImageScrollViewer.ActualWidth <= 0 || ImageScrollViewer.ActualHeight <= 0)
                    return; // Can't calculate yet
            }

            _viewModel.FitToScreen(ImageScrollViewer);
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tempExtractedPath == null || !File.Exists(_tempExtractedPath))
                {
                    ToastService.Warning("Image not loaded yet.");
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
                    ToastService.Success("Image exported successfully.");
                }
            }
            catch (Exception ex)
            {
                ToastService.Error($"Error exporting image: {ex.Message}");
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_document == null) return;

            try
            {
                var editWindow = new AddDocument(_document) { Owner = this };

                if (editWindow.ShowDialog() == true && editWindow.Document != null)
                {
                    _document.Name = editWindow.Document.Name;
                    _document.Type = editWindow.Document.Type;
                    _document.Number = editWindow.Document.Number;
                    _document.Issuer = editWindow.Document.Issuer;
                    _document.Notes = editWindow.Document.Notes;
                    _document.ExpiryDate = editWindow.Document.ExpiryDate;

                    if (!string.IsNullOrWhiteSpace(editWindow.Document.ExternalImagePath) &&
                        File.Exists(editWindow.Document.ExternalImagePath) &&
                        editWindow.Document.ExternalImagePath != _document.ExternalImagePath)
                    {
                        string originalImagePath = editWindow.Document.ExternalImagePath;
                        string vaultDirectory = _vaultService.GetVaultDirectory();

                        string encryptedImagePath =
                            FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);

                        _document.ExternalImagePath = encryptedImagePath;
                        LoadImage();
                    }
                    else if (_viewModel != null)
                    {
                        _viewModel.DocumentName = _document.Name;
                        _viewModel.DocumentType = _document.Type;
                    }

                    _vaultService.Save();
                }
            }
            catch (Exception ex)
            {
                ToastService.Error($"Error editing document: {ex.Message}");
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_document == null) return;

            if (!EntryDialogHelper.ConfirmDelete(_document.Name ?? "document"))
                return;

            try
            {
                var vault = _vaultService.LoadVault();

                var docToDelete = vault.Entries
                    .OfType<DocumentEntry>()
                    .FirstOrDefault(d => d.Type == _document.Type);

                if (docToDelete != null)
                {
                    if (!string.IsNullOrEmpty(docToDelete.ExternalImagePath) &&
                        File.Exists(docToDelete.ExternalImagePath))
                    {
                        try { File.Delete(docToDelete.ExternalImagePath); } catch { }
                    }

                    vault.Entries.Remove(docToDelete);
                    _vaultService.Save();
                }

                Close();
            }
            catch (Exception ex)
            {
                ToastService.Error($"Error deleting document: {ex.Message}");
            }
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            if (_document == null || string.IsNullOrEmpty(_document.ExternalImagePath)) return;

            try
            {
                if (_tempExtractedPath == null || !File.Exists(_tempExtractedPath))
                {
                    ToastService.Warning("Image not loaded yet.");
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
                    ToastService.Success("Document downloaded successfully!");
                }
            }
            catch (Exception ex)
            {
                ToastService.Error($"Error downloading document: {ex.Message}");
            }
        }
    }
}
