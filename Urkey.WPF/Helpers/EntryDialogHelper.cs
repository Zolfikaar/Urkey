using System.Windows;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Helpers
{
    /// <summary>
    /// Shared add/edit dialogs that persist through VaultService.
    /// </summary>
    public static class EntryDialogHelper
    {
        public static AccountEntry? AddAccount(string? defaultType = null)
        {
            var win = string.IsNullOrWhiteSpace(defaultType)
                ? new AddAccount()
                : new AddAccount(defaultType);
            win.Owner = Application.Current.MainWindow;

            if (win.ShowDialog() != true)
                return null;

            var entry = win.ResultEntry;
            var validation = EntryValidator.ValidateAccount(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return null;
            }

            App.VaultService.AddEntry(entry);
            return entry;
        }

        public static CardEntry? AddOrEditCard(CardEntry? existing = null)
        {
            var win = new AddBankCard();
            win.Owner = Application.Current.MainWindow;
            if (existing != null)
            {
                win.SetValues(
                    existing.HolderName,
                    existing.Number,
                    existing.ExpiryDate == default ? "" : existing.ExpiryDate.ToString("MM/yy"),
                    existing.Cvv ?? "",
                    existing.Pin ?? "",
                    existing.Notes);
            }

            if (win.ShowDialog() != true || win.Result == null)
                return null;

            var entry = win.Result;
            if (existing != null)
                entry.Id = existing.Id;

            var validation = EntryValidator.ValidateCard(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return null;
            }

            if (existing == null)
                App.VaultService.AddEntry(entry);
            else
                App.VaultService.UpdateEntry(entry);

            return entry;
        }

        public static AddressEntry? AddOrEditAddress(AddressEntry? existing = null)
        {
            var win = new AddAddress();
            win.Owner = Application.Current.MainWindow;
            if (existing != null)
            {
                win.SetValues(
                    existing.Name,
                    existing.Street,
                    existing.City,
                    existing.Governorate,
                    existing.ZipCode ?? "",
                    existing.Country,
                    existing.Notes);
            }

            if (win.ShowDialog() != true || win.Result == null)
                return null;

            var entry = win.Result;
            if (existing != null)
                entry.Id = existing.Id;

            var validation = EntryValidator.ValidateAddress(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return null;
            }

            if (existing == null)
                App.VaultService.AddEntry(entry);
            else
                App.VaultService.UpdateEntry(entry);

            return entry;
        }

        public static NoteEntry? AddOrEditNote(NoteEntry? existing = null)
        {
            var win = new AddNote();
            win.Owner = Application.Current.MainWindow;
            if (existing != null)
                win.SetValues(existing.Title, existing.Content, existing.Category, existing.Tags);

            if (win.ShowDialog() != true || win.Result == null)
                return null;

            var entry = win.Result;
            if (existing != null)
                entry.Id = existing.Id;

            var validation = EntryValidator.ValidateNote(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return null;
            }

            if (existing == null)
                App.VaultService.AddEntry(entry);
            else
                App.VaultService.UpdateEntry(entry);

            return entry;
        }

        public static DocumentEntry? AddDocument()
        {
            var win = new AddDocument { Owner = Application.Current.MainWindow };
            if (win.ShowDialog() != true || win.Document == null)
                return null;

            var validation = EntryValidator.ValidateDocument(win.Document);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return null;
            }

            var vm = new ViewModels.DocumentsViewModel(App.VaultService);
            vm.Reload();
            vm.SaveNewDocument(win.Document);
            return win.Document;
        }

        public static bool ConfirmDelete(string entryName)
        {
            return ToastService.Confirm(
                Loc.Format("ConfirmDelete_Message", entryName),
                Loc.Get("ConfirmDelete_Title"));
        }

        public static bool ConfirmSignOut()
        {
            return ToastService.Confirm(
                Loc.Get("ConfirmSignOut_Message"),
                Loc.Get("ConfirmSignOut_Title"));
        }

        public static bool ConfirmResetApplicationData()
        {
            return ToastService.Confirm(
                Loc.Get("ResetAppData_ConfirmMessage"),
                Loc.Get("ResetAppData_ConfirmTitle"));
        }
    }
}
