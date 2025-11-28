using System.Windows;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views.Windows;
using Urkey.Core.Managers;
using System.Threading.Tasks;

namespace Urkey.WPF
{
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; } = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            EncryptionService.Initialize();

            Settings = SettingsHelper.LoadSettings();

            LanguageManager.ApplyLanguage(Settings.Language);
            ThemeManager.ApplyTheme(Settings.Theme);

            // منع التطبيق من الإغلاق التلقائي أثناء الفترة الانتقالية
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splashWindow = new Views.Windows.SplashScreen();

            await splashWindow.RunAsync();

            Window next;

            if (VaultManager.VaultExists())
                next = new UnlockWindow();
            else
                next = new FirstTimeSetupWindow();

            next.Show();

            // الآن نرجع البرنامج لسلوك الإغلاق الطبيعي
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SettingsHelper.SaveSettings(Settings);
            base.OnExit(e);
        }

    }


}
