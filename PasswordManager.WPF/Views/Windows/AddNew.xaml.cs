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

namespace PasswordManager.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for AddNew.xaml
    /// </summary>
    public partial class AddNew : Window
    {
        public AddNew()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            //var addNew = new AddNew { Owner = this };
            //if (addNew.ShowDialog() == true)
            //{
            //    // Handle save
            //}
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Apply dark theme if needed (you can check your app's theme setting)
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Check if dark theme should be applied
            // You can implement your own theme detection logic here
            // For now, we'll use the existing resource system
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";

            // The DynamicResource bindings will automatically pick up the correct theme
            // based on your app's resource dictionary merging
        }

        // Exposed properties for easy access
        public string Title => TitleTextBox.Text;
        public string Username => UsernameTextBox.Text;
        public string Password => PasswordBox.Password;
        public string Url => UrlTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // Method to set initial values
        public void SetValues(string title = "", string username = "", string password = "", string url = "", string notes = "")
        {
            TitleTextBox.Text = title;
            UsernameTextBox.Text = username;
            PasswordBox.Password = password;
            UrlTextBox.Text = url;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
           
               
                Close();
         
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            // Basic validation (can be expanded later)
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // جمع القيم من الحقول
            string title = TitleTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            string url = UrlTextBox.Text;
            string notes = NotesTextBox.Text;



            
                // bring the data to save it
                
            // عرضها في نافذة MessageBox
            //string message =
            //    $"Title: {title}\n" +
            //    $"Username: {username}\n" +
            //    $"Password: {password}\n" +
            //    $"URL: {url}\n" +
            //    $"Notes: {notes}";

            //MessageBox.Show(message, "Entered Data", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            
        }
    }
}
