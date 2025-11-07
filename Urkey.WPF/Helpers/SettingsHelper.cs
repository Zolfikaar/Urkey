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

            // نحمل كلمة المرور من التخزين الآمن (إن وجدت)
            settings.MasterPassword = SecureStorage.LoadSecure("MasterPassword");

            return settings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

            // نحفظ الإعدادات العامة فقط
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);

            // نخزن كلمة المرور إن وُجدت
            if (!string.IsNullOrEmpty(settings.MasterPassword))
                SecureStorage.SaveSecure("MasterPassword", settings.MasterPassword);
        }
    }
}
