using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Urkey.Core.Models;
using Urkey.Core.Repository;
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
        public Vault vault;

        public FirstTimeSetupWindow()
        {
            InitializeComponent();

            Loaded += FirstTimeSetupWindow_Loaded;

            var repo = new VaultRepository(            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\Urkey
            _vaultDirectory);
            //vault = repo.LoadVault();

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

            MasterPasswordTextBox.TextChanged += (s, e) =>
            {
                if (_isPasswordVisible)
                    ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
            };

            //// Load Vault
            //_vaultPassword = VaultRepository.
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
                ContinueBtn_Click(sender, e);
            }
        }

        public void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        
        public void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }
    }
}
