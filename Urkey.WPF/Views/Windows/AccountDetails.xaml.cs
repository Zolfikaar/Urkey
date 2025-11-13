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

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for AccountDetails.xaml
    /// </summary>
    public partial class AccountDetails : Window
    {
        public AccountDetails()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
