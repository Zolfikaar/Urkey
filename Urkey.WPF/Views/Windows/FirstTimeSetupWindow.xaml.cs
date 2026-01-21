using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Urkey.Core.Models;
using Urkey.Core.Repository;
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
        private string _password;
        private string _confPassword;

        private readonly VaultService _vaultService;
        private readonly UserService _userService;
        public Vault vault;

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

            // Position settings button based on language
            // In RTL, button should be on left; in LTR, on right
            SettingsButton.HorizontalAlignment = langCode == "ar"
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

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

            // 4) تحديث اتجاه النص (يمين ← يسار للغة العربية)
            FlowDirection = newLang == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            // 5) تبديل مكان زر الإعدادات حسب اللغة
            SettingsButton.HorizontalAlignment = newLang == "ar"
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            // 6) تحديث الخط بناءً على اللغة
            var fontFamily = newLang == "ar"
                ? new FontFamily("Cairo")
                : new FontFamily("LeagueSpartan");

            ApplyFontToWindow(this, fontFamily);

            // 7) إعادة تحميل النصوص مباشرة (DynamicResource يدعم التحديث الفوري)
            // لكن لو تحب تحديث كامل الواجهة:
            // this.InvalidateVisual();

            // 8) إغلاق القائمة
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

            if (_password != null && _confPassword != null )
            {
                if(_password == _confPassword)
                  return true;
                else
                {
                    MessageBox.Show("Master password is not matching confirm password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;

                }

            } else
            {
                MessageBox.Show("Master password and confirm password is required","Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

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
            // This generates salt and hash internally
            var result = _userService.FirstSetup(null, null, masterPassword);
            if (!result.Success)
            {
                MessageBox.Show($"Setup failed: {result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save user data (salt and hash) to user.json
            _userService.Save();

            // Initialize EncryptionService with password and salt
            var userSalt = _userService.GetSalt();
            EncryptionService.Initialize(userSalt, masterPassword);

            // Load or create the vault
            _vaultService.Load();

            // Save the vault to create vault.json file
            _vaultService.Save();

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
