using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Urkey.WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _initialLaunch = true;

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
