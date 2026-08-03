using System.Windows;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class AddNote : Window
    {
        public NoteEntry? Result { get; private set; }

        public AddNote()
        {
            InitializeComponent();
        }

        public new string Title => TitleTextBox.Text;
        public new string Content => ContentTextBox.Text;
        public string Category => CategoryTextBox.Text;
        public string Tags => TagsTextBox.Text;

        public void SetValues(string title = "", string content = "", string category = "", string tags = "")
        {
            TitleTextBox.Text = title;
            ContentTextBox.Text = content;
            CategoryTextBox.Text = category;
            TagsTextBox.Text = tags;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var entry = new NoteEntry
            {
                Title = TitleTextBox.Text?.Trim() ?? string.Empty,
                Content = ContentTextBox.Text?.Trim() ?? string.Empty,
                Category = CategoryTextBox.Text?.Trim() ?? string.Empty,
                Tags = TagsTextBox.Text?.Trim() ?? string.Empty
            };

            var validation = EntryValidator.ValidateNote(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return;
            }

            Result = entry;
            DialogResult = true;
            Close();
        }
    }
}
