using System;
using System.IO;
using Urkey.Core.Paths;

namespace Urkey.WPF.Helpers
{
    public static class DiagnosticLog
    {
        public static void Info(string message) => Write("INFO", message);

        public static void Warn(string message) => Write("WARN", message);

        public static void Error(string message) => Write("ERROR", message);

        private static void Write(string level, string message)
        {
            if (App.Settings?.LogApplicationEvents != true)
                return;

            try
            {
                AppDataPaths.EnsureInitialized();
                string dir = Path.Combine(AppDataPaths.RootDirectory, "logs");
                Directory.CreateDirectory(dir);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(Path.Combine(dir, "urkey.log"), line);
            }
            catch
            {
                // never fail the app because of diagnostics
            }
        }
    }
}
