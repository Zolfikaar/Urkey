using System.Text.Json.Serialization;

namespace Urkey.WPF.Helpers
{
    public class AppSettings
    {
        public string Language { get; set; } = "en";
        public string Theme { get; set; } = "Light";
        public string DataPath { get; set; } = "data/passwords.json";
        public int ClipboardClearSeconds { get; set; } = 10;

        /// <summary>
        /// Idle minutes before auto-lock. 0 = never.
        /// </summary>
        public int AutoLockMinutes { get; set; } = 5;

        // DPAPI master-password storage remains disabled.
        [JsonIgnore]
        public string? MasterPassword { get; set; }

        public bool SidebarExpanded { get; set; } = true;
    }
}
