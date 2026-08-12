using System.Windows;
using Urkey.Core.Managers;
using Urkey.Core.Paths;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF
{
    public partial class App : Application
    {
        private bool _devMode = false;
        private static bool _suppressSettingsSaveOnExit;

        public static AppSettings Settings { get; private set; } = new();
        public static VaultService VaultService = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDataPaths.EnsureInitialized();

            if (!_devMode)
            {
                Settings = SettingsHelper.LoadSettings();
                VaultService = new VaultService();

                LanguageManager.ApplyLanguage(Settings.Language);
                ThemeManager.ApplyTheme(Settings.Theme);

                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var splashWindow = new Views.Windows.SplashScreen();
                await splashWindow.RunAsync();

                Window next = VaultService.VaultExists()
                    ? new UnlockWindow(VaultService)
                    : new FirstTimeSetupWindow(VaultService);

                Application.Current.MainWindow = next;
                next.Show();

                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            else
            {
                Settings = SettingsHelper.LoadSettings();
                VaultService = new VaultService();

                LanguageManager.ApplyLanguage(Settings.Language);
                ThemeManager.ApplyTheme(Settings.Theme);

                Window next = new MainWindow();
                Application.Current.MainWindow = next;
                next.Show();
            }
        }

        /// <summary>
        /// Clear decrypted session state and return to the unlock screen.
        /// </summary>
        public static void LockAndShowUnlockWindow()
        {
            IdleLockService.Stop();
            ClipboardHelper.CancelPendingClear();
            try { Clipboard.Clear(); } catch { /* ignore */ }

            VaultService?.ClearSession();
            FileHelper.CleanupTempDocumentImages();

            var currentMain = Current.MainWindow;
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var unlock = new UnlockWindow(VaultService);
            Current.MainWindow = unlock;
            unlock.Show();

            if (currentMain != null && !ReferenceEquals(currentMain, unlock))
            {
                currentMain.Close();
            }

            Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }

        /// <summary>
        /// Wipe all local app data (vault, documents, settings) and open first-run setup.
        /// Clears in-memory crypto/session state before deleting files to avoid locked handles.
        /// </summary>
        public static void ResetApplicationDataAndShowSetup()
        {
            IdleLockService.Stop();
            ClipboardHelper.CancelPendingClear();
            try { Clipboard.Clear(); } catch { /* ignore */ }

            // Drop decrypted vault + encryption key before touching disk.
            VaultService?.ClearSession();
            FileHelper.CleanupTempDocumentImages();

            // Close owned dialogs that might still reference vault files.
            CloseSecondaryWindows();

            _suppressSettingsSaveOnExit = true;
            try
            {
                AppDataPaths.ClearAllLocalData();
            }
            finally
            {
                _suppressSettingsSaveOnExit = false;
            }

            Settings = new AppSettings();
            VaultService = new VaultService();

            LanguageManager.ApplyLanguage(Settings.Language);
            ThemeManager.ApplyTheme(Settings.Theme);

            var currentMain = Current.MainWindow;
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var setup = new FirstTimeSetupWindow(VaultService);
            Current.MainWindow = setup;
            setup.Show();

            if (currentMain != null && !ReferenceEquals(currentMain, setup))
            {
                currentMain.Close();
            }

            Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }

        private static void CloseSecondaryWindows()
        {
            var main = Current.MainWindow;
            foreach (Window window in Current.Windows.Cast<Window>().ToList())
            {
                if (ReferenceEquals(window, main))
                    continue;

                try { window.Close(); }
                catch { /* ignore */ }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            VaultManager.Lock();
            FileHelper.CleanupTempDocumentImages();
            if (!_suppressSettingsSaveOnExit)
                SettingsHelper.SaveSettings(Settings);
            base.OnExit(e);
        }
    }
}
