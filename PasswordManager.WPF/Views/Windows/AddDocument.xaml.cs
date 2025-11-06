using PasswordManager.Core.Models;
using PasswordManager.Core.Repository;
using PasswordManager.Core.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PasswordManager.WPF.Views.Windows
{
    public partial class AddDocument : Window
    {
        private const long MaxImageSize = 500 * 1024; // 500 KB
        private string? _selectedImagePath;

        public AddDocument()
        {
            InitializeComponent();
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";
        }

        // خصائص تسهّل الوصول إلى القيم
        public string DocumentName => DocumentNameTextBox.Text;
        public string Type => TypeTextBox.Text;
        public string Number => NumberTextBox.Text;
        public string Issuer => IssuerTextBox.Text;
        public string Expiry => ExpiryTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // لتعبئة القيم عند الحاجة
        public void SetValues(string documentName = "", string type = "", string number = "", string issuer = "", string expiry = "", string notes = "")
        {
            DocumentNameTextBox.Text = documentName;
            TypeTextBox.Text = type;
            NumberTextBox.Text = number;
            IssuerTextBox.Text = issuer;
            ExpiryTextBox.Text = expiry;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentNameTextBox.Text))
            {
                MessageBox.Show("Document Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var repo = new VaultRepository(); // المسار يحدد تلقائيًا إلى AppData
                var vault = repo.LoadVault();

                var doc = new DocumentEntry
                {
                    Title = DocumentName,
                    Type = Type,
                    Number = Number,
                    Notes = Notes,
                };

                // نحفظ الصورة إذا كانت موجودة
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    string vaultDir = Path.GetDirectoryName(repo.GetVaultPath())!;
                    string encryptedImagePath = FileHelper.SaveDocumentImage(_selectedImagePath, vaultDir);
                    doc.ExternalImagePath = encryptedImagePath;
                    doc.FileName = System.IO.Path.GetFileName(_selectedImagePath);
                }

                // نضيف الإدخال إلى Vault ونحفظ
                repo.AddEntry(doc);

                MessageBox.Show("Document saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
