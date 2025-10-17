using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.WPF.Views;
using PasswordManager.WPF.Views.Pages;

using Path = System.Windows.Shapes.Path; // for nested menu icon

namespace PasswordManager.WPF.UserControls
{
    public partial class Sidebar : UserControl
    {
        private static string _bgColor = "#F6F6F9";
        private static string _bgWhite = "#FFF";
        private static string _textColor = "#363949";
        private static string _primaryColor = "#7380EC";


        public static string BgColor
        {
            get { return _bgColor; }
            set { _bgColor = value; }
        }
        public static string BgWhite
        {
            get { return _bgWhite; }
            set { _bgWhite = value; }
        }
        public static string TextColor
        {
            get { return _textColor; }
            set { _textColor = value; }
        }
        public static string PrimaryColor
        {
            get { return _primaryColor; }
            set { _primaryColor = value; }
        }

        public static bool _isSidebarOpen = true; // sidebar open by default
        private bool _isAllEntriesMenuOpen = true; // nested menu open by default


        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsActive",
                typeof(bool),
                typeof(Button),
                new PropertyMetadata(false));

        public static void SetIsActive(Button button, bool value)
        {
            button.SetValue(IsActiveProperty, value);
        }

        public static bool GetIsActive(Button button)
        {
            return (bool)button.GetValue(IsActiveProperty);
        }

        public static readonly DependencyProperty IsSidebarExpandedProperty =
            DependencyProperty.Register(
                "IsSidebarExpanded",
                typeof(bool),
                typeof(Sidebar),
                new PropertyMetadata(false, OnIsSidebarExpandedChanged));

        public bool IsSidebarExpanded
        {
            get => (bool)GetValue(IsSidebarExpandedProperty);
            set => SetValue(IsSidebarExpandedProperty, value);
        }


        private static void OnIsSidebarExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sidebar = d as Sidebar;
            sidebar?.UpdateSidebarTextVisibility();
        }

        public Sidebar()
        {
            InitializeComponent();
            SetButtonActive(Home);

            // Ensure nested menu starts expanded by default
            _isAllEntriesMenuOpen = true;
            UpdateNestedMenuVisualState();

            // Initialize sidebar state based on the dependency property
            _isSidebarOpen = IsSidebarExpanded;
        }

        public void ToggleSidebar(ContentControl MainContentArea, Border Topbar, Border Logo)
        {
            // Find the LogoWrapper inside the Logo border
            WrapPanel logoWrapPanel = Logo.Child as WrapPanel;

            if (_isSidebarOpen)
            {
                SidebarWrapper.Width = 60;
                if (logoWrapPanel != null)
                {
                    logoWrapPanel.Width = 60;
                    foreach (var item in logoWrapPanel.Children)
                    {
                        if (item is TextBlock textBlock && textBlock.Name == "LogoText")
                        {
                            textBlock.Visibility = Visibility.Hidden;
                        }
                        else if (item is TextBlock textBlockIcon && textBlockIcon.Name == "LogoIcon")
                        {
                            textBlockIcon.Visibility = Visibility.Visible;

                        }
                    }
                }
                Logo.Width = 60;
                _isSidebarOpen = false;
                IsSidebarExpanded = false;
            }
            else
            {
                SidebarWrapper.Width = 240;
                if (logoWrapPanel != null)
                {
                    logoWrapPanel.Width = 240;
                    foreach (var item in logoWrapPanel.Children)
                    {
                        if (item is TextBlock textBlock && textBlock.Name == "LogoText")
                        {
                            textBlock.Visibility = Visibility.Visible;
                        }
                        else if (item is TextBlock textBlockIcon && textBlockIcon.Name == "LogoIcon")
                        {
                            textBlockIcon.Visibility = Visibility.Visible;
                        }
                    }
                }
                Logo.Width = 240;
                _isSidebarOpen = true;
                IsSidebarExpanded = true;
            }
            UpdateSidebarTextVisibility();
        }

        private void HandleNestedMenuOnSidebarToggle()
        {
            // If sidebar is collapsed, close the nested menu
            if (!_isSidebarOpen && _isAllEntriesMenuOpen)
            {
                _isAllEntriesMenuOpen = false;
            }
        }

        private void SidebarBtnClicked(object sender, RoutedEventArgs e)
        {
            // First, completely reset ALL buttons to their default state
            ResetAllButtons();

            // Then apply active state to the clicked button
            if (sender is Button clickedButton)
            {
                SetButtonActive(clickedButton);

                // Navigation logic
                switch (clickedButton.Name)
                {
                    case "Home":
                        OnNavigationRequested?.Invoke(this, new SidebarNavigationEventArgs(new Home()));
                        break;
                    case "Passwords":
                        OnNavigationRequested?.Invoke(this, new SidebarNavigationEventArgs(new Passwords()));
                        break;

                    case "CreditCards":
                        OnNavigationRequested?.Invoke(this, new SidebarNavigationEventArgs(new CreditCards()));
                        break;





                    case "Settings":
                        OnNavigationRequested?.Invoke(this, new SidebarNavigationEventArgs(new Settings()));
                        break;
                }

                // Update text visibility after navigation
                UpdateSidebarTextVisibility();
            }
        }

        private void AllEntriesMenuClicked(object sender, RoutedEventArgs e)
        {
            _isAllEntriesMenuOpen = !_isAllEntriesMenuOpen;
            UpdateNestedMenuVisualState();
        }

        private void ResetAllButtons()
        {
            // Reset Other Buttons
            foreach (var child in TopBtns.Children)
            {
                if (child is Button btn)
                {
                    SetIsActive(btn, false);
                }
                else if (child is StackPanel stackPanel && stackPanel.Name == "PeopleManagementContainer")
                {
                    // Reset nested menu buttons
                    if (stackPanel.Children[1] is StackPanel nestedItems)
                    {
                        foreach (var nestedChild in nestedItems.Children)
                        {
                            if (nestedChild is Button nestedBtn)
                            {
                                SetIsActive(nestedBtn, false);
                            }
                        }
                    }
                }
            }
            SetIsActive(Settings, false);
        }

        private void SetButtonActive(Button button)
        {
            // First, completely reset ALL buttons to their default state
            ResetAllButtons();

            // Then apply active state to the clicked button
            SetIsActive(button, true);
        }

        public void UpdateSidebarTextVisibility()
        {
            //TopBtns.HorizontalAlignment = IsSidebarExpanded ? HorizontalAlignment.Left : HorizontalAlignment.Center;

            // Update all sidebar button text labels
            foreach (var child in TopBtns.Children)
            {
                if (child is Button btn)
                {
                    // Set button width
                    btn.Width = IsSidebarExpanded ? 240 : 60;

                    if (btn.Content is WrapPanel wrapPanel)
                    {
                        // Set WrapPanel width
                        wrapPanel.Width = IsSidebarExpanded ? 240 : 60;

                        // Handle the first border (icon) - always keep it visible
                        if (wrapPanel.Children.Count > 0 && wrapPanel.Children[0] is Border iconBorder)
                        {
                            // Keep icon visible but adjust width
                            iconBorder.Width = IsSidebarExpanded ? 60 : 60;
                            iconBorder.Visibility = Visibility.Visible;

                            // Ensure the Path (icon) inside the icon border is visible
                            if (iconBorder.Child is Path iconPath)
                            {
                                iconPath.Visibility = Visibility.Visible;
                            }
                        }

                        // Handle the second border (text) - hide text when collapsed
                        if (wrapPanel.Children.Count > 1 && wrapPanel.Children[1] is Border textBorder)
                        {
                            textBorder.Width = 180; // Keep width constant
                            textBorder.Visibility = IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
                            if (textBorder.Child is TextBlock textBlock)
                            {
                                textBlock.Visibility = IsSidebarExpanded ? Visibility.Visible : Visibility.Hidden;
                            }
                        }
                    }
                }
                else if (child is StackPanel stackPanel && stackPanel.Name == "AllEntriesContainer")
                {
                    // Handle All Entries Container
                    if (stackPanel.Children[0] is Button allEntriesMenuBtn)
                    {
                        allEntriesMenuBtn.Width = IsSidebarExpanded ? 240 : 60;

                        if (allEntriesMenuBtn.Template.FindName("PART_LeftIcon", allEntriesMenuBtn) is Path leftIcon)
                        {
                            leftIcon.Visibility = Visibility.Visible;
                        }
                    }

                    // Handle nested menu items
                    if (stackPanel.Children[1] is StackPanel nestedItems)
                    {
                        nestedItems.Width = IsSidebarExpanded ? 240 : 60;

                        foreach (var nestedChild in nestedItems.Children)
                        {
                            if (nestedChild is Button nestedBtn)
                            {
                                nestedBtn.Width = IsSidebarExpanded ? 240 : 60;
                            }
                        }
                    }
                }
            }

            // Update settings.
            Settings.Width = IsSidebarExpanded ? 240 : 60;
            if (Settings.Content is WrapPanel settingsWrapPanel)
            {
                settingsWrapPanel.Width = IsSidebarExpanded ? 240 : 60;

                // Handle the first border (icon) - always keep it visible
                if (settingsWrapPanel.Children.Count > 0 && settingsWrapPanel.Children[0] is Border iconBorder)
                {
                    // Keep icon visible but adjust width
                    iconBorder.Width = IsSidebarExpanded ? 60 : 60;
                    iconBorder.Visibility = Visibility.Visible;

                    // Ensure the Path (icon) inside the icon border is visible
                    if (iconBorder.Child is Path iconPath)
                    {
                        iconPath.Visibility = Visibility.Visible;
                    }
                }

                // Handle the second border (text) - hide text when collapsed
                if (settingsWrapPanel.Children.Count > 1 && settingsWrapPanel.Children[1] is Border textBorder)
                {
                    textBorder.Width = 180; // Keep width constant
                    textBorder.Visibility = IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
                    if (textBorder.Child is TextBlock textBlock)
                    {
                        textBlock.Visibility = IsSidebarExpanded ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            }
        }

        private void UpdateNestedMenuVisualState()
        {
            if (NestedMenuItems != null)
            {
                NestedMenuItems.Visibility = _isAllEntriesMenuOpen ? Visibility.Visible : Visibility.Collapsed;
            }

            if (AllEntriesMenu != null)
            {
                // Update arrow icon direction if present
                AllEntriesMenu.ApplyTemplate();
                var icon = AllEntriesMenu.Template.FindName("PART_LeftIcon", AllEntriesMenu) as Path;
                if (icon != null)
                {
                    // Use up arrow when expanded, down arrow when collapsed
                    icon.Data = (Geometry)FindResource(_isAllEntriesMenuOpen ? "bx_arrow_up" : "bx_arrow_down");
                }
            }
        }

        // Event for navigation
        public event EventHandler<SidebarNavigationEventArgs> OnNavigationRequested;

        /// <summary>
        /// Sets the initial state of the sidebar
        /// </summary>
        /// <param name="isExpanded">Whether the sidebar should be expanded</param>
        public void SetInitialState(bool isExpanded)
        {
            _isSidebarOpen = isExpanded;
            IsSidebarExpanded = isExpanded;
            UpdateSidebarTextVisibility();
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
