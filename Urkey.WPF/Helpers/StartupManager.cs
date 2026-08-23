using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Urkey.WPF.Helpers
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Urkey";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(ValueName) is string;
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                                 ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
                if (key == null)
                    return;

                if (enabled)
                    key.SetValue(ValueName, Quote(GetExecutablePath()));
                else
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                DiagnosticLog.Warn("Could not update Windows startup: " + ex.Message);
                throw;
            }
        }

        private static string GetExecutablePath()
        {
            string? process = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(process))
                return process;

            return Process.GetCurrentProcess().MainModule?.FileName
                   ?? AppContext.BaseDirectory;
        }

        private static string Quote(string path)
            => path.Contains(' ', StringComparison.Ordinal) ? "\"" + path + "\"" : path;
    }
}
