using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
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
                    try
                    {
                        if (System.IO.File.Exists(tempPath))
                        {
                            // Don't set _selectedImagePath here - only set it if user selects a new image
                            SelectedImageName.Text = "Current image loaded";

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(tempPath);
                            bitmap.DecodePixelWidth = 120;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            PreviewImage.Source = bitmap;
                        }
                    }
                    finally
                    {
                        Urkey.Core.Services.FileHelper.TryDeleteTempFile(tempPath);
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
            DateOnly? parsed = null;
            var text = ExpiryTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && DateOnly.TryParse(text, out var d))
                parsed = d;

            Document = new DocumentEntry
            {
                Name = DocumentNameTextBox.Text?.Trim() ?? string.Empty,
                Type = TypeTextBox.Text?.Trim() ?? string.Empty,
                Number = NumberTextBox.Text?.Trim() ?? string.Empty,
                Issuer = IssuerTextBox.Text?.Trim() ?? string.Empty,
                Notes = NotesTextBox.Text,
                ExpiryDate = parsed,
                ExternalImagePath = _selectedImagePath
            };

            if (_editDocument != null)
            {
                Document.Id = _editDocument.Id;
                if (string.IsNullOrEmpty(_selectedImagePath))
                    Document.ExternalImagePath = _originalImagePath;
            }

            var validation = EntryValidator.ValidateDocument(Document);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return;
            }

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
                    ToastService.Warning(Loc.Get("Documents_Error_TooLarge"));
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
