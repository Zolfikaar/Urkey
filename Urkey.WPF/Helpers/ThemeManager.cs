using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Urkey.WPF.Helpers
{
    public static class ThemeManager
    {
        private static bool _watchingOsTheme;

        public static void Initialize()
        {
            ApplyTheme(App.Settings.Theme);
            EnsureOsThemeWatch();
        }

        public static void ApplyTheme(string themeName)
        {
            string resolved = ResolveThemeFile(themeName);
            string themeFile = $"Resources/Themes/{resolved}.xaml";

            var existing = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
            if (existing != null)
                Application.Current.Resources.MergedDictionaries.Remove(existing);

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themeFile, UriKind.Relative)
            });

            EnsureOsThemeWatch();
        }

        public static string ResolveThemeFile(string? themeName)
        {
            if (string.Equals(themeName, "System", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(themeName, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return IsOsLightTheme() ? "Light" : "Dark";
            }

            return string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
        }

        public static bool IsOsLightTheme()
        {
            try
            {
                object? value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    1);
                return value is not int i || i != 0;
            }
            catch
            {
                return true;
            }
        }

        private static void EnsureOsThemeWatch()
        {
            if (_watchingOsTheme)
                return;

            _watchingOsTheme = true;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General &&
                e.Category != UserPreferenceCategory.VisualStyle)
            {
                return;
            }

            if (!string.Equals(App.Settings?.Theme, "System", StringComparison.OrdinalIgnoreCase))
                return;

            Application.Current?.Dispatcher.InvokeAsync(() => ApplyTheme("System"));
        }
    }
}
