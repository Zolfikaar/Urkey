using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class PasswordIssueChip
    {
        public string Text { get; init; } = string.Empty;
        public bool IsReuse { get; init; }
        public bool IsFeature { get; init; }
    }

    public class PasswordIssueItem : ViewModelBase
    {
        public Guid AccountId { get; init; }
        public string ServiceName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string StrengthText { get; init; } = string.Empty;
        public string EntropyText { get; init; } = string.Empty;
        public string IssuesText { get; init; } = string.Empty;
        public string LengthText { get; init; } = string.Empty;
        public bool IsReused { get; init; }
        public bool HasIssues { get; init; }
        public PasswordStrengthLevel Level { get; init; }
        public double StrengthPercent { get; init; }
        public Brush StrengthBrush { get; init; } = Brushes.Gray;
        public Brush StrengthTrackBrush { get; init; } = Brushes.Gray;
        public IReadOnlyList<PasswordIssueChip> Chips { get; init; } = Array.Empty<PasswordIssueChip>();
        public IReadOnlyList<PasswordIssueChip> FeatureTags { get; init; } = Array.Empty<PasswordIssueChip>();
        public bool IsWeak => Level == PasswordStrengthLevel.Weak;
        public bool IsMedium => Level == PasswordStrengthLevel.Medium;
        public bool IsStrong => Level == PasswordStrengthLevel.Strong;

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (!SetProperty(ref _isEditing, value)) return;
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }

        public bool IsNotEditing => !IsEditing;

        private string _draftPassword = string.Empty;
        public string DraftPassword
        {
            get => _draftPassword;
            set => SetProperty(ref _draftPassword, value ?? string.Empty);
        }

        private bool _isDraftVisible;
        public bool IsDraftVisible
        {
            get => _isDraftVisible;
            set => SetProperty(ref _isDraftVisible, value);
        }
    }

    public class PasswordCheckViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;
        private readonly ObservableCollection<PasswordIssueItem> _allItems = new();

        public ICollectionView EntriesView { get; }

        public bool HasEntries => _allItems.Count > 0;
        public bool IsEmpty => !HasEntries;
        public bool HasVisibleEntries => EntriesView.Cast<object>().Any();

        private string _summaryText = string.Empty;
        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        private string _checkedCountText = "0";
        public string CheckedCountText
        {
            get => _checkedCountText;
            private set => SetProperty(ref _checkedCountText, value);
        }

        private string _strongCountText = "0";
        public string StrongCountText
        {
            get => _strongCountText;
            private set => SetProperty(ref _strongCountText, value);
        }

        private string _strongWithIssuesCountText = "0";
        public string StrongWithIssuesCountText
        {
            get => _strongWithIssuesCountText;
            private set => SetProperty(ref _strongWithIssuesCountText, value);
        }

        private string _issueCountText = "0";
        public string IssueCountText
        {
            get => _issueCountText;
            private set => SetProperty(ref _issueCountText, value);
        }

        private string _weakCountText = "0";
        public string WeakCountText
        {
            get => _weakCountText;
            private set => SetProperty(ref _weakCountText, value);
        }

        private string _reusedCountText = "0";
        public string ReusedCountText
        {
            get => _reusedCountText;
            private set => SetProperty(ref _reusedCountText, value);
        }

        private bool _showIssuesOnly;
        public bool ShowIssuesOnly
        {
            get => _showIssuesOnly;
            set
            {
                if (_showIssuesOnly == value) return;
                _showIssuesOnly = value;
                OnPropertyChanged(nameof(ShowIssuesOnly));
                EntriesView.Refresh();
                OnPropertyChanged(nameof(HasVisibleEntries));
            }
        }

        public ICommand ReloadCommand { get; }
        public ICommand ToggleIssuesOnlyCommand { get; }
        public ICommand BeginEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SavePasswordCommand { get; }
        public ICommand GenerateDraftCommand { get; }
        public ICommand ToggleDraftVisibilityCommand { get; }

        public PasswordCheckViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            EntriesView = CollectionViewSource.GetDefaultView(_allItems);
            EntriesView.Filter = FilterEntry;

            ReloadCommand = new RelayCommand<object>(_ => Reload());
            ToggleIssuesOnlyCommand = new RelayCommand<object>(_ => ShowIssuesOnly = !ShowIssuesOnly);
            BeginEditCommand = new RelayCommand<PasswordIssueItem>(BeginEdit, item => item != null);
            CancelEditCommand = new RelayCommand<PasswordIssueItem>(CancelEdit, item => item != null);
            SavePasswordCommand = new RelayCommand<PasswordIssueItem>(SavePassword, item => item != null);
            GenerateDraftCommand = new RelayCommand<PasswordIssueItem>(GenerateDraft, item => item?.IsEditing == true);
            ToggleDraftVisibilityCommand = new RelayCommand<PasswordIssueItem>(
                item => { if (item != null) item.IsDraftVisible = !item.IsDraftVisible; },
                item => item?.IsEditing == true);

            Reload();
        }

        private bool FilterEntry(object obj)
        {
            if (obj is not PasswordIssueItem item) return false;
            return !ShowIssuesOnly || item.HasIssues;
        }

        private void BeginEdit(PasswordIssueItem? item)
        {
            if (item == null) return;

            foreach (var other in _allItems.Where(i => i.IsEditing && i != item))
            {
                other.IsEditing = false;
                other.DraftPassword = string.Empty;
                other.IsDraftVisible = false;
            }

            var account = _vaultService.GetEntries().OfType<AccountEntry>()
                .FirstOrDefault(a => a.Id == item.AccountId);
            item.DraftPassword = account?.Password ?? string.Empty;
            item.IsDraftVisible = false;
            item.IsEditing = true;
        }

        private void CancelEdit(PasswordIssueItem? item)
        {
            if (item == null) return;
            item.IsEditing = false;
            item.DraftPassword = string.Empty;
            item.IsDraftVisible = false;
        }

        private void GenerateDraft(PasswordIssueItem? item)
        {
            if (item == null) return;
            item.DraftPassword = PasswordGeneratorService.Generate(new PasswordGeneratorOptions
            {
                Length = 16,
                UseLowercase = true,
                UseUppercase = true,
                UseDigits = true,
                UseSymbols = true
            });
            item.IsDraftVisible = true;
        }

        private void SavePassword(PasswordIssueItem? item)
        {
            if (item == null) return;
            if (string.IsNullOrWhiteSpace(item.DraftPassword))
            {
                ToastService.Warning(Loc.Get("PasswordCheck_PasswordRequired"));
                return;
            }

            var account = _vaultService.GetEntries().OfType<AccountEntry>()
                .FirstOrDefault(a => a.Id == item.AccountId);
            if (account == null)
            {
                ToastService.Error(Loc.Get("PasswordGenerator_Error"));
                return;
            }

            account.Password = item.DraftPassword.Trim();
            _vaultService.UpdateEntry(account);
            ToastService.Success(Loc.Get("PasswordCheck_PasswordUpdated"));
            Reload();
        }

        public void Reload()
        {
            _allItems.Clear();
            var accounts = _vaultService.GetEntries()
                .OfType<AccountEntry>()
                .Where(a => !string.IsNullOrEmpty(a.Password))
                .ToList();

            var reuseCounts = accounts
                .GroupBy(a => a.Password!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            int weak = 0, reused = 0, strong = 0, strongWithIssues = 0, issues = 0;

            Brush weakBrush = ResolveBrush("ErrorColor", Color.FromRgb(0xEF, 0x53, 0x50));
            Brush mediumBrush = ResolveBrush("AccentGold", Color.FromRgb(0xD4, 0xAF, 0x56));
            Brush strongBrush = ResolveBrush("SuccessColor", Color.FromRgb(0x81, 0xC7, 0x84));
            Brush trackBrush = ResolveBrush("BorderColor", Color.FromRgb(0x34, 0x36, 0x4A));

            foreach (var account in accounts)
            {
                var analysis = PasswordStrengthEvaluator.Analyze(account.Password);
                bool isReused = reuseCounts.TryGetValue(account.Password!, out int count) && count > 1;
                bool hasIssues = analysis.Level != PasswordStrengthLevel.Strong
                                 || isReused
                                 || analysis.IssueKeys.Count > 0;

                if (analysis.Level == PasswordStrengthLevel.Strong)
                {
                    strong++;
                    if (isReused || analysis.IssueKeys.Count > 0)
                        strongWithIssues++;
                }

                if (analysis.Level == PasswordStrengthLevel.Weak) weak++;
                if (isReused) reused++;
                if (hasIssues) issues++;

                var issueKeys = analysis.IssueKeys.ToList();
                if (isReused)
                    issueKeys.Add("PasswordCheck_Issue_Reused");

                string strengthKey = analysis.Level switch
                {
                    PasswordStrengthLevel.Strong => "PasswordCheck_Strength_Strong",
                    PasswordStrengthLevel.Medium => "PasswordCheck_Strength_Medium",
                    PasswordStrengthLevel.Weak => "PasswordCheck_Strength_Weak",
                    _ => "PasswordCheck_Strength_Empty"
                };

                Brush strengthBrush = analysis.Level switch
                {
                    PasswordStrengthLevel.Strong => strongBrush,
                    PasswordStrengthLevel.Medium => mediumBrush,
                    _ => weakBrush
                };

                double percent = analysis.Level switch
                {
                    PasswordStrengthLevel.Strong => Math.Max(78, Math.Min(100, analysis.EntropyBits / 80.0 * 100)),
                    PasswordStrengthLevel.Medium => Math.Max(40, Math.Min(72, analysis.EntropyBits / 80.0 * 100)),
                    PasswordStrengthLevel.Weak => Math.Max(8, Math.Min(35, analysis.EntropyBits / 80.0 * 100)),
                    _ => 0
                };

                var chips = issueKeys
                    .Distinct()
                    .Where(k => k != "PasswordCheck_Issue_Reused")
                    .Select(k => new PasswordIssueChip
                    {
                        Text = Loc.Get(k),
                        IsReuse = false
                    })
                    .ToList();

                var featureTags = new List<PasswordIssueChip>
                {
                    new() { Text = Loc.Format("PasswordCheck_Tag_Length", analysis.Length), IsFeature = true }
                };
                if (analysis.HasLower) featureTags.Add(new PasswordIssueChip { Text = Loc.Get("PasswordCheck_Tag_Lower"), IsFeature = true });
                if (analysis.HasUpper) featureTags.Add(new PasswordIssueChip { Text = Loc.Get("PasswordCheck_Tag_Upper"), IsFeature = true });
                if (analysis.HasDigit) featureTags.Add(new PasswordIssueChip { Text = Loc.Get("PasswordCheck_Tag_Digit"), IsFeature = true });
                if (analysis.HasSymbol) featureTags.Add(new PasswordIssueChip { Text = Loc.Get("PasswordCheck_Tag_Symbol"), IsFeature = true });
                if (isReused) featureTags.Add(new PasswordIssueChip { Text = Loc.Get("PasswordCheck_Issue_Reused"), IsReuse = true });

                _allItems.Add(new PasswordIssueItem
                {
                    AccountId = account.Id,
                    ServiceName = string.IsNullOrWhiteSpace(account.ServiceName)
                        ? Loc.Get("AccountType")
                        : account.ServiceName,
                    Username = string.IsNullOrWhiteSpace(account.Username)
                        ? (account.Email ?? string.Empty)
                        : account.Username,
                    StrengthText = Loc.Get(strengthKey),
                    EntropyText = Loc.Format("PasswordCheck_EntropyFormat", analysis.EntropyBits),
                    LengthText = Loc.Format("PasswordCheck_Tag_Length", analysis.Length),
                    IssuesText = string.Join(" • ", chips.Select(c => c.Text)),
                    IsReused = isReused,
                    HasIssues = hasIssues,
                    Level = analysis.Level,
                    StrengthPercent = percent,
                    StrengthBrush = strengthBrush,
                    StrengthTrackBrush = trackBrush,
                    Chips = chips,
                    FeatureTags = featureTags
                });
            }

            CheckedCountText = accounts.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
            StrongCountText = strong.ToString(System.Globalization.CultureInfo.CurrentCulture);
            StrongWithIssuesCountText = strongWithIssues.ToString(System.Globalization.CultureInfo.CurrentCulture);
            IssueCountText = issues.ToString(System.Globalization.CultureInfo.CurrentCulture);
            WeakCountText = weak.ToString(System.Globalization.CultureInfo.CurrentCulture);
            ReusedCountText = reused.ToString(System.Globalization.CultureInfo.CurrentCulture);
            SummaryText = Loc.Format("PasswordCheck_SummaryStrong", accounts.Count, strong, strongWithIssues, issues, weak, reused);
            EntriesView.Refresh();
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasVisibleEntries));
        }

        private static Brush ResolveBrush(string resourceKey, Color fallback)
        {
            try
            {
                if (System.Windows.Application.Current?.TryFindResource(resourceKey) is Brush brush)
                    return brush;
            }
            catch
            {
                // fall through
            }
            return new SolidColorBrush(fallback);
        }
    }
}
