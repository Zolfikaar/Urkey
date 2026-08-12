using System;
using System.IO;
using System.Text.Json;
using Urkey.Core.Paths;

namespace Urkey.WPF.Helpers
{
    public static class SettingsHelper
    {
        private static string SettingsPath
        {
            get
            {
                AppDataPaths.EnsureInitialized();
                return AppDataPaths.SettingsFilePath;
            }
        }

        public static AppSettings LoadSettings()
        {
            AppSettings settings;
            string settingsPath = SettingsPath;

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
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
            string settingsPath = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            // Persist non-secret settings only — never write the master password.
            settings.MasterPassword = null;
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
    }
}
