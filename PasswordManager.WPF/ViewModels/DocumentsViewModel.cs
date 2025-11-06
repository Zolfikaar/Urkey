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
            }
        }

        // الأوامر
        public ICommand SaveDocCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand PreviewCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public DocumentsViewModel()
        {
            _repo = new VaultRepository(); // المسار يحدد تلقائيًا إلى AppData\PasswordManager
            _vault = _repo.LoadVault();

            // تحميل الوثائق من القبو
            Reload();

            SaveDocCommand = new RelayCommand<DocumentEntry>(SaveNewDocument, CanSaveDocument);
            ReloadCommand = new RelayCommand(_ => Reload());
            PreviewCommand = new RelayCommand(_ => Preview(), _ => SelectedDocument != null);
            OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => SelectedDocument != null);
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

                // 🔹 تحويل الصورة إلى Base64 إذا موجودة
                if (!string.IsNullOrWhiteSpace(newDoc.ExternalImagePath) && File.Exists(newDoc.ExternalImagePath))
                {
                    newDoc.FileContentBase64 = ImageService.ToBase64(newDoc.ExternalImagePath);
                }

                _vault.Entries.Add(newDoc);
                _repo.SaveVault(_vault);
                Documents.Add(newDoc);

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

        private void Preview()
        {
            if (SelectedDocument == null)
            {
                MessageBox.Show("Select a document first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(SelectedDocument.ExternalImagePath) ||
                !File.Exists(SelectedDocument.ExternalImagePath))
            {
                MessageBox.Show("This document has no attached image.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var previewWindow = new DocumentPreview(SelectedDocument.ExternalImagePath)
            {
                Owner = Application.Current.MainWindow
            };
            previewWindow.ShowDialog();
        }

        private void OpenFolder()
        {
            if (SelectedDocument?.ExternalImagePath == null ||
                !File.Exists(SelectedDocument.ExternalImagePath))
            {
                MessageBox.Show("Image file not found.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var psi = new System.Diagnostics.ProcessStartInfo("explorer.exe",
                $"/select,\"{SelectedDocument.ExternalImagePath}\"");
            System.Diagnostics.Process.Start(psi);
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
