using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for EntryTypesDropdown.xaml
    /// </summary>
    public partial class EntryTypesDropdown : UserControl
    {
        public EntryTypesDropdown()
        {
            InitializeComponent();
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        private void ToggleMenu(object sender, RoutedEventArgs e)
        {
            AddMenuPopup.IsOpen = !AddMenuPopup.IsOpen;
        }

        private void OnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddAccount();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

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
                // Create a DocumentsViewModel instance to save the document
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

    }
}
