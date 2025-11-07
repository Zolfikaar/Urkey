using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PasswordManager.Core.Models;
using PasswordManager.Core.Repository;
using PasswordManager.Core.Services;
using PasswordManager.WPF.Commands;
using PasswordManager.WPF.Views.Windows;

namespace PasswordManager.WPF.ViewModels
{
    public class DocumentsViewModel : INotifyPropertyChanged
    {
        private readonly VaultRepository _repo;
        private Vault _vault;

        private ObservableCollection<DocumentEntry> _documents = new();
        public ObservableCollection<DocumentEntry> Documents
        {
            get => _documents;
            set
            {
                _documents = value;
                OnPropertyChanged(nameof(Documents));
            }
        }

        private DocumentEntry? _selectedDocument;
        public DocumentEntry? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                _selectedDocument = value;
                OnPropertyChanged(nameof(SelectedDocument));
                LoadPreviewImage();
            }
        }

        private System.Windows.Media.Imaging.BitmapImage? _previewImage;
        public System.Windows.Media.Imaging.BitmapImage? PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged(nameof(PreviewImage));
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

        private bool _showSecurityWarning = false;
        public bool ShowSecurityWarning
        {
            get => _showSecurityWarning;
            set
            {
                _showSecurityWarning = value;
                OnPropertyChanged(nameof(ShowSecurityWarning));
            }
        }

        // الأوامر
        public ICommand SaveDocCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand PreviewCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomResetCommand { get; }
        public ICommand DismissWarningCommand { get; }

        public DocumentsViewModel()
        {
            _repo = new VaultRepository(); // المسار يحدد تلقائيًا إلى AppData\PasswordManager
            _vault = _repo.LoadVault();

            // تحميل الوثائق من القبو
            Reload();

            SaveDocCommand = new RelayCommand<DocumentEntry>(SaveNewDocument, CanSaveDocument);
            ReloadCommand = new RelayCommand(_ => Reload());
            PreviewCommand = new RelayCommand<DocumentEntry>(Preview, doc => doc != null);
            OpenFolderCommand = new RelayCommand<DocumentEntry>(OpenFolder, doc => doc != null);
            EditCommand = new RelayCommand<DocumentEntry>(Edit, doc => doc != null);
            DeleteCommand = new RelayCommand<DocumentEntry>(Delete, doc => doc != null);
            DownloadCommand = new RelayCommand<DocumentEntry>(Download, doc => doc != null && !string.IsNullOrEmpty(doc.ExternalImagePath));
            ZoomInCommand = new RelayCommand(_ => ZoomLevel += 0.1, _ => PreviewImage != null);
            ZoomOutCommand = new RelayCommand(_ => ZoomLevel -= 0.1, _ => PreviewImage != null);
            ZoomResetCommand = new RelayCommand(_ => ZoomLevel = 1.0, _ => PreviewImage != null);
            DismissWarningCommand = new RelayCommand(_ => ShowSecurityWarning = false);
        }

        public void SaveNewDocument(DocumentEntry? newDoc)
        {
            try
            {
                if (newDoc is null)
                {
                    MessageBox.Show("Error saving document: Document is null", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 🔹 حفظ الصورة المشفرة إذا موجودة
                if (!string.IsNullOrWhiteSpace(newDoc.ExternalImagePath) && File.Exists(newDoc.ExternalImagePath))
                {
                    // حفظ المسار الأصلي قبل التغيير
                    string originalImagePath = newDoc.ExternalImagePath;
                    
                    // حفظ الصورة المشفرة في مجلد DocumentsFiles
                    string vaultDirectory = _repo.GetVaultDirectory();
                    string encryptedImagePath = FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);
                    
                    // تحديث المسار ليشير إلى الملف المشفر المحفوظ
                    newDoc.ExternalImagePath = encryptedImagePath;
                    
                    // أيضاً نحفظ Base64 في FileContentBase64 للتوافق مع النسخ القديمة (من الصورة الأصلية)
                    newDoc.FileContentBase64 = ImageService.ToBase64(originalImagePath);
                }

                _vault.Entries.Add(newDoc);
                _repo.SaveVault(_vault);
                Documents.Add(newDoc);

                // Show security warning about deleting original file
                ShowSecurityWarning = true;

                // Select the newly added document
                SelectedDocument = newDoc;

                MessageBox.Show("Document saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving document: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Reload()
        {
            _vault = _repo.LoadVault();
            Documents.Clear();

            foreach (var doc in _vault.Entries.OfType<DocumentEntry>())
                Documents.Add(doc);

            OnPropertyChanged(nameof(Documents));
        }

        private void Preview(DocumentEntry? document)
        {
            // Use the passed document parameter, or fall back to SelectedDocument
            var doc = document ?? SelectedDocument;
            
            if (doc == null)
            {
                MessageBox.Show("Select a document first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(doc.ExternalImagePath) ||
                !File.Exists(doc.ExternalImagePath))
            {
                MessageBox.Show("This document has no attached image.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var previewWindow = new DocumentPreview(doc.ExternalImagePath)
            {
                Owner = Application.Current.MainWindow
            };
            previewWindow.ShowDialog();
        }

        private void LoadPreviewImage()
        {
            PreviewImage = null;
            
            if (SelectedDocument == null || 
                string.IsNullOrEmpty(SelectedDocument.ExternalImagePath) ||
                !File.Exists(SelectedDocument.ExternalImagePath))
            {
                return;
            }

            try
            {
                // Check if the file exists and is readable
                if (!File.Exists(SelectedDocument.ExternalImagePath))
                {
                    MessageBox.Show("The encrypted image file was not found. The document may have been moved or deleted.", 
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Try to extract and load the image
                string tempPath = FileHelper.ExtractDocumentImage(SelectedDocument.ExternalImagePath);
                if (File.Exists(tempPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(tempPath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                    ZoomLevel = 1.0; // Reset zoom when loading new image
                }
                else
                {
                    MessageBox.Show("Failed to extract the document image. The file may be corrupted.", 
                        "Extraction Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (FormatException ex) when (ex.Message.Contains("Base-64") || ex.Message.Contains("Base64"))
            {
                MessageBox.Show(
                    "The document image file appears to be corrupted or was not encrypted properly. " +
                    "This may happen if the file was created with an older version of the application. " +
                    "Please try re-adding this document.\n\n" +
                    $"Technical details: {ex.Message}", 
                    "Corrupted File", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preview: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(DocumentEntry? document)
        {
            // Use the passed document parameter, or fall back to SelectedDocument
            var doc = document ?? SelectedDocument;
            
            if (doc?.ExternalImagePath == null ||
                !File.Exists(doc.ExternalImagePath))
            {
                MessageBox.Show("Image file not found.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var psi = new System.Diagnostics.ProcessStartInfo("explorer.exe",
                $"/select,\"{doc.ExternalImagePath}\"");
            System.Diagnostics.Process.Start(psi);
        }

        private void Edit(DocumentEntry? document)
        {
            var doc = document ?? SelectedDocument;
            if (doc == null) return;

            var editWindow = new AddDocument
            {
                Owner = Application.Current.MainWindow,
                Title = "Edit Document"
            };

            // TODO: Load document data into edit window
            // For now, just show the window
            if (editWindow.ShowDialog() == true && editWindow.Document != null)
            {
                // Update document
                var index = Documents.IndexOf(doc);
                if (index >= 0)
                {
                    // Update properties
                    doc.Name = editWindow.Document.Name;
                    doc.Type = editWindow.Document.Type;
                    doc.Number = editWindow.Document.Number;
                    doc.Issuer = editWindow.Document.Issuer;
                    doc.Notes = editWindow.Document.Notes;
                    doc.ExpiryDate = editWindow.Document.ExpiryDate;

                    // If new image selected, save it
                    if (!string.IsNullOrWhiteSpace(editWindow.Document.ExternalImagePath) && 
                        File.Exists(editWindow.Document.ExternalImagePath) &&
                        editWindow.Document.ExternalImagePath != doc.ExternalImagePath)
                    {
                        string originalImagePath = editWindow.Document.ExternalImagePath;
                        string vaultDirectory = _repo.GetVaultDirectory();
                        string encryptedImagePath = FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);
                        doc.ExternalImagePath = encryptedImagePath;
                    }

                    _repo.SaveVault(_vault);
                    LoadPreviewImage();
                    OnPropertyChanged(nameof(Documents));
                }
            }
        }

        private void Delete(DocumentEntry? document)
        {
            var doc = document ?? SelectedDocument;
            if (doc == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{doc.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete encrypted image file if exists
                    if (!string.IsNullOrEmpty(doc.ExternalImagePath) && File.Exists(doc.ExternalImagePath))
                    {
                        try
                        {
                            File.Delete(doc.ExternalImagePath);
                        }
                        catch { /* Ignore if file deletion fails */ }
                    }

                    _vault.Entries.Remove(doc);
                    Documents.Remove(doc);
                    _repo.SaveVault(_vault);

                    if (SelectedDocument == doc)
                    {
                        SelectedDocument = null;
                        PreviewImage = null;
                    }

                    MessageBox.Show("Document deleted successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting document: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Download(DocumentEntry? document)
        {
            var doc = document ?? SelectedDocument;
            if (doc == null || string.IsNullOrEmpty(doc.ExternalImagePath)) return;

            try
            {
                string tempPath = FileHelper.ExtractDocumentImage(doc.ExternalImagePath);
                if (!File.Exists(tempPath))
                {
                    MessageBox.Show("Failed to extract document image.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    FileName = $"{doc.Name ?? "Document"}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(tempPath, saveDialog.FileName, overwrite: true);
                    MessageBox.Show("Document downloaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading document: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveDocument(DocumentEntry? entry)
        {
            return entry is not null &&
                   !string.IsNullOrWhiteSpace(entry.Type) &&
                   !string.IsNullOrWhiteSpace(entry.Number);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
