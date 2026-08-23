using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using Urkey.WPF.Views.Windows;
using static Urkey.WPF.Views.MainWindow;

namespace Urkey.WPF.Views.Pages
{
    public partial class Settings : Page
    {
        private bool _suppressPrefEvents;

        public Settings()
        {
            InitializeComponent();
            Unloaded += Settings_Unloaded;
            Loaded += Settings_Loaded;
        }

        public void ShowCategory(string category)
        {
            foreach (var item in SettingsNav.Items)
            {
                if (item is ListBoxItem listItem &&
                    string.Equals(listItem.Tag as string, category, StringComparison.OrdinalIgnoreCase))
                {
                    SettingsNav.SelectedItem = listItem;
                    break;
                }
            }
        }

        private void Settings_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.SidebarStateChanged -= MainWindow_SidebarStateChanged;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            _suppressPrefEvents = true;
            try
            {
                PopulateAutoLockMinutes();
                PopulateHotkeyBoxes();
                ApplyClipboardSettingToUi();
                ApplyAutoLockSettingToUi();
                ApplyThemeRadios();
                ApplyAutofillPrefs();
                ApplyAdvancedPrefs();
                ApplyStartupCheck();

                if (SignOutButton != null)
                {
                    SignOutButton.Click -= OnSignOutClick;
                    SignOutButton.Click += OnSignOutClick;
                }
            }
            finally
            {
                _suppressPrefEvents = false;
            }
        }

        private void SettingsNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GeneralPanel == null || ImportPanel == null || AdvancedPanel == null)
                return;

            string tag = (SettingsNav.SelectedItem as ListBoxItem)?.Tag as string ?? "General";
            GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            ImportPanel.Visibility = tag == "ImportExport" ? Visibility.Visible : Visibility.Collapsed;
            AdvancedPanel.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {
            string newLang = App.Settings.Language == "en" ? "ar" : "en";
            var newDirection = newLang == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            App.Settings.Language = newLang;
            SettingsHelper.SaveSettings(App.Settings);
            LanguageManager.ApplyLanguage(newLang);
            RecreateMainWindow(newDirection, StartupPage.Settings);
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

        private void ThemeRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents || sender is not RadioButton radio || radio.IsChecked != true)
                return;

            string theme = radio.Name switch
            {
                "ThemeSystemRadio" => "System",
                "ThemeDarkRadio" => "Dark",
                _ => "Light"
            };

            ThemeManager.ApplyTheme(theme);
            App.Settings.Theme = theme;
            Persist();

            string toastKey = theme switch
            {
                "Dark" => "Theme_Applied_Dark",
                "System" => "Theme_Applied_System",
                _ => "Theme_Applied_Light"
            };
            ToastService.Success(Loc.Get(toastKey));
            DiagnosticLog.Info("Theme changed to " + theme);
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

        private void OnExportCsvClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmPlaintextExport())
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = Loc.Get("ExportCsv_SaveTitle"),
                Filter = Loc.Get("ExportCsv_FileFilter"),
                FileName = "urkey-accounts.csv",
                AddExtension = true,
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            try
            {
                int count = VaultExportService.ExportAccountsCsv(App.VaultService.GetEntries(), dialog.FileName);
                ToastService.Success(Loc.Format("Export_Success", count));
                DiagnosticLog.Info($"Exported {count} accounts to CSV");
            }
            catch (Exception ex)
            {
                ToastService.Error(Loc.Format("Export_Failed", ex.Message));
            }
        }

        private void OnExportTextClick(object sender, RoutedEventArgs e)
        {
            if (!ConfirmPlaintextExport())
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = Loc.Get("ExportText_SaveTitle"),
                Filter = Loc.Get("ExportText_FileFilter"),
                FileName = "urkey-export.txt",
                AddExtension = true,
                DefaultExt = ".txt"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            try
            {
                int count = VaultExportService.ExportToText(App.VaultService.GetEntries(), dialog.FileName);
                ToastService.Success(Loc.Format("Export_Success", count));
                DiagnosticLog.Info($"Exported {count} entries to text");
            }
            catch (Exception ex)
            {
                ToastService.Error(Loc.Format("Export_Failed", ex.Message));
            }
        }

        private void OnBackupClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = Loc.Get("Backup_SelectFolder")
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            try
            {
                string dest = Path.Combine(
                    dialog.FolderName,
                    "UrkeyBackup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                VaultExportService.CreateBackup(App.VaultService.GetVaultDirectory(), dest);
                ToastService.Success(Loc.Format("Backup_Success", dest));
                DiagnosticLog.Info("Created encrypted backup at " + dest);
            }
            catch (Exception ex)
            {
                ToastService.Error(Loc.Format("Backup_Failed", ex.Message));
            }
        }

        private void OnSortAzClick(object sender, RoutedEventArgs e)
        {
            App.VaultService.SortEntriesAlphabetically();
            App.Settings.SortEntriesAlphabetically = true;
            Persist();
            ToastService.Success(Loc.Get("SortAz_Done"));
        }

        private void AutoLockMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            if (AutoLockNeverRadio.IsChecked == true)
            {
                App.Settings.AutoLockMinutes = 0;
                AutoLockMinutesBox.IsEnabled = false;
            }
            else
            {
                AutoLockMinutesBox.IsEnabled = true;
                App.Settings.AutoLockMinutes = ReadAutoLockMinutes();
            }

            Persist();
            IdleLockService.NotifyActivity();
        }

        private void AutoLockMinutesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressPrefEvents || AutoLockNeverRadio.IsChecked == true)
                return;

            App.Settings.AutoLockMinutes = ReadAutoLockMinutes();
            Persist();
            IdleLockService.NotifyActivity();
        }

        private int ReadAutoLockMinutes()
        {
            return AutoLockMinutesBox.SelectedIndex switch
            {
                0 => 1,
                1 => 5,
                2 => 15,
                3 => 30,
                4 => 60,
                _ => 5
            };
        }

        private void ApplyAutoLockSettingToUi()
        {
            int minutes = App.Settings.AutoLockMinutes;
            AutoLockNeverRadio.IsChecked = minutes <= 0;
            AutoLockIdleRadio.IsChecked = minutes > 0;
            AutoLockMinutesBox.IsEnabled = minutes > 0;
            AutoLockMinutesBox.SelectedIndex = minutes switch
            {
                1 => 0,
                5 => 1,
                15 => 2,
                30 => 3,
                60 => 4,
                _ => minutes > 0 ? 1 : 1
            };
        }

        private void PopulateAutoLockMinutes()
        {
            if (AutoLockMinutesBox.Items.Count > 0)
                return;

            AutoLockMinutesBox.Items.Add(Loc.Get("AutoLock_1Minute"));
            AutoLockMinutesBox.Items.Add(Loc.Get("AutoLock_5Minutes"));
            AutoLockMinutesBox.Items.Add(Loc.Get("AutoLock_15Minutes"));
            AutoLockMinutesBox.Items.Add(Loc.Get("AutoLock_30Minutes"));
            AutoLockMinutesBox.Items.Add(Loc.Get("AutoLock_60Minutes"));
        }

        private void ApplyThemeRadios()
        {
            string theme = App.Settings.Theme ?? "Light";
            ThemeSystemRadio.IsChecked = theme.Equals("System", StringComparison.OrdinalIgnoreCase);
            ThemeDarkRadio.IsChecked = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            ThemeLightRadio.IsChecked = !theme.Equals("System", StringComparison.OrdinalIgnoreCase)
                                        && !theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        }

        private void SidebarToggle_Loaded(object sender, RoutedEventArgs e)
        {
            if (SidebarToggle != null)
            {
                SidebarToggle.IsChecked = App.Settings.SidebarExpanded;
                UpdateSidebarToggleText();
            }

            MainWindow.SidebarStateChanged += MainWindow_SidebarStateChanged;
        }

        private void MainWindow_SidebarStateChanged(object? sender, bool isExpanded)
        {
            if (SidebarToggle != null && SidebarToggle.IsChecked != isExpanded)
                SidebarToggle.IsChecked = isExpanded;
            UpdateSidebarToggleText();
        }

        private void UpdateSidebarToggleText()
        {
            if (SidebarToggle == null)
                return;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            bool isExpanded = mainWindow?.IsSidebarExpanded ?? App.Settings.SidebarExpanded;
            SidebarToggle.Content = isExpanded
                ? Application.Current.Resources["SidebarState_Expanded"]
                : Application.Current.Resources["SidebarState_Shrunk"];
        }

        private void SidebarToggle_Checked(object sender, RoutedEventArgs e) => ToggleSidebar(true);

        private void SidebarToggle_Unchecked(object sender, RoutedEventArgs e) => ToggleSidebar(false);

        private void ToggleSidebar(bool isExpanded)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;
            mainWindow.SetSidebarExpanded(isExpanded);
            UpdateSidebarToggleText();
        }

        private void ClipboardTimeButton_Click(object sender, RoutedEventArgs e)
        {
            ClipboardTimePopup.IsOpen = !ClipboardTimePopup.IsOpen;
        }

        private void OnSignOutClick(object sender, RoutedEventArgs e)
        {
            if (!EntryDialogHelper.ConfirmSignOut())
                return;

            DiagnosticLog.Info("User locked the vault");
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
            if (sender is not Button button || ClipboardTimeButton == null)
                return;

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

            Persist();
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

        private void StartupCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            bool enabled = StartupCheck.IsChecked == true;
            try
            {
                StartupManager.SetEnabled(enabled);
                App.Settings.StartWithWindows = enabled;
                Persist();
            }
            catch (Exception ex)
            {
                _suppressPrefEvents = true;
                StartupCheck.IsChecked = App.Settings.StartWithWindows;
                _suppressPrefEvents = false;
                ToastService.Error(Loc.Format("StartupSettings_Failed", ex.Message));
            }
        }

        private void ApplyStartupCheck()
        {
            bool enabled = App.Settings.StartWithWindows || StartupManager.IsEnabled();
            App.Settings.StartWithWindows = enabled;
            StartupCheck.IsChecked = enabled;
        }

        private void AutofillPref_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            App.Settings.AutosavePasswords = AutosavePasswordsCheck.IsChecked == true;
            App.Settings.AutofillPasswords = AutofillPasswordsCheck.IsChecked == true;
            App.Settings.AutosaveAddresses = AutosaveAddressesCheck.IsChecked == true;
            App.Settings.AutofillAddresses = AutofillAddressesCheck.IsChecked == true;
            App.Settings.AutosaveBankCards = AutosaveBankCardsCheck.IsChecked == true;
            App.Settings.AutofillBankCards = AutofillBankCardsCheck.IsChecked == true;
            Persist();
        }

        private void ApplyAutofillPrefs()
        {
            AutosavePasswordsCheck.IsChecked = App.Settings.AutosavePasswords;
            AutofillPasswordsCheck.IsChecked = App.Settings.AutofillPasswords;
            AutosaveAddressesCheck.IsChecked = App.Settings.AutosaveAddresses;
            AutofillAddressesCheck.IsChecked = App.Settings.AutofillAddresses;
            AutosaveBankCardsCheck.IsChecked = App.Settings.AutosaveBankCards;
            AutofillBankCardsCheck.IsChecked = App.Settings.AutofillBankCards;
        }

        private void CompromisedToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            App.Settings.CheckCompromisedPasswords = CompromisedToggle.IsChecked == true;
            Persist();
        }

        private void LogEventsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            App.Settings.LogApplicationEvents = LogEventsCheck.IsChecked == true;
            Persist();
            DiagnosticLog.Info("Diagnostic logging " + (App.Settings.LogApplicationEvents ? "enabled" : "disabled"));
        }

        private void FastHotkey_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            ApplyHotkeyFromUi();
        }

        private void FastHotkey_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressPrefEvents)
                return;

            ApplyHotkeyFromUi();
        }

        private void ApplyHotkeyFromUi()
        {
            App.Settings.FastHotkeyEnabled = FastHotkeyCheck.IsChecked == true;
            App.Settings.FastHotkeyModifier = HotkeyModifierBox.SelectedItem as string ?? "Ctrl+Alt";
            App.Settings.FastHotkeyKey = HotkeyKeyBox.SelectedItem as string ?? "A";
            Persist();
            (Application.Current.MainWindow as MainWindow)?.RefreshFastHotkey();
        }

        private void ApplyAdvancedPrefs()
        {
            CompromisedToggle.IsChecked = App.Settings.CheckCompromisedPasswords;
            LogEventsCheck.IsChecked = App.Settings.LogApplicationEvents;
            FastHotkeyCheck.IsChecked = App.Settings.FastHotkeyEnabled;
        }

        private void PopulateHotkeyBoxes()
        {
            if (HotkeyModifierBox.Items.Count == 0)
            {
                HotkeyModifierBox.Items.Add("Ctrl+Alt");
                HotkeyModifierBox.Items.Add("Ctrl+Shift");
                HotkeyModifierBox.Items.Add("Alt+Shift");
            }

            if (HotkeyKeyBox.Items.Count == 0)
            {
                foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                    HotkeyKeyBox.Items.Add(letter.ToString());
            }

            HotkeyModifierBox.SelectedItem = App.Settings.FastHotkeyModifier;
            if (HotkeyModifierBox.SelectedItem == null)
                HotkeyModifierBox.SelectedIndex = 0;

            HotkeyKeyBox.SelectedItem = App.Settings.FastHotkeyKey;
            if (HotkeyKeyBox.SelectedItem == null)
                HotkeyKeyBox.SelectedIndex = 0;
        }

        private static bool ConfirmPlaintextExport()
        {
            return ToastService.Confirm(
                Loc.Get("Export_ConfirmMessage"),
                Loc.Get("Export_ConfirmTitle"));
        }

        private static void Persist() => SettingsHelper.SaveSettings(App.Settings);
    }
}
