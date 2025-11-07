using System.Windows;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF
{
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // تهيئة مؤقتة لخدمة التشفير أثناء التطوير
            EncryptionService.Initialize();

            Settings = SettingsHelper.LoadSettings();

            LanguageManager.ApplyLanguage(Settings.Language);
            ThemeManager.ApplyTheme(Settings.Theme);

            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
        }


        protected override void OnExit(ExitEventArgs e)
        {
            SettingsHelper.SaveSettings(Settings);
            base.OnExit(e);
        }

        

    }



}
