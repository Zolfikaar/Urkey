using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for FirstTimeSetupWindow.xaml
    /// </summary>
    public partial class FirstTimeSetupWindow : Window
    {
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;
        private string _password = string.Empty;
        private string _confPassword = string.Empty;

        private readonly VaultService _vaultService;
        private readonly UserService _userService;

        public FirstTimeSetupWindow(VaultService vaultService)
        {
            InitializeComponent();

            Loaded += FirstTimeSetupWindow_Loaded;

            
            _vaultService = vaultService;
            _userService = new UserService();

            // Bind password visibility
            MasterPasswordBox.PasswordChanged += (s, e) =>
            {
                if (!_isPasswordVisible)
                    MasterPasswordTextBox.Text = MasterPasswordBox.Password;
            };

            MasterPasswordTextBox.TextChanged += (s, e) =>
            {
                if (_isPasswordVisible)
                    MasterPasswordBox.Password = MasterPasswordTextBox.Text;
            };

            // Bind confirm password visibility
            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                if (!_isConfirmPasswordVisible)
                    ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
            };

            ConfirmPasswordTextBox.TextChanged += (s, e) =>
            {
                if (_isConfirmPasswordVisible)
                    ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
            };


        }

        private void FirstTimeSetupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply language and set FlowDirection
            var langCode = App.Settings?.Language ?? "en";
            LanguageManager.ApplyLanguage(langCode);

            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            // Settings gear: AuthSettingsBtnStyle uses HorizontalAlignment=Right (trailing),
            // mirrored by FlowDirection (LTR → bottom-right, RTL → bottom-left).

            // Apply font based on language
            var fontFamily = langCode == "ar" ? new FontFamily("Cairo") : new FontFamily("LeagueSpartan");
            ApplyFontToWindow(this, fontFamily);

            // Set focus on password field
            MasterPasswordBox.Focus();
        }
        public void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            // 1) تحديد اللغة الجديدة
            string newLang = App.Settings.Language == "en" ? "ar" : "en";

            // 2) تحديث الإعدادات
            App.Settings.Language = newLang;
            SettingsHelper.SaveSettings(App.Settings);

            // 3) تطبيق اللغة الجديدة على الواجهة
            LanguageManager.ApplyLanguage(newLang);

            // 4) تحديث اتجاه النص — trailing alignment on SettingsButton mirrors automatically
            FlowDirection = newLang == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            // 5) تحديث الخط بناءً على اللغة
            var fontFamily = newLang == "ar"
                ? new FontFamily("Cairo")
                : new FontFamily("LeagueSpartan");

            ApplyFontToWindow(this, fontFamily);

            // 6) إغلاق القائمة
            SettingsPopup.IsOpen = false;
        }

        private void ApplyFontToWindow(DependencyObject parent, FontFamily fontFamily)
        {
            if (parent == null) return;

            // Apply font to current element based on type
            if (parent is TextBlock textBlock)
            {
                textBlock.FontFamily = fontFamily;
            }
            else if (parent is TextBox textBox)
            {
                textBox.FontFamily = fontFamily;
            }
            else if (parent is PasswordBox passwordBox)
            {
                passwordBox.FontFamily = fontFamily;
            }
            else if (parent is Button button)
            {
                button.FontFamily = fontFamily;
            }
            else if (parent is Label label)
            {
                label.FontFamily = fontFamily;
            }
            else if (parent is Control control)
            {
                control.FontFamily = fontFamily;
            }
            else if (parent is Window window)
            {
                window.FontFamily = fontFamily;
            }

            // Recursively apply to children
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ApplyFontToWindow(child, fontFamily);
            }
        }

        public void ToggleMasterPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                MasterPasswordTextBox.Text = MasterPasswordBox.Password;
                MasterPasswordTextBox.Visibility = Visibility.Visible;
                MasterPasswordBox.Visibility = Visibility.Collapsed;
                MasterPasswordTextBox.Focus();
            }
            else
            {
                MasterPasswordBox.Password = MasterPasswordTextBox.Text;
                MasterPasswordBox.Visibility = Visibility.Visible;
                MasterPasswordTextBox.Visibility = Visibility.Collapsed;
                MasterPasswordBox.Focus();
            }
        }
        public void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

            if (_isConfirmPasswordVisible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Focus();
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                if (IsPasswordsMatch())
                    ContinueBtn_Click(sender, e);
                else
                    return;
            }
        }

        private bool IsPasswordsMatch()
        {
            _password = _isPasswordVisible ? MasterPasswordTextBox.Text.Trim() : MasterPasswordBox.Password.Trim();
            _confPassword = _isConfirmPasswordVisible ? ConfirmPasswordTextBox.Text.Trim() : ConfirmPasswordBox.Password.Trim();

            if (_password != null && _confPassword != null)
            {
                if (_password == _confPassword)
                    return true;

                ToastService.Error(
                    Application.Current.TryFindResource("Setup_Error_PasswordMismatch") as string
                        ?? "Passwords do not match.");
                return false;
            }

            ToastService.Warning(
                Application.Current.TryFindResource("Setup_Error_EmptyPassword") as string
                    ?? "Password is required.");
            return false;

        }

        public void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            // Validate passwords match before proceeding
            if (!IsPasswordsMatch())
            {
                return; // IsPasswordsMatch already shows error message
            }

            // Get the password value
            string masterPassword;
            masterPassword = _isPasswordVisible
                ? MasterPasswordTextBox.Text.Trim()
                : MasterPasswordBox.Password.Trim();

            // Setup user with email, phone, and master password
            // This generates salt and verifier hash internally (never stores the AES key).
            var result = _userService.FirstSetup(null, null, masterPassword);
            if (!result.Success)
            {
                var errorKey = result.Error switch
                {
                    UserService.FirstSetupError.EmptyMasterPassword => "Setup_Error_EmptyPassword",
                    UserService.FirstSetupError.InvalidMasterPassword => "Setup_Error_InvalidPassword",
                    UserService.FirstSetupError.InvalidEmail => "Setup_Error_InvalidEmail",
                    UserService.FirstSetupError.InvalidPhone => "Setup_Error_InvalidPhone",
                    _ => "Setup_Error_Generic"
                };
                ToastService.Error(
                    Application.Current.TryFindResource(errorKey) as string ?? errorKey);
                return;
            }

            _userService.Save();

            var userSalt = _userService.GetSalt();
            EncryptionService.InitializeFromPassword(
                masterPassword,
                userSalt,
                EncryptionService.CurrentKdfIterations);

            Urkey.Core.Managers.VaultManager.Unlock();

            // Create empty encrypted vault (AES-GCM)
            _vaultService.Load();
            _vaultService.Save();

            // Drop plaintext password copies from the setup UI
            MasterPasswordBox.Password = string.Empty;
            MasterPasswordTextBox.Text = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
            ConfirmPasswordTextBox.Text = string.Empty;
            _password = string.Empty;
            _confPassword = string.Empty;

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            Close();
        }
        
        public void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }
    }
}
