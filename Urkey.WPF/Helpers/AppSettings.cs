using System.Text.Json.Serialization;

namespace Urkey.WPF.Helpers
{
    public class AppSettings
    {
        public string Language { get; set; } = "en";

        /// <summary>Light, Dark, or System.</summary>
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

        public bool StartWithWindows { get; set; }

        public bool SortEntriesAlphabetically { get; set; }

        public bool CheckCompromisedPasswords { get; set; } = true;

        public bool LogApplicationEvents { get; set; }

        public bool FastHotkeyEnabled { get; set; } = true;

        public string FastHotkeyModifier { get; set; } = "Ctrl+Alt";

        public string FastHotkeyKey { get; set; } = "A";

        public bool AutosavePasswords { get; set; } = true;
        public bool AutofillPasswords { get; set; } = true;
        public bool AutosaveAddresses { get; set; } = true;
        public bool AutofillAddresses { get; set; } = true;
        public bool AutosaveBankCards { get; set; } = true;
        public bool AutofillBankCards { get; set; } = true;

        public bool SetupPanelExpanded { get; set; } = true;
    }
}
