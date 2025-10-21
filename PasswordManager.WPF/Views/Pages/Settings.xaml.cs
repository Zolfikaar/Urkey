using System;
using System.Windows;
using System.Windows.Controls;
using PasswordManager.WPF.Helpers;

namespace PasswordManager.WPF.Views.Pages
{
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.FlowDirection = Application.Current.MainWindow.FlowDirection;
        }

        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {
            // تبديل اللغة الحالية
            string newLang = App.Settings.Language == "en" ? "ar" : "en";

            // تطبيق اللغة فوراً
            LanguageManager.ApplyLanguage(newLang);
            this.FlowDirection = Application.Current.MainWindow.FlowDirection;

            // تحديث الإعدادات
            App.Settings.Language = newLang;

            // حفظ التغييرات
            SettingsHelper.SaveSettings(App.Settings);

            // تحديث النصوص حسب اللغة الجديدة
            MessageBox.Show(newLang == "ar" ? "تم تغيير اللغة إلى العربية" : "Language changed to English");

        }

        private void OnThemeChanged(object sender, RoutedEventArgs e)
        {
            // تبديل الثيم الحالي
            string newTheme = App.Settings.Theme == "Light" ? "Dark" : "Light";

            // تطبيق الثيم فوراً
            ThemeManager.ApplyTheme(newTheme);

            // تحديث الإعدادات
            App.Settings.Theme = newTheme;

            // حفظ التغييرات
            SettingsHelper.SaveSettings(App.Settings);

            // رسالة تأكيد
            MessageBox.Show(newTheme == "Dark" ? "Dark theme applied" : "Light theme applied");

        }
    }
}
