using System;
using System.ComponentModel;
using System.Windows;
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
                _zoomLevel = Math.Max(0.5, Math.Min(3.0, value));
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
                _rotationAngle = value;
                OnPropertyChanged(nameof(RotationAngle));
            }
        }

        public DocumentPreviewViewModel(BitmapImage? image, string documentName = "", string documentType = "", double containerWidth = 760, double containerHeight = 520)
        {
            PreviewImage = image;
            DocumentName = documentName;
            DocumentType = documentType;
            
            // Calculate initial zoom to fit screen
            if (image != null && image.PixelWidth > 0 && image.PixelHeight > 0)
            {
                double widthRatio = containerWidth / image.PixelWidth;
                double heightRatio = containerHeight / image.PixelHeight;
                _fitToScreenZoom = Math.Min(widthRatio, heightRatio) * 0.95; // 95% to add some margin
                ZoomLevel = _fitToScreenZoom;
            }
            
            ZoomInCommand = new RelayCommand<object?>(_ => { if (PreviewImage != null) ZoomLevel += 0.1; }, _ => PreviewImage != null);
            ZoomOutCommand = new RelayCommand<object?>(_ => { if (PreviewImage != null) ZoomLevel -= 0.1; }, _ => PreviewImage != null);
            ZoomResetCommand = new RelayCommand<object?>(_ => { if (PreviewImage != null) ZoomLevel = _fitToScreenZoom; }, _ => PreviewImage != null);
            RotateCommand = new RelayCommand<object?>(_ => { if (PreviewImage != null) RotationAngle = (RotationAngle + 90) % 360; }, _ => PreviewImage != null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

