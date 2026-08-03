using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.WPF.Helpers;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Pages
{
    public partial class Accounts : Page
    {
        public Accounts()
        {
            InitializeComponent();
            DataContext = new AccountsViewModel(App.VaultService);
        }

        //private void ToggleMenu(object sender, RoutedEventArgs e)
        //{
        //    AddMenuPopup.IsOpen = !AddMenuPopup.IsOpen;
        //}

        

        private void OnAddBankCard_Click(object sender, RoutedEventArgs e)
            => EntryDialogHelper.AddOrEditCard();

        private void OnAddDocument_Click(object sender, RoutedEventArgs e)
            => EntryDialogHelper.AddDocument();

        private void OnAddAddress_Click(object sender, RoutedEventArgs e)
            => EntryDialogHelper.AddOrEditAddress();

        private void OnAddNote_Click(object sender, RoutedEventArgs e)
            => EntryDialogHelper.AddOrEditNote();

        //private void OnAddWebsite_Click(object sender, RoutedEventArgs e)
        //{
        //    AddMenuPopup.IsOpen = false;
        //    var win = new AddAccount("Website");
        //    win.Owner = Application.Current.MainWindow;
        //    var ok = win.ShowDialog();
        //    if (ok == true && DataContext is AccountsViewModel vm) vm.AddOrRefreshAccount(win.ResultEntry);
        //}

        //private void OnAddApplication_Click(object sender, RoutedEventArgs e)
        //{
        //    AddMenuPopup.IsOpen = false;
        //    var win = new AddAccount("Application");
        //    win.Owner = Application.Current.MainWindow;
        //    var ok = win.ShowDialog();
        //    if (ok == true && DataContext is AccountsViewModel vm) vm.AddOrRefreshAccount(win.ResultEntry);
        //}

        //private void OnAddOther_Click(object sender, RoutedEventArgs e)
        //{
        //    AddMenuPopup.IsOpen = false;
        //    var win = new AddAccount("Other");
        //    win.Owner = Application.Current.MainWindow;
        //    var ok = win.ShowDialog();
        //    if (ok == true && DataContext is AccountsViewModel vm) vm.AddOrRefreshAccount(win.ResultEntry);
        //}

        private string GetSelectedCategory()
        {
            if (DataContext is not AccountsViewModel vm)
                return "Website";

            var selected = vm.SelectedCategory ?? "Website";

            if (MatchesCategory(selected, "All categories"))
                return "Website";

            if (MatchesCategory(selected, "Accounts"))
                return "Application";

            if (MatchesCategory(selected, "Website", "AccountTypeWebsite") ||
                MatchesCategory(selected, "Email", "AccountTypeEmail"))
                return "Website";

            return selected;
        }

        private bool MatchesCategory(string selected, string literal, string? resourceKey = null)
        {
            if (string.Equals(selected, literal, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(resourceKey))
                return false;

            var localized = TryFindResource(resourceKey) as string
                            ?? Application.Current.TryFindResource(resourceKey) as string;

            return !string.IsNullOrWhiteSpace(localized) &&
                   string.Equals(selected, localized, StringComparison.OrdinalIgnoreCase);
        }

        private void OnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            var type = GetSelectedCategory();
            if (EntryDialogHelper.AddAccount(type) != null && DataContext is AccountsViewModel vm)
                vm.Reload();
        }

        private void OnMoreOptionsClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsViewModel vm && vm.SelectedAccount == null)
                return;

            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.DataContext = button.DataContext;
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void OnDetailsClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountsViewModel vm && vm.SelectedAccount != null)
            {
                var win = new AccountDetails
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = vm.SelectedAccount
                };
                win.ShowDialog();
            }
        }

        private void OnOpenPreviewClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is AccountEntry entry)
            {
                var win = new AccountPreview
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = entry
                };
                win.Show();
            }
        }

        private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AccountsViewModel vm && vm.SelectedAccount != null)
            {
                var win = new AccountPreview
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = vm.SelectedAccount
                };
                win.Show();
            }
        }

        private void OnGridCardMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                OpenPreviewFromSender(sender);
        }

        private void OnGridCardClicked(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks that originated from action buttons inside the card.
            if (e.OriginalSource is DependencyObject source &&
                FindAncestor<Button>(source) != null)
                return;

            if (DataContext is AccountsViewModel vm &&
                sender is Border border &&
                border.DataContext is AccountEntry entry)
            {
                vm.SelectedAccount = entry;
            }
        }

        private void OnGridCopyPasswordClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement fe && fe.DataContext is AccountEntry entry)
            {
                if (string.IsNullOrWhiteSpace(entry.Password))
                    return;

                ClipboardHelper.CopyText(entry.Password, App.Settings.ClipboardClearSeconds);
                ToastService.Success(Loc.Get("Toast_CopiedPassword"));
            }
        }

        private void OpenPreviewFromSender(object sender)
        {
            if (sender is FrameworkElement fe && fe.DataContext is AccountEntry entry)
                OpenPreview(entry);
        }

        private void OpenPreview(AccountEntry entry)
        {
            var win = new AccountPreview
            {
                Owner = Application.Current.MainWindow,
                DataContext = entry
            };
            win.Show();
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
