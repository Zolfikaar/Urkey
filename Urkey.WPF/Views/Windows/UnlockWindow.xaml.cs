using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Windows
{
    public partial class UnlockWindow
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

        private bool _isPasswordVisible;
        private string _enteredMasterPassword = string.Empty;
        private readonly VaultService _vaultService;
        private readonly UserViewModel _userVM;

        private int _failedAttempts;
        private DateTime? _lockoutUntil;
        private DispatcherTimer? _lockoutTimer;

        public UnlockWindow(VaultService vaultService)
        {
            InitializeComponent();
            Loaded += UnlockWindow_Loaded;
            _vaultService = vaultService;

            MasterPasswordBox.PasswordChanged += (_, _) =>
            {
                if (!_isPasswordVisible)
                {
                    MasterPasswordTextBox.Text = MasterPasswordBox.Password;
                    _enteredMasterPassword = MasterPasswordBox.Password;
                }
            };

            MasterPasswordTextBox.TextChanged += (_, _) =>
            {
                if (_isPasswordVisible)
                {
                    MasterPasswordBox.Password = MasterPasswordTextBox.Text;
                    _enteredMasterPassword = MasterPasswordTextBox.Text;
                }
            };

            _userVM = new UserViewModel();
        }

        private void UnlockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var langCode = App.Settings.Language;
            LanguageManager.ApplyLanguage(langCode);

            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            SettingsButton.HorizontalAlignment = langCode == "ar"
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            var fontFamily = langCode == "ar" ? new FontFamily("Cairo") : new FontFamily("LeagueSpartan");
            ApplyFontToWindow(this, fontFamily);

            MasterPasswordBox.Focus();
        }

        private void ApplyFontToWindow(DependencyObject parent, FontFamily fontFamily)
        {
            if (parent is TextBlock textBlock)
                textBlock.FontFamily = fontFamily;
            else if (parent is TextBox textBox)
                textBox.FontFamily = fontFamily;
            else if (parent is PasswordBox passwordBox)
                passwordBox.FontFamily = fontFamily;
            else if (parent is Button button)
                button.FontFamily = fontFamily;
            else if (parent is Label label)
                label.FontFamily = fontFamily;
            else if (parent is Control control)
                control.FontFamily = fontFamily;
            else if (parent is Window window)
                window.FontFamily = fontFamily;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ApplyFontToWindow(child, fontFamily);
            }
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                MasterPasswordTextBox.Text = MasterPasswordBox.Password;
                _enteredMasterPassword = MasterPasswordBox.Password;
                MasterPasswordTextBox.Visibility = Visibility.Visible;
                MasterPasswordBox.Visibility = Visibility.Collapsed;
                MasterPasswordTextBox.Focus();
            }
            else
            {
                MasterPasswordBox.Password = MasterPasswordTextBox.Text;
                _enteredMasterPassword = MasterPasswordTextBox.Text;
                MasterPasswordBox.Visibility = Visibility.Visible;
                MasterPasswordTextBox.Visibility = Visibility.Collapsed;
                MasterPasswordBox.Focus();
            }
        }

        private void TogglePasswordArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TogglePasswordButton_Click(TogglePasswordButton, new RoutedEventArgs());
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                UnlockButton_Click(sender, e);
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsLockedOut(out var remaining))
            {
                ShowLocalizedMessage(
                    "UnlockWindow_LockoutMessage",
                    "UnlockWindow_ErrorTitle",
                    MessageBoxImage.Warning,
                    (int)Math.Ceiling(remaining.TotalSeconds));
                return;
            }

            var password = _enteredMasterPassword;
            UnlockButton.IsEnabled = false;
            UserService.UnlockResult result;
            try
            {
                result = _userVM.Unlock(password);
            }
            catch (Exception)
            {
                UnlockButton.IsEnabled = true;
                ShowLocalizedMessage("UnlockWindow_UnlockFailed", "UnlockWindow_ErrorTitle", MessageBoxImage.Error);
                return;
            }
            finally
            {
                ClearPasswordFields();
            }

            if (!result.Success)
            {
                UnlockButton.IsEnabled = true;
                HandleFailedUnlock(result.Status);
                return;
            }

            _failedAttempts = 0;

            try
            {
                _vaultService.Load();
            }
            catch (Exception)
            {
                UnlockButton.IsEnabled = true;
                App.VaultService.ClearSession();
                ShowLocalizedMessage("UnlockWindow_VaultLoadFailed", "UnlockWindow_ErrorTitle", MessageBoxImage.Error);
                return;
            }

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }

        private void HandleFailedUnlock(UserService.UnlockStatus status)
        {
            if (status == UserService.UnlockStatus.MigrationFailed)
            {
                ShowLocalizedMessage("UnlockWindow_MigrationFailed", "UnlockWindow_ErrorTitle", MessageBoxImage.Error);
                return;
            }

            _failedAttempts++;
            if (_failedAttempts >= MaxFailedAttempts)
            {
                _lockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                StartLockoutTimer();
                ShowLocalizedMessage(
                    "UnlockWindow_LockoutMessage",
                    "UnlockWindow_ErrorTitle",
                    MessageBoxImage.Warning,
                    (int)LockoutDuration.TotalSeconds);
                return;
            }

            ShowLocalizedMessage("UnlockWindow_IncorrectPassword", "UnlockWindow_ErrorTitle", MessageBoxImage.Error);
        }

        private bool IsLockedOut(out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (_lockoutUntil == null)
                return false;

            remaining = _lockoutUntil.Value - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _lockoutUntil = null;
                _failedAttempts = 0;
                return false;
            }

            return true;
        }

        private void StartLockoutTimer()
        {
            _lockoutTimer?.Stop();
            _lockoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _lockoutTimer.Tick += (_, _) =>
            {
                if (!IsLockedOut(out _))
                    _lockoutTimer?.Stop();
            };
            _lockoutTimer.Start();
        }

        private void ClearPasswordFields()
        {
            _enteredMasterPassword = string.Empty;
            MasterPasswordBox.Password = string.Empty;
            MasterPasswordTextBox.Text = string.Empty;
        }

        private void ForgotPasswordLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Placeholder only — recovery is a future phase.
            ShowLocalizedMessage("UnlockWindow_ForgotPasswordPlaceholder", "UnlockWindow_InfoTitle", MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        private static void ShowLocalizedMessage(
            string messageKey,
            string titleKey,
            MessageBoxImage icon,
            params object[] formatArgs)
        {
            string message = ResolveString(messageKey);
            if (formatArgs.Length > 0)
            {
                try { message = string.Format(message, formatArgs); }
                catch { /* keep unformatted */ }
            }

            switch (icon)
            {
                case MessageBoxImage.Error:
                    ToastService.Error(message);
                    break;
                case MessageBoxImage.Warning:
                    ToastService.Warning(message);
                    break;
                case MessageBoxImage.Information:
                    ToastService.Info(message);
                    break;
                default:
                    ToastService.Show(message);
                    break;
            }
        }

        private static string ResolveString(string key)
        {
            return Application.Current.TryFindResource(key) as string
                   ?? key;
        }
    }
}
