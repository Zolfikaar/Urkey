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
    /// Interaction logic for AddNote.xaml
    /// </summary>
    public partial class AddNote : Window
    {
        public AddNote()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";
        }

        // Exposed properties for easy access
        public string Title => TitleTextBox.Text;
        public string Content => ContentTextBox.Text;
        public string Category => CategoryTextBox.Text;
        public string Tags => TagsTextBox.Text;

        // Method to set initial values
        public void SetValues(string title = "", string content = "", string category = "", string tags = "")
        {
            TitleTextBox.Text = title;
            ContentTextBox.Text = content;
            CategoryTextBox.Text = category;
            TagsTextBox.Text = tags;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // جمع القيم من الحقول
            string title = TitleTextBox.Text;
            string content = ContentTextBox.Text;
            string category = CategoryTextBox.Text;
            string tags = TagsTextBox.Text;

            Close();
        }
    }
}
