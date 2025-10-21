using System;
using System.Linq;
using System.Windows;

namespace PasswordManager.WPF.Helpers
{
    public static class ThemeManager
    {
        public static void ChangeTheme(string themeName = "LightTheme")
        {
            try
            {
                // مثال: themeName = "DarkTheme" أو "LightTheme"
                string themeFile = $"Resources/Themes/{themeName}.xaml";

                var existingTheme = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
                if (existingTheme != null)
                    Application.Current.Resources.MergedDictionaries.Remove(existingTheme);

                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(themeFile, UriKind.Relative)
                };

                Application.Current.Resources.MergedDictionaries.Add(newTheme);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing theme: {ex.Message}");
            }
        }
    }
}
