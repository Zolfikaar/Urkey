using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class EntryListItem : ViewModelBase
    {
        public EntryListItem(VaultEntry entry)
        {
            Entry = entry;
            Refresh();
        }

        public VaultEntry Entry { get; }

        public Guid Id => Entry.Id;

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        private string _subtitle = string.Empty;
        public string Subtitle
        {
            get => _subtitle;
            private set => SetProperty(ref _subtitle, value);
        }

        private string _typeKey = string.Empty;
        public string TypeKey
        {
            get => _typeKey;
            private set => SetProperty(ref _typeKey, value);
        }

        private string _typeDisplay = string.Empty;
        public string TypeDisplay
        {
            get => _typeDisplay;
            private set => SetProperty(ref _typeDisplay, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            private set => SetProperty(ref _email, value);
        }

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            private set => SetProperty(ref _username, value);
        }

        private string _urlOrPath = string.Empty;
        public string UrlOrPath
        {
            get => _urlOrPath;
            private set => SetProperty(ref _urlOrPath, value);
        }

        private string _detail = string.Empty;
        public string Detail
        {
            get => _detail;
            private set => SetProperty(ref _detail, value);
        }

        public void Refresh()
        {
            Title = EntryMetadata.GetDisplayName(Entry);
            Subtitle = EntryMetadata.GetSecondaryText(Entry);
            TypeKey = EntryMetadata.GetTypeKey(Entry);
            TypeDisplay = Loc.Get(TypeKey switch
            {
                EntryMetadata.TypeAccount => "AccountType",
                EntryMetadata.TypeCard => "AccountTypeBankCard",
                EntryMetadata.TypeAddress => "AccountTypeAddress",
                EntryMetadata.TypeNote => "AccountTypeNote",
                EntryMetadata.TypeDocument => "AccountTypeDocument",
                _ => "AccountType"
            });

            if (Entry is AccountEntry account)
            {
                Email = account.Email ?? string.Empty;
                Username = account.Username ?? string.Empty;
                UrlOrPath = account.Url ?? account.ApplicationPath ?? string.Empty;
                Detail = string.Join(Environment.NewLine, new[]
                {
                    Title,
                    string.IsNullOrWhiteSpace(Username) ? null : $"{Loc.Get("Column_Username")}: {Username}",
                    string.IsNullOrWhiteSpace(Email) ? null : $"{Loc.Get("Column_Email")}: {Email}",
                    string.IsNullOrWhiteSpace(UrlOrPath) ? null : UrlOrPath,
                    string.IsNullOrWhiteSpace(account.Notes) ? null : account.Notes
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                Email = string.Empty;
                Username = string.Empty;
                UrlOrPath = string.Empty;
                Detail = string.Join(Environment.NewLine, new[] { Title, TypeDisplay, Subtitle }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }
    }
}
