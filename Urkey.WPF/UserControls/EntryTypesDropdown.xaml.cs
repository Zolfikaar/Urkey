using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.UserControls
{
    public partial class EntryTypesDropdown : UserControl
    {
        public event EventHandler? EntryAdded;

        public EntryTypesDropdown()
        {
            InitializeComponent();
            FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        private void ToggleMenu(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = !AddMenuPopup.IsOpen;
        }

        private void OnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = false;
            if (EntryDialogHelper.AddAccount() != null)
                EntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddBankCard_Click(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = false;
            if (EntryDialogHelper.AddOrEditCard() != null)
                EntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = false;
            if (EntryDialogHelper.AddDocument() != null)
                EntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddAddress_Click(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = false;
            if (EntryDialogHelper.AddOrEditAddress() != null)
                EntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddNote_Click(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = false;
            if (EntryDialogHelper.AddOrEditNote() != null)
                EntryAdded?.Invoke(this, EventArgs.Empty);
        }
    }
}
