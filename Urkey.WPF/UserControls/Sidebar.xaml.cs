using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Urkey.Core.Models;
using Urkey.WPF.Views.Pages;

namespace Urkey.WPF.UserControls
{
    public partial class Sidebar : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsActive",
                typeof(bool),
                typeof(Sidebar),
                new PropertyMetadata(false));

        public static void SetIsActive(Button button, bool value)
            => button.SetValue(IsActiveProperty, value);

        public static bool GetIsActive(Button button)
            => (bool)button.GetValue(IsActiveProperty);

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.RegisterAttached(
                "Count",
                typeof(string),
                typeof(Sidebar),
                new PropertyMetadata(string.Empty));

        public static void SetCount(Button button, string? value)
            => button.SetValue(CountProperty, value ?? string.Empty);

        public static string GetCount(Button button)
            => (string)button.GetValue(CountProperty);

        public static readonly DependencyProperty IsSidebarExpandedProperty =
            DependencyProperty.Register(
                nameof(IsSidebarExpanded),
                typeof(bool),
                typeof(Sidebar),
                new PropertyMetadata(false, OnIsSidebarExpandedChanged));

        public bool IsSidebarExpanded
        {
            get => (bool)GetValue(IsSidebarExpandedProperty);
            set => SetValue(IsSidebarExpandedProperty, value);
        }

        public event EventHandler<SidebarNavigationEventArgs>? OnNavigationRequested;
        public event EventHandler? OnToggleRequested;

        public Sidebar()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                SetButtonActive(Home);
                UpdateSidebarTextVisibility();
                UpdateToggleIcon(IsSidebarExpanded);
                RefreshCounts();
            };
        }

        private static void OnIsSidebarExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Sidebar sidebar)
            {
                sidebar.UpdateSidebarTextVisibility();
                sidebar.UpdateToggleIcon((bool)e.NewValue);
            }
        }

        private void SidebarToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            OnToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateToggleIcon(bool expanded)
        {
            if (SidebarToggleBtn == null) return;

            var key = expanded ? "bx_sidebar" : "bx_menu_alt_left";
            if (TryFindResource(key) is Geometry geometry)
                SidebarToggleBtn.Tag = geometry;

            SidebarToggleBtn.ToolTip = TryFindResource(
                expanded ? "SidebarCollapseLabel" : "SidebarExpandLabel") as string
                ?? TryFindResource("SidebarToggleLabel") as string;
        }

        private void SidebarBtnClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton) return;

            SetButtonActive(clickedButton);

            Page? page = clickedButton.Name switch
            {
                "Home" => new Home(),
                "AllEntries" => new AllEntries(),
                "Accounts" => new Accounts(),
                "CreditCards" => new CreditCards(),
                "Addresses" => new Addresses(),
                "Notes" => new Notes(),
                "Docs" => new Documents(),
                "PasswordCheck" => new PasswordCheck(),
                "PasswordGenerator" => new PasswordGenerator(),
                "Settings" => new Settings(),
                _ => null
            };

            if (page != null)
                OnNavigationRequested?.Invoke(this, new SidebarNavigationEventArgs(page));

            UpdateSidebarTextVisibility();
            RefreshCounts();
        }

        public void RefreshCounts()
        {
            var entries = App.VaultService?.GetEntries() ?? Array.Empty<VaultEntry>();
            SetCount(AllEntries, FormatCount(entries.Count));
            SetCount(Accounts, FormatCount(entries.OfType<AccountEntry>().Count()));
            SetCount(CreditCards, FormatCount(entries.OfType<CardEntry>().Count()));
            SetCount(Addresses, FormatCount(entries.OfType<AddressEntry>().Count()));
            SetCount(Notes, FormatCount(entries.OfType<NoteEntry>().Count()));
            SetCount(Docs, FormatCount(entries.OfType<DocumentEntry>().Count()));
        }

        private static string FormatCount(int count) => count > 0 ? count.ToString() : string.Empty;

        private void ResetAllButtons()
        {
            foreach (var child in TopBtns.Children)
            {
                if (child is Button btn)
                    SetIsActive(btn, false);
            }

            SetIsActive(Settings, false);
            SetIsActive(SidebarToggleBtn, false);
        }

        private void SetButtonActive(Button button)
        {
            ResetAllButtons();
            SetIsActive(button, true);
        }

        public void UpdateSidebarTextVisibility()
        {
            void UpdateButton(Button btn)
            {
                if (btn == null) return;
                btn.ApplyTemplate();
                var visibility = IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
                if (btn.Template?.FindName("Label", btn) is TextBlock label)
                    label.Visibility = visibility;
                if (btn.Template?.FindName("CountLabel", btn) is TextBlock count)
                    count.Visibility = visibility;
            }

            foreach (var child in TopBtns.Children)
            {
                if (child is Button btn)
                    UpdateButton(btn);
            }

            UpdateButton(Settings);
            UpdateButton(SidebarToggleBtn);
        }

        public void SetInitialState(bool isExpanded)
        {
            IsSidebarExpanded = isExpanded;
            UpdateSidebarTextVisibility();
            UpdateToggleIcon(isExpanded);
        }

        public void ActivateNavButton(string buttonName)
        {
            if (FindName(buttonName) is Button button)
                SetButtonActive(button);
        }
    }

    public class SidebarNavigationEventArgs : EventArgs
    {
        public Page View { get; set; }

        public SidebarNavigationEventArgs(Page view)
        {
            View = view;
        }
    }
}
