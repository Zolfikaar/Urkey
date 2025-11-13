using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Urkey.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ViewToggler.xaml
    /// </summary>
    public partial class ViewToggler : UserControl
    {
        private const string IsListViewPropertyName = "IsListView";
        private const string IsGridViewPropertyName = "IsGridView";

        private INotifyPropertyChanged? _notifier;

        public ViewToggler()
        {
            InitializeComponent();
            DataContextChanged += ViewToggler_DataContextChanged;
            Loaded += (_, __) => UpdateButtonStates();
            Unloaded += ViewToggler_Unloaded;
        }

        private void ViewToggler_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_notifier != null)
                _notifier.PropertyChanged -= OnDataContextPropertyChanged;

            _notifier = e.NewValue as INotifyPropertyChanged;

            if (_notifier != null)
                _notifier.PropertyChanged += OnDataContextPropertyChanged;

            UpdateButtonStates();
        }

        private void ViewToggler_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_notifier != null)
                _notifier.PropertyChanged -= OnDataContextPropertyChanged;
            _notifier = null;
        }

        private void OnDataContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != IsListViewPropertyName && e.PropertyName != IsGridViewPropertyName)
                return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(UpdateButtonStates), DispatcherPriority.Background);
                return;
            }

            UpdateButtonStates();
        }

        private void OnListViewClicked(object sender, RoutedEventArgs e)
        {
            ToggleView(true);
        }

        private void OnGridViewClicked(object sender, RoutedEventArgs e)
        {
            ToggleView(false);
        }

        private void ToggleView(bool isList)
        {
            TrySetIsListView(isList);

            // Ensure buttons reflect state even if bindings don't propagate
            if (ListViewButton != null) ListViewButton.IsChecked = isList;
            if (GridViewButton != null) GridViewButton.IsChecked = !isList;
        }

        private void UpdateButtonStates()
        {
            bool isList = true;
            bool stateFound = TryReadIsListView(out bool boundState);
            if (stateFound) isList = boundState;

            if (ListViewButton != null) ListViewButton.IsChecked = isList;
            if (GridViewButton != null) GridViewButton.IsChecked = !isList;
        }

        private bool TryReadIsListView(out bool isListView)
        {
            isListView = true;
            var dc = DataContext;
            var prop = dc?.GetType().GetProperty(IsListViewPropertyName);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                isListView = (bool)(prop.GetValue(dc) ?? true);
                return true;
            }

            return false;
        }

        private void TrySetIsListView(bool value)
        {
            var dc = DataContext;
            var prop = dc?.GetType().GetProperty(IsListViewPropertyName);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
            {
                var current = (bool)(prop.GetValue(dc) ?? false);
                if (current != value)
                    prop.SetValue(dc, value);
            }
        }
    }
}
