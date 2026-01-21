using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using static Urkey.WPF.Views.MainWindow;

namespace Urkey.WPF.Views.Pages
{
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.Unloaded += Settings_Unloaded;
        }

        private void Settings_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from sidebar state changes to prevent memory leaks
            MainWindow.SidebarStateChanged -= MainWindow_SidebarStateChanged;
        }


        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {

            // حدّد اللغة الجديدة
            string newLang = App.Settings.Language == "en" ? "ar" : "en";

            // حدّد الاتجاه
            var newDirection = newLang == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            // حدّث الإعدادات أولاً
            App.Settings.Language = newLang;
            SettingsHelper.SaveSettings(App.Settings);

            // طبّق اللغة قبل إنشاء النافذة
            LanguageManager.ApplyLanguage(newLang);

            // أعد إنشاء النافذة
            RecreateMainWindow(newDirection, StartupPage.Settings);

            // UI helpers
            UpdateSidebarToggleText();

        }

        public static void RecreateMainWindow(FlowDirection direction, StartupPage startupPage)
        {
            var oldWindow = Application.Current.MainWindow;
            if (oldWindow == null)
                return;

            // أخفِ القديمة فوراً 
            oldWindow.Hide();

            // استخدم Dispatcher لتأجيل إنشاء النافذة الجديدة بعد انتهاء الحدث الحالي
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // إنشاء النافذة الجديدة مع نفس حجم وموقع القديمة
                var newWindow = new MainWindow(startupPage)
                {
                    Left = oldWindow.Left,
                    Top = oldWindow.Top,
                    Width = oldWindow.Width,
                    Height = oldWindow.Height,
                    WindowState = oldWindow.WindowState,
                    FlowDirection = direction
                };

                // عيّن MainWindow الجديدة قبل العرض
                Application.Current.MainWindow = newWindow;

                // عرض النافذة الجديدة
                newWindow.Show();

                // أغلق القديمة بعد العرض
                oldWindow.Close();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }





        //public static void ApplyFlowDirectionImmediately(FlowDirection direction)
        //{
        //    if (Application.Current?.MainWindow == null)
        //        return;

        //    var window = Application.Current.MainWindow;

        //    // افصل المحتوى مؤقتاً
        //    var content = window.Content;
        //    window.Content = null;

        //    // غيّر الاتجاه
        //    window.FlowDirection = direction;

        //    // رجّع المحتوى
        //    window.Content = content;

        //    // أجبر إعادة الحساب
        //    window.InvalidateVisual();
        //    window.UpdateLayout();
        //}


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

        // toggle sidebar state (open\closed)
        private void SidebarToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSidebar(true);
        }

        private void SidebarToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSidebar(false);
        }

        private void  ToggleSidebar(bool isExpanded)
        {
            // Get MainWindow
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            // Use explicit setter to ensure animation and persistence
            mainWindow.SetSidebarExpanded(isExpanded);

            // Update text immediately to reflect the intended state
            UpdateSidebarToggleText();
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
