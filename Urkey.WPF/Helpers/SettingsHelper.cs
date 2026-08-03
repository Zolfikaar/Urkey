using System;
using System.IO;
using System.Text.Json;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Helpers
{
    public static class SettingsHelper
    {
        private static readonly string _settingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Urkey", "settings.json");

        public static AppSettings LoadSettings()
        {
            AppSettings settings;

            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                settings = JsonSerializer.Deserialize<AppSettings>(json)!;
            }
            else
            {
                settings = new AppSettings();
                SaveSettings(settings);
            }

            // DPAPI master-password storage is intentionally disabled (Phase 1 decision).
            settings.MasterPassword = null;

            return settings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

            // Persist non-secret settings only — never write the master password.
            settings.MasterPassword = null;
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
    }
}
