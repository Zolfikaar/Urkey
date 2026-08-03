using System.Windows;
using Urkey.Core.Models;

namespace Urkey.WPF.Views.Windows
{
    public partial class DocumentDetails : Window
    {
        public DocumentDetails(DocumentEntry document)
        {
            InitializeComponent();
            DataContext = document;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

