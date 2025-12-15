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
using Urkey.Core.Models;
using Urkey.Core.Repository;

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for TestVaultWindow.xaml
    /// </summary>
    /// 
    public partial class TestVaultWindow : Window
    {
        public Vault vault;
        //
        public TestVaultWindow(string currentPassword)
        {
            InitializeComponent();

            //Loaded += TestWindowLoaded;




            commingPassword.Text = currentPassword;

            var repo = new VaultRepository(            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\Urkey
            _vaultDirectory);
            vault = repo.LoadVault();

            //string[] vaultData =
            //{
            //    vault.Entries.Capacity
            //};

            //vaultInfo.Text = Convert.ToString(
            //    vault.Password
            //    );
        }

        //public void TestWindowLoaded(object sender, RoutedEventArgs e)
        //{
        //    Vault vault;

        //}
    }
}
