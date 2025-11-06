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

        public ObservableCollection<DocumentEntry> Documents { get; }

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
        public ICommand AddCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand PreviewCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public DocumentsViewModel()
        {
            _repo = new VaultRepository(); // المسار يحدد تلقائيًا إلى AppData\PasswordManager
            _vault = _repo.LoadVault();

            var docs = _vault.Entries.OfType<DocumentEntry>();
            Documents = new ObservableCollection<DocumentEntry>(docs);

            AddCommand = new RelayCommand(_ => AddDocument());
            ReloadCommand = new RelayCommand(_ => Reload());
            PreviewCommand = new RelayCommand(_ => Preview(), _ => SelectedDocument != null);
            OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => SelectedDocument != null);
        }

        private void AddDocument()
        {
            // فتح نافذة إضافة مستند جديد
            var addWindow = new AddDocument
            {
                Owner = Application.Current.MainWindow
            };
            addWindow.ShowDialog();

            // إعادة تحميل البيانات بعد الإضافة
            Reload();
        }

        public void Reload()
        {
            _vault = _repo.LoadVault();
            Documents.Clear();
            foreach (var doc in _vault.Entries.OfType<DocumentEntry>())
                Documents.Add(doc);
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
