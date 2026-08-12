using System;
using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using Urkey.WPF.Views.Windows;
using static Urkey.WPF.Views.MainWindow;

namespace Urkey.WPF.Views.Pages
{
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.Unloaded += Settings_Unloaded;
            Loaded += Settings_Loaded;
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
            _ = startupPage;
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
                return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindow.FlowDirection = direction;
                mainWindow.InvalidateVisual();
                mainWindow.UpdateLayout();
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

            ToastService.Success(Loc.Get(newTheme == "Dark" ? "Theme_Applied_Dark" : "Theme_Applied_Light"));
        }

        private void OnChangeMasterPasswordClick(object sender, RoutedEventArgs e)
        {
            var win = new ChangeMasterPasswordWindow
            {
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        private void OnImportPasswordsClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Get("Import_SelectFileTitle"),
                Filter = Loc.Get("Import_FileFilter"),
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            var win = new ImportPasswordsWindow(dialog.FileName)
            {
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        private void AutoLockButton_Click(object sender, RoutedEventArgs e)
        {
            AutoLockPopup.IsOpen = !AutoLockPopup.IsOpen;
        }

        private void AutoLock_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || AutoLockButton == null)
                return;

            AutoLockButton.Content = button.Content;
            AutoLockPopup.IsOpen = false;

            App.Settings.AutoLockMinutes = button.Name switch
            {
                "AutoLock1" => 1,
                "AutoLock5" => 5,
                "AutoLock15" => 15,
                "AutoLock30" => 30,
                "AutoLockNever" => 0,
                _ => App.Settings.AutoLockMinutes
            };

            SettingsHelper.SaveSettings(App.Settings);
            IdleLockService.NotifyActivity();
        }

        private void ApplyAutoLockSettingToUi()
        {
            if (AutoLockButton == null)
                return;

            var key = App.Settings.AutoLockMinutes switch
            {
                1 => "AutoLock_1Minute",
                5 => "AutoLock_5Minutes",
                15 => "AutoLock_15Minutes",
                30 => "AutoLock_30Minutes",
                0 => "AutoLock_Never",
                _ => "AutoLock_5Minutes"
            };

            AutoLockButton.Content = TryFindResource(key)
                                     ?? Application.Current.TryFindResource(key)
                                     ?? key;
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

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyClipboardSettingToUi();
            ApplyAutoLockSettingToUi();

            if (SignOutButton != null)
            {
                SignOutButton.Click -= OnSignOutClick;
                SignOutButton.Click += OnSignOutClick;
            }
        }

        private void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            if (!EntryDialogHelper.ConfirmSignOut())
                return;

            App.LockAndShowUnlockWindow();
        }

        private void OnResetAppDataClick(object sender, RoutedEventArgs e)
        {
            if (!EntryDialogHelper.ConfirmResetApplicationData())
                return;

            try
            {
                App.ResetApplicationDataAndShowSetup();
            }
            catch (Exception ex)
            {
                ToastService.Error(Loc.Format("ResetAppData_Failed", ex.Message));
            }
        }

        private void ClipboardTime_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && ClipboardTimeButton != null)
            {
                // Get the resource key from the button's content (which is a DynamicResource binding)
                ClipboardTimeButton.Content = button.Content;
                ClipboardTimePopup.IsOpen = false;

                App.Settings.ClipboardClearSeconds = button.Name switch
                {
                    "ClipboardTime10" => 10,
                    "ClipboardTime30" => 30,
                    "ClipboardTime60" => 60,
                    "ClipboardTimeNever" => 0,
                    _ => App.Settings.ClipboardClearSeconds
                };

                SettingsHelper.SaveSettings(App.Settings);
            }
        }

        private void ApplyClipboardSettingToUi()
        {
            if (ClipboardTimeButton == null)
                return;

            var key = App.Settings.ClipboardClearSeconds switch
            {
                10 => "ClipboardTime_10Seconds",
                30 => "ClipboardTime_30Seconds",
                60 => "ClipboardTime_1Minute",
                0 => "ClipboardTime_Never",
                _ => "ClipboardTime_10Seconds"
            };

            ClipboardTimeButton.Content = TryFindResource(key)
                                          ?? Application.Current.TryFindResource(key)
                                          ?? key;
        }
    }
}
