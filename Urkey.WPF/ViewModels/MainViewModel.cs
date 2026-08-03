using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;

namespace Urkey.WPF.ViewModels
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

        private FlowDirection _wordDirection = FlowDirection.LeftToRight;
        public FlowDirection WordDirection
        {
            get => _wordDirection;
            set
            {
                _wordDirection = value;
                OnPropertyChanged("WordDirection");
            }
        }

        public enum NavigationTarget
        {
            Home,
            Settings
           
        }

        private NavigationTarget _currentPage;
        public NavigationTarget CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage == value)
                    return;

                _currentPage = value;
                OnPropertyChanged();
            }
        }

        //MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public MainViewModel()
        {
            // Check if this is the first launch by looking for a settings file or registry entry
            // For now, we'll use a simple approach with a file
            CheckInitialLaunchStatus();

        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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
            // Initialize session UI state after unlock.
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
