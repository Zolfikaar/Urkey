using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using PasswordManager.Core.Models;
using PasswordManager.Core.Repository;
using PasswordManager.WPF.Commands;

namespace PasswordManager.WPF.ViewModels
{
    public class AddNewEntryViewModel : INotifyPropertyChanged
    {
        private readonly VaultRepository _repository;

        private string _serviceName = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isPasswordVisible;
        private bool _isConfirmPasswordVisible;

        public AddNewEntryViewModel()
        {
            // حدد مسار vault داخل AppData
            var userVaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordManager"
            );
            Directory.CreateDirectory(userVaultDir);

            var vaultPath = Path.Combine(userVaultDir, "vault.json");
            _repository = new VaultRepository(vaultPath);

            // أوامر
            SaveCommand = new RelayCommand(SaveCredential, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            GeneratePasswordCommand = new RelayCommand(GeneratePassword);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ToggleConfirmPasswordVisibilityCommand = new RelayCommand(ToggleConfirmPasswordVisibility);
        }

        // 🟦 الخصائص
        public string ServiceName
        {
            get => _serviceName;
            set
            {
                _serviceName = value;
                OnPropertyChanged(nameof(ServiceName));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged(nameof(ConfirmPassword));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged(nameof(IsPasswordVisible));
            }
        }

        public bool IsConfirmPasswordVisible
        {
            get => _isConfirmPasswordVisible;
            set
            {
                _isConfirmPasswordVisible = value;
                OnPropertyChanged(nameof(IsConfirmPasswordVisible));
            }
        }

        // 🟩 الأوامر
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand GeneratePasswordCommand { get; }
        public RelayCommand TogglePasswordVisibilityCommand { get; }
        public RelayCommand ToggleConfirmPasswordVisibilityCommand { get; }

        // 🟦 منطق الأوامر
        private bool CanSave(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(ServiceName) &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password == ConfirmPassword;
        }

        private void SaveCredential(object? parameter)
        {
            try
            {
                var newAccount = new AccountEntry
                {
                    ServiceName = ServiceName,
                    Username = Username,
                    Password = Password
                };

                _repository.AddEntry(newAccount);

                MessageBox.Show("Credential saved successfully!",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving credential: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object? parameter)
        {
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private void GeneratePassword(object? parameter)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            var random = new Random();
            var password = new StringBuilder();

            for (int i = 0; i < 16; i++)
                password.Append(chars[random.Next(chars.Length)]);

            Password = password.ToString();
            ConfirmPassword = Password;
        }

        private void TogglePasswordVisibility(object? parameter)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ToggleConfirmPasswordVisibility(object? parameter)
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        // 🟪 PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
