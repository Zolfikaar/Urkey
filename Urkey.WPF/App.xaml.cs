using System.Windows;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views;
using Urkey.WPF.Views.Windows;
//using static System.Net.Mime.MediaTypeNames;

namespace Urkey.WPF
{
    public partial class App : Application
    {
        private bool _devMode = false;
        public static AppSettings Settings { get; private set; } = new();
        //private UserService _userService;
        public static VaultService VaultService;
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if(!_devMode)
            {


            Settings = SettingsHelper.LoadSettings();
            // initialize services
            //_userService = new UserService();
            VaultService = new VaultService();

            
            LanguageManager.ApplyLanguage(Settings.Language);
            ThemeManager.ApplyTheme(Settings.Theme);

            // منع التطبيق من الإغلاق التلقائي أثناء الفترة الانتقالية
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splashWindow = new Views.Windows.SplashScreen();


            await splashWindow.RunAsync();

            Window next;

            if (VaultService.VaultExists())
                next = new UnlockWindow(VaultService);
            
            else
                next = new FirstTimeSetupWindow(VaultService);

            Application.Current.MainWindow = next;
            next.Show();

            // الآن نرجع البرنامج لسلوك الإغلاق الطبيعي
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

            }
            else
            {
                Settings = SettingsHelper.LoadSettings();
                // initialize services
                //_userService = new UserService();
                VaultService = new VaultService();

                LanguageManager.ApplyLanguage(Settings.Language);
                ThemeManager.ApplyTheme(Settings.Theme);

                Window next = new MainWindow();
                Application.Current.MainWindow = next;
                next.Show();
            }

        }



        protected override void OnExit(ExitEventArgs e)
        {
            SettingsHelper.SaveSettings(Settings);
            base.OnExit(e);
        }

    }


}
