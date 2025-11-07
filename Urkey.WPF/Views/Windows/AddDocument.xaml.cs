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

        // ⬅️ الوثيقة الناتجة عن الإضافة
        public DocumentEntry? Document { get; private set; }

        public AddDocument()
        {
            InitializeComponent();
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ApplyTheme();

        private void ApplyTheme()
        {
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";
            // theme logic later
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
