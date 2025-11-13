using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Urkey.WPF.Commands;

namespace Urkey.WPF.ViewModels
{
    public class DocumentPreviewViewModel : INotifyPropertyChanged
    {
        private BitmapImage? _previewImage;
        public BitmapImage? PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged(nameof(PreviewImage));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private double _zoomLevel = 1.0;
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                // Clamp zoom to reasonable bounds [0.05, 3.0]
                _zoomLevel = Math.Max(0.05, Math.Min(3.0, value));
                OnPropertyChanged(nameof(ZoomLevel));
            }
        }

        private string _documentName = string.Empty;
        public string DocumentName
        {
            get => _documentName;
            set
            {
                _documentName = value;
                OnPropertyChanged(nameof(DocumentName));
            }
        }

        private string _documentType = string.Empty;
        public string DocumentType
        {
            get => _documentType;
            set
            {
                _documentType = value;
                OnPropertyChanged(nameof(DocumentType));
            }
        }

        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomResetCommand { get; }
        public ICommand RotateCommand { get; }

        private double _fitToScreenZoom = 1.0;

        private double _rotationAngle = 0.0;
        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                // Normalize rotation to 0-360 range
                _rotationAngle = value % 360;
                if (_rotationAngle < 0) _rotationAngle += 360;
                OnPropertyChanged(nameof(RotationAngle));
            }
        }

        private bool _showDebug = false;
        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                _showDebug = value;
                OnPropertyChanged(nameof(ShowDebug));
            }
        }

        public DocumentPreviewViewModel(BitmapImage? image, string documentName = "", string documentType = "")
        {
            PreviewImage = image;
            DocumentName = documentName;
            DocumentType = documentType;

            // Set very small initial zoom (will be updated by FitToScreen)
            // This ensures the image is never too zoomed in initially
            if (image != null && image.PixelWidth > 0 && image.PixelHeight > 0)
            {
                _fitToScreenZoom = 0.05; // Minimum zoom - ensures readability
                ZoomLevel = 0.05; // Start at minimum to ensure image is always visible
            }

            ZoomInCommand = new RelayCommand<object?>(_ =>
            {
                if (PreviewImage != null) ZoomLevel += 0.1;
            }, _ => PreviewImage != null);

            ZoomOutCommand = new RelayCommand<object?>(_ =>
            {
                if (PreviewImage != null) ZoomLevel -= 0.1;
            }, _ => PreviewImage != null);

            ZoomResetCommand = new RelayCommand<object?>(_ =>
            {
                if (PreviewImage != null) ZoomLevel = _fitToScreenZoom;
            }, _ => PreviewImage != null);

            RotateCommand = new RelayCommand<object?>(_ =>
            {
                if (PreviewImage != null) RotationAngle = (RotationAngle + 90) % 360;
            }, _ => PreviewImage != null);
        }

        /// <summary>
        /// Calculates and sets zoom to fit the image within the ScrollViewer viewport.
        /// Uses viewport dimensions (fallback to ActualWidth/Height if viewport is 0).
        /// Formula: fitZoom = min(viewportW / imgPixelW, viewportH / imgPixelH) * 0.95, clamped [0.05, 1.0]
        /// </summary>
        public void FitToScreen(ScrollViewer scrollViewer)
        {
            if (PreviewImage == null || scrollViewer == null)
                return;

            try
            {
                // Get viewport dimensions (preferred) or fallback to actual dimensions
                double viewportWidth = scrollViewer.ViewportWidth;
                double viewportHeight = scrollViewer.ViewportHeight;

                if (viewportWidth <= 0 || viewportHeight <= 0)
                {
                    viewportWidth = scrollViewer.ActualWidth;
                    viewportHeight = scrollViewer.ActualHeight;
                }

                if (viewportWidth <= 0 || viewportHeight <= 0 || PreviewImage.PixelWidth <= 0 || PreviewImage.PixelHeight <= 0)
                    return;

                // Calculate fit zoom: min of width/height ratios
                // Use 0.90 multiplier (instead of 0.95) to ensure more margin and better readability
                double widthRatio = viewportWidth / PreviewImage.PixelWidth;
                double heightRatio = viewportHeight / PreviewImage.PixelHeight;
                double fitZoom = Math.Min(widthRatio, heightRatio) * 0.90;

                // Clamp to [0.05, 1.0] - never zoom beyond 100%, never zoom too small
                // Ensure we never exceed 1.0 (actual size) to prevent over-zooming
                fitZoom = Math.Max(0.05, Math.Min(1.0, fitZoom));
                
                // Additional safety: if calculated zoom is still too large (>0.8), cap it further
                // This ensures very large images are always readable
                if (fitZoom > 0.8 && PreviewImage.PixelWidth > 2000)
                {
                    fitZoom = Math.Min(fitZoom, 0.5); // Cap at 50% for very large images
                }

                _fitToScreenZoom = fitZoom;
                ZoomLevel = fitZoom;
            }
            catch
            {
                // Silently fail if calculation errors occur
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
