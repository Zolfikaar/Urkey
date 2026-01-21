using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
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
        {
            var win = new AddBankCard();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void OnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddDocument { Owner = Application.Current.MainWindow };
            if (win.ShowDialog() == true && win.Document != null)
            {
                var vm = new DocumentsViewModel(App.VaultService);
                vm.SaveNewDocument(win.Document);
            }
        }

        private void OnAddAddress_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddAddress();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void OnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddNote();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

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
            var win = new AddAccount(type) { Owner = Application.Current.MainWindow };
            var ok = win.ShowDialog();
            if (ok == true && DataContext is AccountsViewModel vm) vm.AddOrRefreshAccount(win.ResultEntry);
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

        private void OnGridCardClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AccountsViewModel vm && sender is Border border && border.DataContext is AccountEntry entry)
            {
                vm.SelectedAccount = entry;
            }
        }
    }
}
