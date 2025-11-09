using Urkey.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Urkey.WPF.Views.Windows
{
    public partial class AddDocument : Window
    {
        private const long MaxImageSize = 500 * 1024; // 500 KB
        private string? _selectedImagePath;
        private string? _originalImagePath; // Original encrypted image path when editing
        private readonly DocumentEntry? _editDocument;

        // ⬅️ الوثيقة الناتجة عن الإضافة
        public DocumentEntry? Document { get; private set; }

        public AddDocument(DocumentEntry? documentToEdit = null)
        {
            InitializeComponent();
            _editDocument = documentToEdit;
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
            Loaded += OnLoaded;
            
            if (_editDocument != null)
            {
                Title = "Edit Document";
                LoadDocumentData();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ApplyTheme();

        private void ApplyTheme()
        {
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";
            // theme logic later
        }

        private void LoadDocumentData()
        {
            if (_editDocument == null) return;

            DocumentNameTextBox.Text = _editDocument.Name;
            TypeTextBox.Text = _editDocument.Type;
            NumberTextBox.Text = _editDocument.Number;
            IssuerTextBox.Text = _editDocument.Issuer;
            NotesTextBox.Text = _editDocument.Notes;
            
            if (_editDocument.ExpiryDate.HasValue)
            {
                ExpiryTextBox.Text = _editDocument.ExpiryDate.Value.ToString("dd/MM/yyyy");
            }

            // Load image if exists
            if (!string.IsNullOrEmpty(_editDocument.ExternalImagePath) && 
                System.IO.File.Exists(_editDocument.ExternalImagePath))
            {
                _originalImagePath = _editDocument.ExternalImagePath; // Store original encrypted path
                try
                {
                    string tempPath = Urkey.Core.Services.FileHelper.ExtractDocumentImage(_editDocument.ExternalImagePath);
                    if (System.IO.File.Exists(tempPath))
                    {
                        // Don't set _selectedImagePath here - only set it if user selects a new image
                        SelectedImageName.Text = "Current image loaded";
                        
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(tempPath);
                        bitmap.DecodePixelWidth = 120;
                        bitmap.EndInit();
                        PreviewImage.Source = bitmap;
                    }
                }
                catch
                {
                    // Ignore errors loading image
                }
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentNameTextBox.Text))
            {
                MessageBox.Show("Document name is required.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly? parsed = null;
            var text = ExpiryTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && DateOnly.TryParse(text, out var d))
                parsed = d;

            // ✅ إنشاء كائن الوثيقة من بيانات المستخدم
            Document = new DocumentEntry
            {
                Name = DocumentNameTextBox.Text ?? string.Empty,
                Type = TypeTextBox.Text ?? string.Empty,
                Number = NumberTextBox.Text ?? string.Empty,
                Issuer = IssuerTextBox.Text ?? string.Empty,
                Notes = NotesTextBox.Text,
                ExpiryDate = parsed,
                ExternalImagePath = _selectedImagePath
            };

            // If editing and no new image selected, keep the original encrypted image path
            if (_editDocument != null && string.IsNullOrEmpty(_selectedImagePath))
            {
                Document.ExternalImagePath = _originalImagePath;
            }

            // ✅ علّم الصفحة الأم أن العملية تمت بنجاح
            DialogResult = true;
            Close();
        }

        private void OnBrowseImageClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dlg.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(dlg.FileName);
                if (fileInfo.Length > MaxImageSize)
                {
                    MessageBox.Show($"Selected image is too large ({fileInfo.Length / 1024} KB).\nMaximum allowed size is 500 KB.",
                                    "File too large", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedImagePath = dlg.FileName;
                SelectedImageName.Text = fileInfo.Name;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(dlg.FileName);
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
            }
        }
    }
}
