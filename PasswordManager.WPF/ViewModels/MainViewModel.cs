using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PasswordManager.Core.Models;
using PasswordManager.WPF.Views;
using System.ComponentModel;
using System.IO;

namespace PasswordManager.WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _initialLaunch = true;
        private bool _isAuthenticated = false;
        private bool _isVaultLocked = true;

        private string _masterPassword = string.Empty;
        private string _password = string.Empty;
        private string _email = string.Empty;
        private string _emailVerified = string.Empty;
        private string _phoneNumber = string.Empty;


        private int _loginAttempts = 0;
        private const int MaxLoginAttempts = 3;
        private const int MaxPasswordAttempts = 3;

        private const int LockoutDurationMinutes = 5;
        private const int ClearClipboardDuration = 1;


        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 40;

        private Dictionary<string, string> _data = new Dictionary<string, string>();



        //MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public MainViewModel()
        {
            // Check if this is the first launch by looking for a settings file or registry entry
            // For now, we'll use a simple approach with a file
            CheckInitialLaunchStatus();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public bool InitialLaunch()
        { return _initialLaunch; }

        public void ActivateApp()
        {
            _initialLaunch = false;
            SaveInitialLaunchStatus();
        }

        public void LoadMainwindow()
        {
            // here show initialize the data 
            //object value = System.Diagnostics.Debug(MessageBox);
            MessageBox.Show("Load main Window");
        }

        private void CheckInitialLaunchStatus()
        {
            // Check if the app has been launched before
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "settings.txt");

            if (File.Exists(settingsPath))
            {
                _initialLaunch = false;
            }
        }

        private void SaveInitialLaunchStatus()
        {
            // Save that the app has been launched
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager");
            Directory.CreateDirectory(appDataPath);

            string settingsPath = Path.Combine(appDataPath, "settings.txt");
            File.WriteAllText(settingsPath, "AppLaunched=true");
        }

    }

}
