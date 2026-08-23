using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class ActivityDisplayItem
    {
        public string Text { get; init; } = string.Empty;
        public string TimestampText { get; init; } = string.Empty;
    }

    public class HomeEntryCard
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string Initial { get; init; } = "?";
    }

    public class SetupTaskItem
    {
        public string Title { get; init; } = string.Empty;
        public bool IsComplete { get; init; }
        public string Action { get; init; } = string.Empty;
        public string StatusText => IsComplete ? Loc.Get("Setup_Done") : Loc.Get("Setup_Todo");
    }

    public class HomeViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;

        public HomeViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            ToggleSetupCommand = new RelayCommand<object>(_ => ToggleSetup());
            OpenEntryCommand = new RelayCommand<HomeEntryCard>(OpenEntry);
            Reload();
        }

        public ICommand ToggleSetupCommand { get; }
        public ICommand OpenEntryCommand { get; }

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

        private string _strongCountText = "0";
        public string StrongCountText { get => _strongCountText; private set => SetProperty(ref _strongCountText, value); }

        private string _weakCountText = "0";
        public string WeakCountText { get => _weakCountText; private set => SetProperty(ref _weakCountText, value); }

        private string _duplicateCountText = "0";
        public string DuplicateCountText { get => _duplicateCountText; private set => SetProperty(ref _duplicateCountText, value); }

        private string _compromisedCountText = "0";
        public string CompromisedCountText { get => _compromisedCountText; private set => SetProperty(ref _compromisedCountText, value); }

        private string _savedPasswordsHeading = string.Empty;
        public string SavedPasswordsHeading
        {
            get => _savedPasswordsHeading;
            private set => SetProperty(ref _savedPasswordsHeading, value);
        }

        private string _storageUsageText = string.Empty;
        public string StorageUsageText { get => _storageUsageText; private set => SetProperty(ref _storageUsageText, value); }

        private string _storageFilesText = string.Empty;
        public string StorageFilesText { get => _storageFilesText; private set => SetProperty(ref _storageFilesText, value); }

        private double _storagePercent;
        public double StoragePercent { get => _storagePercent; private set => SetProperty(ref _storagePercent, value); }

        private string _setupProgressText = string.Empty;
        public string SetupProgressText { get => _setupProgressText; private set => SetProperty(ref _setupProgressText, value); }

        private double _setupPercent;
        public double SetupPercent { get => _setupPercent; private set => SetProperty(ref _setupPercent, value); }

        private bool _isSetupExpanded = true;
        public bool IsSetupExpanded
        {
            get => _isSetupExpanded;
            set
            {
                if (!SetProperty(ref _isSetupExpanded, value)) return;
                OnPropertyChanged(nameof(IsSetupCollapsed));
                OnPropertyChanged(nameof(SetupToggleLabel));
                App.Settings.SetupPanelExpanded = value;
                SettingsHelper.SaveSettings(App.Settings);
            }
        }

        public bool IsSetupCollapsed => !IsSetupExpanded;
        public string SetupToggleLabel => Loc.Get(IsSetupExpanded ? "Setup_Collapse" : "Setup_Expand");

        public ObservableCollection<ActivityDisplayItem> RecentActivity { get; } = new();
        public ObservableCollection<HomeEntryCard> RecentEntries { get; } = new();
        public ObservableCollection<HomeEntryCard> FavoriteEntries { get; } = new();
        public ObservableCollection<SetupTaskItem> SetupTasks { get; } = new();

        public bool HasActivity => RecentActivity.Count > 0;
        public bool IsActivityEmpty => !HasActivity;
        public bool HasRecentEntries => RecentEntries.Count > 0;
        public bool IsRecentEmpty => !HasRecentEntries;
        public bool HasFavorites => FavoriteEntries.Count > 0;
        public bool IsFavoritesEmpty => !HasFavorites;

        public void Reload()
        {
            var entries = _vaultService.EnsureLoaded().Entries;

            int passwords = SecurityScoreCalculator.CountPasswords(entries);
            int score = SecurityScoreCalculator.Calculate(entries);
            var health = PasswordHealthAnalyzer.Analyze(entries);
            var storage = DocumentStorageStats.Calculate(entries, _vaultService.GetVaultDirectory());

            PasswordsTotalText = passwords.ToString(CultureInfo.CurrentCulture);
            VaultsTotalText = 1.ToString(CultureInfo.CurrentCulture);
            SecurityScoreText = string.Format(CultureInfo.CurrentCulture, "{0}%", score);
            StrongCountText = health.Strong.ToString(CultureInfo.CurrentCulture);
            WeakCountText = health.Weak.ToString(CultureInfo.CurrentCulture);
            DuplicateCountText = health.Duplicate.ToString(CultureInfo.CurrentCulture);
            CompromisedCountText = App.Settings.CheckCompromisedPasswords
                ? health.Compromised.ToString(CultureInfo.CurrentCulture)
                : "—";
            SavedPasswordsHeading = Loc.Format("Home_SavedPasswords", health.Total);
            StorageUsageText = Loc.Format(
                "Home_StorageUsage",
                DocumentStorageStats.FormatSize(storage.UsedBytes),
                DocumentStorageStats.FormatSize(storage.MaxTotalBytes));
            StorageFilesText = Loc.Format("Home_StorageFiles", storage.FileCount);
            StoragePercent = storage.Percent;
            IsSetupExpanded = App.Settings.SetupPanelExpanded;

            ReloadRecent(entries);
            ReloadFavorites(entries);
            ReloadActivity();
            ReloadSetup(entries, health);

            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(IsActivityEmpty));
            OnPropertyChanged(nameof(HasRecentEntries));
            OnPropertyChanged(nameof(IsRecentEmpty));
            OnPropertyChanged(nameof(HasFavorites));
            OnPropertyChanged(nameof(IsFavoritesEmpty));
            OnPropertyChanged(nameof(SetupToggleLabel));
            OnPropertyChanged(nameof(IsSetupCollapsed));
        }

        private void ReloadRecent(IReadOnlyList<VaultEntry> entries)
        {
            RecentEntries.Clear();
            foreach (var account in entries.OfType<AccountEntry>()
                         .OrderByDescending(VaultService.GetRecency)
                         .Take(6))
            {
                RecentEntries.Add(ToCard(account));
            }
        }

        private void ReloadFavorites(IReadOnlyList<VaultEntry> entries)
        {
            FavoriteEntries.Clear();
            foreach (var account in entries.OfType<AccountEntry>().Where(a => a.IsFavorite).Take(8))
                FavoriteEntries.Add(ToCard(account));
        }

        private void ReloadActivity()
        {
            RecentActivity.Clear();
            foreach (var item in _vaultService.GetRecentActivity(8))
            {
                RecentActivity.Add(new ActivityDisplayItem
                {
                    Text = FormatActivity(item),
                    TimestampText = item.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                });
            }
        }

        private void ReloadSetup(IReadOnlyList<VaultEntry> entries, PasswordHealthStats health)
        {
            bool hasAccount = entries.OfType<AccountEntry>().Any();
            bool autolockOn = App.Settings.AutoLockMinutes > 0;
            bool clipboardOn = App.Settings.ClipboardClearSeconds > 0;
            bool healthy = health.Total == 0 || (health.Weak == 0 && health.Duplicate == 0);

            SetupTasks.Clear();
            SetupTasks.Add(new SetupTaskItem { Title = Loc.Get("Setup_AddAccount"), IsComplete = hasAccount, Action = "add-account" });
            SetupTasks.Add(new SetupTaskItem { Title = Loc.Get("Setup_Import"), IsComplete = health.Total >= 3, Action = "import" });
            SetupTasks.Add(new SetupTaskItem { Title = Loc.Get("Setup_Autolock"), IsComplete = autolockOn, Action = "autolock" });
            SetupTasks.Add(new SetupTaskItem { Title = Loc.Get("Setup_PasswordCheck"), IsComplete = healthy && hasAccount, Action = "password-check" });
            SetupTasks.Add(new SetupTaskItem { Title = Loc.Get("Setup_Clipboard"), IsComplete = clipboardOn, Action = "clipboard" });

            int done = SetupTasks.Count(t => t.IsComplete);
            SetupPercent = SetupTasks.Count == 0 ? 100 : done * 100.0 / SetupTasks.Count;
            SetupProgressText = done == SetupTasks.Count
                ? Loc.Get("Setup_AllDone")
                : Loc.Format("Setup_Progress", done, SetupTasks.Count);
        }

        private void ToggleSetup() => IsSetupExpanded = !IsSetupExpanded;

        private void OpenEntry(HomeEntryCard? card)
        {
            if (card == null)
                return;

            var account = _vaultService.GetEntries().OfType<AccountEntry>().FirstOrDefault(a => a.Id == card.Id);
            if (account == null)
                return;

            _vaultService.TouchLastAccessed(account.Id);
            var editor = new Views.Windows.EditAccount(account)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            if (editor.ShowDialog() == true)
            {
                var validation = EntryValidator.ValidateAccount(account);
                if (validation.IsValid)
                    _vaultService.UpdateEntry(account);
                Reload();
            }
        }

        private static HomeEntryCard ToCard(AccountEntry account)
        {
            string title = EntryMetadata.GetDisplayName(account);
            return new HomeEntryCard
            {
                Id = account.Id,
                Title = title,
                Subtitle = EntryMetadata.GetSecondaryText(account),
                Initial = string.IsNullOrWhiteSpace(title) ? "?" : title.Trim()[..1].ToUpperInvariant()
            };
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
