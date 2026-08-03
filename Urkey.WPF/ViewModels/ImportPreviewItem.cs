using Urkey.Core.Services.Import;

namespace Urkey.WPF.ViewModels
{
    public sealed class ImportPreviewItem : ViewModelBase
    {
        private bool _isSelected = true;
        private DuplicateResolution _duplicateResolution = DuplicateResolution.Skip;
        private bool _isDuplicate;
        private Guid? _existingEntryId;

        public required ImportCandidate Candidate { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsDuplicate
        {
            get => _isDuplicate;
            set
            {
                if (SetProperty(ref _isDuplicate, value))
                    OnPropertyChanged(nameof(DuplicateBadgeVisible));
            }
        }

        public Guid? ExistingEntryId
        {
            get => _existingEntryId;
            set => SetProperty(ref _existingEntryId, value);
        }

        public DuplicateResolution DuplicateResolution
        {
            get => _duplicateResolution;
            set => SetProperty(ref _duplicateResolution, value);
        }

        public string ServiceName => Candidate.ServiceName;
        public string Url => Candidate.Url;
        public string Username => string.IsNullOrWhiteSpace(Candidate.Username)
            ? Candidate.Email
            : Candidate.Username;
        public string MaskedPassword => string.IsNullOrEmpty(Candidate.Password) ? string.Empty : "••••••••";
        public bool HasError => Candidate.HasError;
        public bool DuplicateBadgeVisible => IsDuplicate;
        public int SourceRowNumber => Candidate.SourceRowNumber;
    }

    public sealed class ImportColumnOption
    {
        public int Index { get; init; }
        public string Display { get; init; } = string.Empty;

        public static ImportColumnOption None(string noneLabel) => new()
        {
            Index = -1,
            Display = noneLabel
        };
    }
}
