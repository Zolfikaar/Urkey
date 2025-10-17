using System.Configuration;
using System.Data;
using System.Windows;
using PasswordManager.Core.Services;

using PasswordManager.WPF.Views;

namespace PasswordManager.WPF
{
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
        }

    }



}
