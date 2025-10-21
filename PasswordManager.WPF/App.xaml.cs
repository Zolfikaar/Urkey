using System.Windows;
using PasswordManager.WPF.Helpers;
//using PasswordManager.WPF.Views;

namespace PasswordManager.WPF
{
    public partial class App : Application
    {
        //protected bool isInitialLoad = true;
        //protected string master_password = "";
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            /*
             اذا كان البرنامج يشتغل للمرة الاولى , يجب عرض واجهة ادخال الرمز الرئيسي 
             */
            //if (isInitialLoad)
            //{
            //    // show set master password window
            //} else
            //{
            //    // show master password window to login
            //}

            // Apply default language
            Console.WriteLine("Applying default language...");
            LanguageManager.ApplyDefaultLanguage();
            Console.WriteLine($"Language applied: {LanguageManager.CurrentLanguage}");

            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
        }

    }



}
