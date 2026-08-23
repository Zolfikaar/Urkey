using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.ViewModels
{
    public class DocumentsViewModel : INotifyPropertyChanged, ISupportsViewMode
    {
        private readonly VaultService _vaultService;
        private Vault _vault;

        private ObservableCollection<DocumentEntry> _documents = new();
        public ObservableCollection<DocumentEntry> Documents
        {
            get => _documents;
            set
            {
                _documents = value;
                OnPropertyChanged(nameof(Documents));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsEmpty => Documents.Count == 0;

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

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value ?? string.Empty;
                OnPropertyChanged(nameof(SearchText));
                Reload();
            }
        }

        private bool _isListView = true;
        public bool IsListView
        {
            get => _isListView;
            set
            {
                if (_isListView == value) return;
                _isListView = value;
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(IsGridView));
            }
        }

        public bool IsGridView => !_isListView;

        // الأوامر
        public ICommand SaveDocCommand { get; }
        public ICommand AddCommand { get; }
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

        public DocumentsViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            _vault = _vaultService.EnsureLoaded();

            SaveDocCommand = new RelayCommand<DocumentEntry>(SaveNewDocument, CanSaveDocument);
            AddCommand = new RelayCommand<object>(_ => AddDocument());
            ReloadCommand = new RelayCommand<DocumentEntry>(_ => Reload());
            PreviewCommand = new RelayCommand<DocumentEntry>(Preview, doc => doc != null);
            OpenFolderCommand = new RelayCommand<DocumentEntry>(OpenFolder, doc => doc != null);
            EditCommand = new RelayCommand<DocumentEntry>(Edit, doc => doc != null);
            DeleteCommand = new RelayCommand<DocumentEntry>(Delete, doc => doc != null);
            DownloadCommand = new RelayCommand<DocumentEntry>(Download, doc => doc != null && !string.IsNullOrEmpty(doc.ExternalImagePath));
            ZoomInCommand = new RelayCommand<object?>(_ => ZoomLevel += 0.1, _ => PreviewImage != null);
            ZoomOutCommand = new RelayCommand<object?>(_ => ZoomLevel -= 0.1, _ => PreviewImage != null);
            ZoomResetCommand = new RelayCommand<object?>(_ => ZoomLevel = 1.0, _ => PreviewImage != null);
            DismissWarningCommand = new RelayCommand<DocumentEntry>(_ => ShowSecurityWarning = false);

            Reload();
        }

        private void AddDocument()
        {
            var win = new AddDocument
            {
                Owner = Application.Current.MainWindow
            };

            if (win.ShowDialog() == true && win.Document is not null)
            {
                SaveNewDocument(win.Document);
                Reload();
            }
        }

        public void SaveNewDocument(DocumentEntry? newDoc)
        {
            try
            {
                if (newDoc is null)
                {
                    ToastService.Error(Loc.Get("Documents_Error_Null"));
                    return;
                }

                var validation = EntryValidator.ValidateDocument(newDoc);
                if (!validation.IsValid)
                {
                    ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                    return;
                }

                _vault = _vaultService.EnsureLoaded();

                if (!string.IsNullOrWhiteSpace(newDoc.ExternalImagePath) && File.Exists(newDoc.ExternalImagePath))
                {
                    string originalImagePath = newDoc.ExternalImagePath;
                    if (!IsAllowedImage(originalImagePath))
                    {
                        ToastService.Warning(Loc.Get("Documents_Error_InvalidType"));
                        return;
                    }

                    var info = new FileInfo(originalImagePath);
                    if (info.Length > 500 * 1024)
                    {
                        ToastService.Warning(Loc.Get("Documents_Error_TooLarge"));
                        return;
                    }

                    // Encrypt image file at rest; do not also embed raw Base64 in vault JSON.
                    newDoc.FileContentBase64 = null;
                    newDoc.FileName = info.Name;
                    string vaultDirectory = _vaultService.GetVaultDirectory();
                    newDoc.ExternalImagePath = FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);
                }

                _vaultService.AddEntry(newDoc, logActivity: false);
                _vaultService.LogActivity(
                    EntryMetadata.ActionUploaded,
                    EntryMetadata.TypeDocument,
                    EntryMetadata.GetDisplayName(newDoc),
                    newDoc.Id);

                Documents.Add(newDoc);
                ShowSecurityWarning = true;
                SelectedDocument = newDoc;

                ToastService.Success(Loc.Get("Documents_Saved"));
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("Documents_Error_Save"));
            }
        }

        public void Reload()
        {
            var previousId = SelectedDocument?.Id;
            _vault = _vaultService.LoadVault();
            Documents.Clear();

            var query = _searchText.Trim();
            foreach (var doc in EntryListSort.Apply(_vault.Entries.OfType<DocumentEntry>()))
            {
                if (!string.IsNullOrEmpty(query) &&
                    !(doc.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) &&
                    !(doc.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) &&
                    !(doc.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                    continue;

                Documents.Add(doc);
            }

            SelectedDocument = Documents.FirstOrDefault(d => d.Id == previousId) ?? Documents.FirstOrDefault();
            OnPropertyChanged(nameof(Documents));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private static bool IsAllowedImage(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg";
        }

        private void Preview(DocumentEntry? document)
        {
            // Use the passed document parameter, or fall back to SelectedDocument
            var doc = document ?? SelectedDocument;
            
            if (doc == null)
            {
                ToastService.Success(Loc.Get("Documents_SelectFirst"));
                return;
            }

            if (string.IsNullOrEmpty(doc.ExternalImagePath) ||
                !File.Exists(doc.ExternalImagePath))
            {
                ToastService.Success(Loc.Get("Documents_NoImage"));
                return;
            }

            SelectedDocument = doc;

            var previewWindow = new DocumentPreview(doc.ExternalImagePath, doc)
            {
                Owner = Application.Current.MainWindow
            };
            previewWindow.Show();
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
                    ToastService.Warning(Loc.Get("Documents_FileMissing"));
                    return;
                }

                string tempPath = FileHelper.ExtractDocumentImage(SelectedDocument.ExternalImagePath);
                try
                {
                    if (File.Exists(tempPath))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(tempPath);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        PreviewImage = bitmap;
                        ZoomLevel = 0.5;
                    }
                    else
                    {
                        ToastService.Error(Loc.Get("Documents_ExtractFailed"));
                    }
                }
                finally
                {
                    FileHelper.TryDeleteTempFile(tempPath);
                }
            }
            catch (FormatException)
            {
                ToastService.Error(Loc.Get("Documents_Corrupted"));
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("Documents_PreviewFailed"));
            }
        }

        private void OpenFolder(DocumentEntry? document)
        {
            // Use the passed document parameter, or fall back to SelectedDocument
            var doc = document ?? SelectedDocument;
            
            if (doc?.ExternalImagePath == null ||
                !File.Exists(doc.ExternalImagePath))
            {
                ToastService.Warning(Loc.Get("Documents_FileMissing"));
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

            var editWindow = new AddDocument(doc)
            {
                Owner = Application.Current.MainWindow
            };

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
                        string vaultDirectory = _vaultService.GetVaultDirectory();
                        string encryptedImagePath = FileHelper.SaveDocumentImage(originalImagePath, vaultDirectory);
                        doc.ExternalImagePath = encryptedImagePath;
                    }

                    _vaultService.UpdateEntry(doc);
                    LoadPreviewImage();
                    OnPropertyChanged(nameof(Documents));
                }
            }
        }

        private void Delete(DocumentEntry? document)
        {
            var doc = document ?? SelectedDocument;
            if (doc == null) return;

            if (!EntryDialogHelper.ConfirmDelete(doc.Name))
                return;

            try
            {
                if (!string.IsNullOrEmpty(doc.ExternalImagePath) && File.Exists(doc.ExternalImagePath))
                {
                    try { File.Delete(doc.ExternalImagePath); }
                    catch { /* Ignore if file deletion fails */ }
                }

                _vaultService.RemoveEntry(doc.Id);
                Documents.Remove(doc);

                if (SelectedDocument == doc)
                {
                    SelectedDocument = null;
                    PreviewImage = null;
                }

                ToastService.Success(Loc.Get("Documents_Deleted"));
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("Documents_Error_Delete"));
            }
        }

        private void Download(DocumentEntry? document)
        {
            var doc = document ?? SelectedDocument;
            if (doc == null || string.IsNullOrEmpty(doc.ExternalImagePath)) return;

            try
            {
                string tempPath = FileHelper.ExtractDocumentImage(doc.ExternalImagePath);
                try
                {
                    if (!File.Exists(tempPath))
                    {
                        ToastService.Error(Loc.Get("Documents_ExtractFailed"));
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
                        ToastService.Success(Loc.Get("Documents_Downloaded"));
                    }
                }
                finally
                {
                    FileHelper.TryDeleteTempFile(tempPath);
                }
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("Documents_Error_Download"));
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
