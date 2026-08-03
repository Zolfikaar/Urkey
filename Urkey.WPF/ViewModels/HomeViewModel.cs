using System.Collections.ObjectModel;
using System.Globalization;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class ActivityDisplayItem
    {
        public string Text { get; init; } = string.Empty;
        public string TimestampText { get; init; } = string.Empty;
    }

    public class HomeViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;

        public HomeViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            Reload();
        }

        private string _passwordsTotalText = "0";
        public string PasswordsTotalText
        {
            get => _passwordsTotalText;
            private set => SetProperty(ref _passwordsTotalText, value);
        }

        private string _vaultsTotalText = "1";
        public string VaultsTotalText
        {
            get => _vaultsTotalText;
            private set => SetProperty(ref _vaultsTotalText, value);
        }

        private string _securityScoreText = "100%";
        public string SecurityScoreText
        {
            get => _securityScoreText;
            private set => SetProperty(ref _securityScoreText, value);
        }

        public ObservableCollection<ActivityDisplayItem> RecentActivity { get; } = new();

        public bool HasActivity => RecentActivity.Count > 0;
        public bool IsActivityEmpty => !HasActivity;

        public void Reload()
        {
            var entries = _vaultService.EnsureLoaded().Entries;

            int passwords = SecurityScoreCalculator.CountPasswords(entries);
            int score = SecurityScoreCalculator.Calculate(entries);

            PasswordsTotalText = passwords.ToString(CultureInfo.CurrentCulture);
            // Single-vault architecture for now.
            VaultsTotalText = 1.ToString(CultureInfo.CurrentCulture);
            SecurityScoreText = string.Format(CultureInfo.CurrentCulture, "{0}%", score);

            RecentActivity.Clear();
            foreach (var item in _vaultService.GetRecentActivity(15))
            {
                RecentActivity.Add(new ActivityDisplayItem
                {
                    Text = FormatActivity(item),
                    TimestampText = item.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                });
            }

            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(IsActivityEmpty));
        }

        private static string FormatActivity(ActivityLogEntry item)
        {
            if (item.Action == EntryMetadata.ActionImported)
                return Loc.Format("Activity_Imported", item.EntryName);

            string actionKey = item.Action switch
            {
                EntryMetadata.ActionAdded => "Activity_Added",
                EntryMetadata.ActionEdited => "Activity_Edited",
                EntryMetadata.ActionDeleted => "Activity_Deleted",
                EntryMetadata.ActionUploaded => "Activity_Uploaded",
                _ => "Activity_Added"
            };

            string typeKey = item.EntryType switch
            {
                EntryMetadata.TypeAccount => "AccountType",
                EntryMetadata.TypeCard => "AccountTypeBankCard",
                EntryMetadata.TypeAddress => "AccountTypeAddress",
                EntryMetadata.TypeNote => "AccountTypeNote",
                EntryMetadata.TypeDocument => "AccountTypeDocument",
                _ => "AccountType"
            };

            return Loc.Format(actionKey, item.EntryName, Loc.Get(typeKey));
        }
    }
}
