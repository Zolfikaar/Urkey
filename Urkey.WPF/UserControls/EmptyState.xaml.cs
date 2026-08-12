using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Urkey.WPF.UserControls
{
    public partial class EmptyState : UserControl
    {
        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(nameof(IconData), typeof(Geometry), typeof(EmptyState),
                new PropertyMetadata(null, OnIconDataChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnTitleChanged));

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnHintChanged));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnActionTextChanged));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState),
                new PropertyMetadata(null, OnActionCommandChanged));

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public EmptyState()
        {
            InitializeComponent();
            Loaded += (_, _) => SyncVisuals();
        }

        private static void OnIconDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((EmptyState)d).SyncVisuals();

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((EmptyState)d).SyncVisuals();

        private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((EmptyState)d).SyncVisuals();

        private static void OnActionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((EmptyState)d).SyncVisuals();

        private static void OnActionCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((EmptyState)d).SyncVisuals();

        private void SyncVisuals()
        {
            if (IconPath != null)
                IconPath.Data = IconData;

            if (TitleText != null)
                TitleText.Text = Title ?? string.Empty;

            if (HintText != null)
            {
                HintText.Text = Hint ?? string.Empty;
                HintText.Visibility = string.IsNullOrWhiteSpace(Hint)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            if (ActionButton != null && ActionLabel != null)
            {
                var show = ActionCommand != null && !string.IsNullOrWhiteSpace(ActionText);
                ActionButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                ActionLabel.Text = ActionText ?? string.Empty;
            }
        }

        private void OnActionClick(object sender, RoutedEventArgs e)
        {
            if (ActionCommand?.CanExecute(null) == true)
                ActionCommand.Execute(null);
        }
    }
}
