using System;
using System.Linq;
using System.Windows;

namespace PasswordManager.WPF.Helpers
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            string themeFile = $"Resources/Themes/{themeName}.xaml";

            var existing = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
            if (existing != null)
                Application.Current.Resources.MergedDictionaries.Remove(existing);

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themeFile, UriKind.Relative)
            });
        }
    }
}