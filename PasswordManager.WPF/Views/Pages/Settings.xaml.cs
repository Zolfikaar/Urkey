using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PasswordManager.WPF.Helpers;

namespace PasswordManager.WPF.Views.Pages
{

    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.FlowDirection = Application.Current.MainWindow.FlowDirection;

            // Initialize language toggle state
            LangToggle.IsChecked = LanguageManager.CurrentLanguage == LangCode.ar;
        }

        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {
            // Toggle between languages
            if (LangToggle.IsChecked == true)
            {
                LanguageManager.ChangeLanguage(LangCode.ar);
            }
            else
            {
                LanguageManager.ChangeLanguage(LangCode.en);
            }

            // Update flow direction for this page
            this.FlowDirection = Application.Current.MainWindow.FlowDirection;
        }

        private void OnThemeChanged(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeTheme("DarkTheme");
            // أو ThemeManager.ChangeTheme("LightTheme");
        }

        //public void ChangeTheme()
        //{

        //}

        //public void ChangeLanguage()
        //{

        //}
    }
}
