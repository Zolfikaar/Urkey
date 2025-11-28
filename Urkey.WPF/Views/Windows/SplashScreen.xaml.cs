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
            
            //Loaded += SplashScreen_Loaded;
        }

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply language and set FlowDirection
            var langCode = App.Settings?.Language ?? "en";
            LanguageManager.ApplyLanguage(langCode);
            
            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Apply font based on language
            var fontFamily = langCode == "ar" ? new FontFamily("Cairo") : new FontFamily("LeagueSpartan");
            ApplyFontToWindow(this, fontFamily);
        }

        private void ApplyFontToWindow(DependencyObject parent, FontFamily fontFamily)
        {
            if (parent == null) return;

            // Apply font to current element based on type
            if (parent is TextBlock textBlock)
            {
                textBlock.FontFamily = fontFamily;
            }
            else if (parent is TextBox textBox)
            {
                textBox.FontFamily = fontFamily;
            }
            else if (parent is PasswordBox passwordBox)
            {
                passwordBox.FontFamily = fontFamily;
            }
            else if (parent is Button button)
            {
                button.FontFamily = fontFamily;
            }
            else if (parent is Label label)
            {
                label.FontFamily = fontFamily;
            }
            else if (parent is Control control)
            {
                control.FontFamily = fontFamily;
            }
            else if (parent is Window window)
            {
                window.FontFamily = fontFamily;
            }

            // Recursively apply to children
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ApplyFontToWindow(child, fontFamily);
            }
        }

        public async Task RunAsync()
        {
            if (!this.IsVisible) Show();// عرض النافذة
            await Task.Delay(3000);     // وقت الانتظار
            Close();                    // غلق النافذة


        }
    }
}
