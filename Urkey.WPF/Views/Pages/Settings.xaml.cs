using System;
using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;

namespace Urkey.WPF.Views.Pages
{
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.FlowDirection = Application.Current.MainWindow.FlowDirection;
            this.Unloaded += Settings_Unloaded;
        }

        private void Settings_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from sidebar state changes to prevent memory leaks
            MainWindow.SidebarStateChanged -= MainWindow_SidebarStateChanged;
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

            // تحديث نص زر الشريط الجانبي بعد تغيير اللغة
            UpdateSidebarToggleText();

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

        private void SidebarToggle_Loaded(object sender, RoutedEventArgs e)
        {
            // Load initial state
            if (SidebarToggle != null)
            {
                SidebarToggle.IsChecked = App.Settings.SidebarExpanded;
                UpdateSidebarToggleText();
            }

            // Subscribe to sidebar state changes
            MainWindow.SidebarStateChanged += MainWindow_SidebarStateChanged;
        }

        private void MainWindow_SidebarStateChanged(object sender, bool isExpanded)
        {
            if (SidebarToggle != null && SidebarToggle.IsChecked != isExpanded)
            {
                SidebarToggle.IsChecked = isExpanded;
            }
            UpdateSidebarToggleText();
        }

        private void UpdateSidebarToggleText()
        {
            if (SidebarToggle != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                bool isExpanded = mainWindow?.IsSidebarExpanded ?? App.Settings.SidebarExpanded;
                SidebarToggle.Content = isExpanded
                    ? Application.Current.Resources["SidebarState_Expanded"]
                    : Application.Current.Resources["SidebarState_Shrunk"];
            }
        }

        private void SidebarToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSidebar(true);
            UpdateSidebarToggleText();
        }

        private void SidebarToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSidebar(false);
            UpdateSidebarToggleText();
        }

        private void ToggleSidebar(bool isExpanded)
        {
            // Get MainWindow
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            // Only toggle if the state is different
            if (mainWindow.IsSidebarExpanded != isExpanded)
            {
                // Trigger the sidebar toggle animation and state change
                var eventArgs = new RoutedEventArgs();
                mainWindow.ToggleSidebar_Click(mainWindow, eventArgs);
            }
            else
            {
                // State matches, but ensure it's saved
                App.Settings.SidebarExpanded = isExpanded;
                SettingsHelper.SaveSettings(App.Settings);
            }
        }

        private void ClipboardTimeButton_Click(object sender, RoutedEventArgs e)
        {
            ClipboardTimePopup.IsOpen = !ClipboardTimePopup.IsOpen;
        }

        private void ClipboardTime_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && ClipboardTimeButton != null)
            {
                // Get the resource key from the button's content (which is a DynamicResource binding)
                ClipboardTimeButton.Content = button.Content;
                ClipboardTimePopup.IsOpen = false;

                // Save the selection (skeleton - no logic yet)
                // TODO: Implement clipboard clear time logic
            }
        }
    }
}
