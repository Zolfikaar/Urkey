using System;
using System.ComponentModel;
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
    private bool _isPasswordVisible = false;
    private bool _isConfirmPasswordVisible = false;

    public AddNewEntryViewModel()
    {
      // Get the vault file path from the application directory
      var appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      var vaultPath = System.IO.Path.Combine(appDirectory, "vault.json");
      _repository = new VaultRepository(vaultPath, "default_master_password"); // TODO: Get actual master password
      SaveCommand = new RelayCommand<object>(SaveCredential, CanSave);
      CancelCommand = new RelayCommand<object>(Cancel);
      GeneratePasswordCommand = new RelayCommand<object>(GeneratePassword);
      TogglePasswordVisibilityCommand = new RelayCommand<object>(TogglePasswordVisibility);
      ToggleConfirmPasswordVisibilityCommand = new RelayCommand<object>(ToggleConfirmPasswordVisibility);
    }

    public string ServiceName
    {
      get => _serviceName;
      set
      {
        _serviceName = value;
        OnPropertyChanged(nameof(ServiceName));
        ((RelayCommand<object>)SaveCommand).RaiseCanExecuteChanged();
      }
    }

    public string Username
    {
      get => _username;
      set
      {
        _username = value;
        OnPropertyChanged(nameof(Username));
        ((RelayCommand<object>)SaveCommand).RaiseCanExecuteChanged();
      }
    }

    public string Password
    {
      get => _password;
      set
      {
        _password = value;
        OnPropertyChanged(nameof(Password));
        ((RelayCommand<object>)SaveCommand).RaiseCanExecuteChanged();
      }
    }

    public string ConfirmPassword
    {
      get => _confirmPassword;
      set
      {
        _confirmPassword = value;
        OnPropertyChanged(nameof(ConfirmPassword));
        ((RelayCommand<object>)SaveCommand).RaiseCanExecuteChanged();
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

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand GeneratePasswordCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

    private bool CanSave(object parameter)
    {
      return !string.IsNullOrWhiteSpace(ServiceName) &&
             !string.IsNullOrWhiteSpace(Username) &&
             !string.IsNullOrWhiteSpace(Password) &&
             Password == ConfirmPassword;
    }

    private void SaveCredential(object parameter)
    {
      try
      {
        var credential = new Credential
        {
          ServiceName = ServiceName.Trim(),
          Username = Username.Trim(),
          Password = Password // TODO: Encrypt password before saving
        };

        // Save to vault
        _repository.AddCredential(credential);

        MessageBox.Show("Credential saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

        // Close the window
        if (parameter is Window window)
        {
          window.DialogResult = true;
          window.Close();
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error saving credential: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void Cancel(object parameter)
    {
      if (parameter is Window window)
      {
        window.DialogResult = false;
        window.Close();
      }
    }

    private void GeneratePassword(object parameter)
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
      var random = new Random();
      var password = new StringBuilder();

      for (int i = 0; i < 16; i++)
      {
        password.Append(chars[random.Next(chars.Length)]);
      }

      Password = password.ToString();
      ConfirmPassword = Password;
    }

    private void TogglePasswordVisibility(object parameter)
    {
      IsPasswordVisible = !IsPasswordVisible;
    }

    private void ToggleConfirmPasswordVisibility(object parameter)
    {
      IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
