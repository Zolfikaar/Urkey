using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Urkey.Core.Managers;
using Urkey.Core.Models;
using Urkey.Core.Paths;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using Urkey.WPF.Views.Pages;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Helpers
{
    /// <summary>
    /// Headless-ish capture of key UI surfaces for README marketing shots.
    /// Invoked via: Urkey.WPF.exe --capture-screenshots [outputDir]
    /// Uses an isolated temp AppData root so the user's real vault is untouched.
    /// </summary>
    internal static class ScreenshotCaptureRunner
    {
        private const string DemoMasterPassword = "UrKey-Demo-2026!";
        private const double CaptureWidth = 1360;
        private const double CaptureHeight = 820;

        public static async Task RunAsync(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            string isolatedRoot = Path.Combine(Path.GetTempPath(), "UrKey-Screenshot-" + Guid.NewGuid().ToString("N"));
            AppDataPaths.OverrideRootDirectory(isolatedRoot);

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                SeedDemoVault();

                App.Settings = new AppSettings
                {
                    Language = "en",
                    Theme = "Light",
                    SidebarExpanded = true,
                    AutoLockMinutes = 0,
                    ClipboardClearSeconds = 30
                };
                SettingsHelper.SaveSettings(App.Settings);
                App.VaultService = new VaultService();

                // Auth surfaces (English then Arabic)
                await CaptureAuthSurfacesAsync(outputDir);

                // Main shell pages
                await CaptureMainShellAsync(outputDir, "en");
                await CaptureMainShellAsync(outputDir, "ar");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(isolatedRoot))
                        Directory.Delete(isolatedRoot, recursive: true);
                }
                catch
                {
                    /* best-effort cleanup */
                }

                Application.Current.Shutdown(0);
            }
        }

        private static void SeedDemoVault()
        {
            var userService = new UserService();
            var setup = userService.FirstSetup(null, null, DemoMasterPassword);
            if (!setup.Success)
                throw new InvalidOperationException("Failed to seed demo user for screenshots.");

            userService.Save();

            EncryptionService.InitializeFromPassword(
                DemoMasterPassword,
                userService.GetSalt(),
                EncryptionService.CurrentKdfIterations);
            VaultManager.Unlock();

            var vaultService = new VaultService();
            vaultService.Load();

            string reused = "SharedPass!42";
            vaultService.AddEntry(new AccountEntry
            {
                ServiceName = "GitHub",
                Username = "demo.user",
                Email = "demo@urkey.app",
                Password = "Tr0ng-GitHub-Key#9fQ2mL8x",
                Url = "https://github.com",
                AccountType = "Website",
                IsFavorite = true
            });
            vaultService.AddEntry(new AccountEntry
            {
                ServiceName = "LinkedIn",
                Username = "demo.user",
                Email = "demo@urkey.app",
                Password = reused,
                Url = "https://linkedin.com",
                AccountType = "Website"
            });
            vaultService.AddEntry(new AccountEntry
            {
                ServiceName = "Legacy Router",
                Username = "admin",
                Email = string.Empty,
                Password = "1234",
                Url = "http://192.168.1.1",
                AccountType = "Website"
            });
            vaultService.AddEntry(new AccountEntry
            {
                ServiceName = "Work Portal",
                Username = "demo.user",
                Email = "demo@urkey.app",
                Password = reused,
                Url = "https://portal.example.com",
                AccountType = "Website"
            });
            vaultService.AddEntry(new AccountEntry
            {
                ServiceName = "Cloud Notes",
                Username = "notes-bot",
                Email = "notes@urkey.app",
                Password = "MediumPass99",
                Url = "https://notes.example.com",
                AccountType = "Website"
            });
            vaultService.AddEntry(new CardEntry
            {
                HolderName = "Demo User",
                Number = "4111111111111111",
                ExpiryDate = new DateOnly(2029, 8, 1),
                Cvv = "123",
                Bank = "Demo Bank",
                Notes = "Screenshot demo card"
            });
            vaultService.AddEntry(new NoteEntry
            {
                Title = "Recovery phrases",
                Content = "Demo note — never store real recovery data in screenshots."
            });

            vaultService.Save();
            VaultManager.Lock();
            EncryptionService.Clear();
        }

        private static async Task CaptureAuthSurfacesAsync(string outputDir)
        {
            foreach (var (lang, setupName, unlockName) in new[]
                     {
                         ("en", "setup-en.png", "unlock-en.png"),
                         ("ar", "setup-ar.png", "unlock-ar.png")
                     })
            {
                ApplyChrome(lang);

                // Setup window (visual only — vault already exists; we still show the UI)
                var setup = PrepareWindow(new FirstTimeSetupWindow(App.VaultService));
                await ShowAndCaptureAsync(setup, Path.Combine(outputDir, setupName));
                setup.Close();

                var unlock = PrepareWindow(new UnlockWindow(App.VaultService));
                await ShowAndCaptureAsync(unlock, Path.Combine(outputDir, unlockName));
                unlock.Close();
            }

            // Unlock crypto for main-shell captures
            var userService = new UserService();
            userService.Unlock(DemoMasterPassword);
            App.VaultService = new VaultService();
            App.VaultService.Load();
        }

        private static async Task CaptureMainShellAsync(string outputDir, string lang)
        {
            ApplyChrome(lang);
            string suffix = lang == "ar" ? "ar" : "en";

            var main = PrepareWindow(new MainWindow(MainWindow.StartupPage.Home));
            main.Show();
            await WaitForLayoutAsync(main);

            await NavigateAndCaptureAsync(main, new Home(), Path.Combine(outputDir, $"home-{suffix}.png"), "Home");
            await NavigateAndCaptureAsync(main, new PasswordCheck(), Path.Combine(outputDir, $"password-check-{suffix}.png"), "PasswordCheck");
            await NavigateAndCaptureAsync(main, new PasswordGenerator(), Path.Combine(outputDir, $"password-generator-{suffix}.png"), "PasswordGenerator");
            await NavigateAndCaptureAsync(main, new Settings { DataContext = main.DataContext }, Path.Combine(outputDir, $"settings-{suffix}.png"), "Settings");
            await NavigateAndCaptureAsync(main, new Accounts(), Path.Combine(outputDir, $"accounts-{suffix}.png"), "Accounts");
            await NavigateAndCaptureAsync(main, new CreditCards(), Path.Combine(outputDir, $"bank-cards-{suffix}.png"), "CreditCards");

            main.Close();
            await DispatcherYield();
        }

        private static void ApplyChrome(string lang)
        {
            App.Settings.Language = lang;
            App.Settings.Theme = "Light";
            App.Settings.SidebarExpanded = true;
            LanguageManager.ApplyLanguage(lang);
            ThemeManager.ApplyTheme("Light");
        }

        private static T PrepareWindow<T>(T window) where T : Window
        {
            window.WindowState = WindowState.Normal;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 40;
            window.Top = 40;
            window.Width = CaptureWidth;
            window.Height = CaptureHeight;
            window.ShowInTaskbar = false;
            return window;
        }

        private static async Task NavigateAndCaptureAsync(MainWindow main, Page page, string path, string sidebarButtonName)
        {
            main.NavigateToPage(page, sidebarButtonName);
            await WaitForLayoutAsync(main);
            // Allow async VM loads (Password Check, Accounts, etc.)
            await Task.Delay(450);
            await WaitForLayoutAsync(main);
            CaptureWindow(main, path);
        }

        private static async Task ShowAndCaptureAsync(Window window, string path)
        {
            window.Show();
            await WaitForLayoutAsync(window);
            await Task.Delay(250);
            await WaitForLayoutAsync(window);
            CaptureWindow(window, path);
        }

        private static async Task WaitForLayoutAsync(Window window)
        {
            window.UpdateLayout();
            await DispatcherYield();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            await DispatcherYield();
        }

        private static Task DispatcherYield()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Application.Current.Dispatcher.BeginInvoke(
                () => tcs.TrySetResult(),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            return tcs.Task;
        }

        private static void CaptureWindow(Window window, string path)
        {
            window.UpdateLayout();

            double dpi = VisualTreeHelper.GetDpi(window).PixelsPerDip;
            int pixelWidth = Math.Max(1, (int)Math.Round(window.ActualWidth * dpi));
            int pixelHeight = Math.Max(1, (int)Math.Round(window.ActualHeight * dpi));

            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * dpi, 96 * dpi, PixelFormats.Pbgra32);
            rtb.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var fs = File.Create(path);
            encoder.Save(fs);
        }
    }
}
