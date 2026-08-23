using System.Collections.ObjectModel;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class CardsViewModel : ViewModelBase, ISupportsViewMode
    {
        private readonly VaultService _vaultService;

        public ObservableCollection<CardEntry> Items { get; } = new();

        private CardEntry? _selectedItem;
        public CardEntry? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value)) return;
                RevealNumber = false;
                RevealCvv = false;
                RevealPin = false;
                NotifyCardDisplay();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _revealNumber;
        public bool RevealNumber
        {
            get => _revealNumber;
            set
            {
                if (!SetProperty(ref _revealNumber, value)) return;
                OnPropertyChanged(nameof(DisplayedNumber));
            }
        }

        private bool _revealCvv;
        public bool RevealCvv
        {
            get => _revealCvv;
            set
            {
                if (!SetProperty(ref _revealCvv, value)) return;
                OnPropertyChanged(nameof(DisplayedCvv));
            }
        }

        private bool _revealPin;
        public bool RevealPin
        {
            get => _revealPin;
            set
            {
                if (!SetProperty(ref _revealPin, value)) return;
                OnPropertyChanged(nameof(DisplayedPin));
            }
        }

        public string SelectedMaskedNumber
            => SelectedItem == null ? string.Empty : EntryMetadata.MaskCard(SelectedItem.Number);

        public string DisplayedNumber
        {
            get
            {
                if (SelectedItem == null) return string.Empty;
                return RevealNumber ? FormatCardNumber(SelectedItem.Number) : EntryMetadata.MaskCard(SelectedItem.Number);
            }
        }

        public string DisplayedCvv
        {
            get
            {
                if (SelectedItem == null || string.IsNullOrWhiteSpace(SelectedItem.Cvv))
                    return "•••";
                return RevealCvv ? SelectedItem.Cvv! : new string('•', Math.Max(3, SelectedItem.Cvv!.Length));
            }
        }

        public string DisplayedPin
        {
            get
            {
                if (SelectedItem == null || string.IsNullOrWhiteSpace(SelectedItem.Pin))
                    return "••••";
                return RevealPin ? SelectedItem.Pin! : new string('•', Math.Max(4, SelectedItem.Pin!.Length));
            }
        }

        public string DisplayedExpiry
            => SelectedItem == null
                ? string.Empty
                : SelectedItem.ExpiryDate.ToString("MM/yy");

        public bool HasSelection => SelectedItem != null;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
                Reload();
            }
        }

        private bool _isListView = true;
        public bool IsListView
        {
            get => _isListView;
            set
            {
                if (!SetProperty(ref _isListView, value)) return;
                OnPropertyChanged(nameof(IsGridView));
            }
        }

        public bool IsGridView => !_isListView;

        public bool IsEmpty => Items.Count == 0;

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand ToggleNumberCommand { get; }
        public ICommand ToggleCvvCommand { get; }
        public ICommand TogglePinCommand { get; }
        public ICommand CopyNumberCommand { get; }
        public ICommand CopyCvvCommand { get; }
        public ICommand CopyPinCommand { get; }
        public ICommand CopyHolderCommand { get; }

        public CardsViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            AddCommand = new RelayCommand<object>(_ => Add());
            EditCommand = new RelayCommand<object>(_ => Edit(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand<object>(_ => Delete(), _ => SelectedItem != null);
            ReloadCommand = new RelayCommand<object>(_ => Reload());
            ToggleNumberCommand = new RelayCommand<object>(_ => RevealNumber = !RevealNumber, _ => SelectedItem != null);
            ToggleCvvCommand = new RelayCommand<object>(_ => RevealCvv = !RevealCvv, _ => SelectedItem != null);
            TogglePinCommand = new RelayCommand<object>(_ => RevealPin = !RevealPin, _ => SelectedItem != null);
            CopyNumberCommand = new RelayCommand<object>(_ => Copy(SelectedItem?.Number), _ => SelectedItem != null);
            CopyCvvCommand = new RelayCommand<object>(_ => Copy(SelectedItem?.Cvv), _ => !string.IsNullOrWhiteSpace(SelectedItem?.Cvv));
            CopyPinCommand = new RelayCommand<object>(_ => Copy(SelectedItem?.Pin), _ => !string.IsNullOrWhiteSpace(SelectedItem?.Pin));
            CopyHolderCommand = new RelayCommand<object>(_ => Copy(SelectedItem?.HolderName), _ => SelectedItem != null);
            Reload();
        }

        public void Reload()
        {
            var previousId = SelectedItem?.Id;
            Items.Clear();
            var query = _searchText.Trim();
            foreach (var card in EntryListSort.Apply(_vaultService.GetEntries().OfType<CardEntry>()))
            {
                if (!string.IsNullOrEmpty(query) &&
                    !Contains(card.HolderName, query) &&
                    !Contains(card.Number, query) &&
                    !Contains(card.Notes, query))
                    continue;

                Items.Add(card);
            }

            SelectedItem = Items.FirstOrDefault(c => c.Id == previousId) ?? Items.FirstOrDefault();
            OnPropertyChanged(nameof(IsEmpty));
            NotifyCardDisplay();
        }

        private void NotifyCardDisplay()
        {
            OnPropertyChanged(nameof(SelectedMaskedNumber));
            OnPropertyChanged(nameof(DisplayedNumber));
            OnPropertyChanged(nameof(DisplayedCvv));
            OnPropertyChanged(nameof(DisplayedPin));
            OnPropertyChanged(nameof(DisplayedExpiry));
            OnPropertyChanged(nameof(HasSelection));
        }

        private void Add()
        {
            if (EntryDialogHelper.AddOrEditCard() != null)
                Reload();
        }

        private void Edit()
        {
            if (SelectedItem == null) return;
            if (EntryDialogHelper.AddOrEditCard(SelectedItem) != null)
                Reload();
        }

        private void Delete()
        {
            if (SelectedItem == null) return;
            if (!EntryDialogHelper.ConfirmDelete(SelectedItem.HolderName))
                return;

            _vaultService.RemoveEntry(SelectedItem.Id);
            Reload();
        }

        private static void Copy(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            ClipboardHelper.CopyText(text, App.Settings.ClipboardClearSeconds);
            ToastService.Success(Loc.Get("Toast_Copied"));
        }

        private static string FormatCardNumber(string? number)
        {
            if (string.IsNullOrWhiteSpace(number)) return string.Empty;
            var digits = new string(number.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return number;
            return string.Join(" ", Enumerable.Range(0, (digits.Length + 3) / 4)
                .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4))));
        }

        private static bool Contains(string? text, string q)
            => !string.IsNullOrEmpty(text) && text.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
