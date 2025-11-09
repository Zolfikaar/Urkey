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
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

