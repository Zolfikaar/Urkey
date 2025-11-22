using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            
            Loaded += SplashScreen_Loaded;
        }

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply language and set FlowDirection
            var langCode = App.Settings?.Language ?? "en";
            LanguageManager.ApplyLanguage(langCode);
            
            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(5000); // وقت الظهور قبل الانتقال

            var unlock = new UnlockWindow(); // شاشة كلمة المرور الرئيسية
            unlock.Show();
            this.Close();
        }
    }
}
