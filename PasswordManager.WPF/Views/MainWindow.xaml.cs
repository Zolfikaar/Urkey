using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PasswordManager.WPF.Helpers;
using PasswordManager.WPF.UserControls;
using PasswordManager.WPF.ViewModels;
using PasswordManager.WPF.Views.Pages;

namespace PasswordManager.WPF.Views
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool _isSidebarExpanded = true; // Changed to true to make sidebar open by default
        private MainViewModel _mainViewModel;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                if (_isSidebarExpanded != value)
                {
                    _isSidebarExpanded = value;
                    OnPropertyChanged(nameof(IsSidebarExpanded));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Event to notify when sidebar state changes
        public static event EventHandler<bool> SidebarStateChanged;

        private string _burgerIcon = "\uE700";
        private string _closeIcon = "\uE711";

        public MainWindow()
        {
            InitializeComponent();

            // Set flow direction based on current language
            this.FlowDirection = LanguageManager.CurrentLanguage == LangCode.ar
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            // Initialize the main view model
            _mainViewModel = new MainViewModel();
            this.DataContext = _mainViewModel;

            // Load sidebar preference from settings
            LoadSidebarPreference();

            // Subscribe to the Sidebar's navigation event  
            Sidebar.OnNavigationRequested += Sidebar_OnNavigationRequested;

            // Set initial content  
            var homePage = new Home();
            homePage.DataContext = _mainViewModel;
            MainContentArea.Content = homePage; // Fix: Replace Navigate with Content property  

            // Set the sidebar initial state after it's loaded
            Sidebar.SetInitialState(_isSidebarExpanded);

            // Initialize logo wrapper width
            InitializeLogoWrapperWidth();
        }

        private void Sidebar_OnNavigationRequested(object sender, SidebarNavigationEventArgs e)
        {
            // Set the DataContext for the new page
            if (e.View is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContext = _mainViewModel;
            }
            MainContentArea.Content = e.View; // Fix: Replace Navigate with Content property  
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            PerformAnimation();

            // Delay the state change until after the animation completes
            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) }; // Match animation duration
            delay.Tick += (s, args) =>
            {
                delay.Stop();
                IsSidebarExpanded = !IsSidebarExpanded;
                BurgerIcon.Text = IsSidebarExpanded ? _closeIcon : _burgerIcon;

                // Update logo text visibility
                UpdateLogoTextVisibility(IsSidebarExpanded);

                // Notify that sidebar state has changed
                SidebarStateChanged?.Invoke(this, IsSidebarExpanded);
            };
            delay.Start();
        }

        private void PerformAnimation()
        {
            double from = _isSidebarExpanded ? 240 : 60;
            double to = _isSidebarExpanded ? 60 : 240;

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut }
            };

            SidebarContainer.BeginAnimation(WidthProperty, animation);
            Logo.BeginAnimation(WidthProperty, animation);

            // Also animate the logo wrapper width
            if (LogoWrapper != null)
            {
                LogoWrapper.BeginAnimation(WidthProperty, animation);
            }

            // Schedule text/icon visibility update AFTER the animation completes
            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            delay.Tick += (s, args) =>
            {
                delay.Stop();
                //Sidebar.SetSidebarState(_isSidebarExpanded); // Update internal layout AFTER animation
            };
            delay.Start();

            //_isSidebarExpanded = !_isSidebarExpanded;
        }

        private void LoadSidebarPreference()
        {
            try
            {
                var setting = ConfigurationManager.AppSettings["SidebarOpenByDefault"];
                if (!string.IsNullOrEmpty(setting))
                {
                    bool shouldOpenByDefault = bool.Parse(setting);
                    if (shouldOpenByDefault)
                    {
                        // Sidebar should be open by default
                        _isSidebarExpanded = true;
                        IsSidebarExpanded = true;
                        SidebarContainer.Width = 240;
                        Logo.Width = 240;
                        if (LogoWrapper != null)
                        {
                            LogoWrapper.Width = 240;
                        }
                        BurgerIcon.Text = _closeIcon;

                        // Update logo text visibility
                        UpdateLogoTextVisibility(true);

                        // Notify that sidebar state has changed
                        SidebarStateChanged?.Invoke(this, true);
                    }
                    else
                    {
                        // Sidebar should be closed by default
                        _isSidebarExpanded = false;
                        IsSidebarExpanded = false;
                        SidebarContainer.Width = 60;
                        Logo.Width = 60;
                        if (LogoWrapper != null)
                        {
                            LogoWrapper.Width = 60;
                        }
                        BurgerIcon.Text = _burgerIcon;

                        // Update logo text visibility
                        UpdateLogoTextVisibility(false);

                        // Notify that sidebar state has changed
                        SidebarStateChanged?.Invoke(this, false);
                    }
                }
                else
                {
                    // No setting found, use default (open)
                    _isSidebarExpanded = true;
                    IsSidebarExpanded = true;
                    SidebarContainer.Width = 240;
                    Logo.Width = 240;
                    if (LogoWrapper != null)
                    {
                        LogoWrapper.Width = 240;
                    }
                    BurgerIcon.Text = _closeIcon;

                    // Update logo text visibility
                    UpdateLogoTextVisibility(true);

                    // Notify that sidebar state has changed
                    SidebarStateChanged?.Invoke(this, true);
                }
            }
            catch
            {
                // Use default value if setting is invalid
                _isSidebarExpanded = true;
                IsSidebarExpanded = true;
                SidebarContainer.Width = 240;
                Logo.Width = 240;
                if (LogoWrapper != null)
                {
                    LogoWrapper.Width = 240;
                }
                BurgerIcon.Text = _closeIcon;

                // Update logo text visibility
                UpdateLogoTextVisibility(true);

                // Notify that sidebar state has changed
                SidebarStateChanged?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Refreshes the sidebar state based on current settings
        /// </summary>
        public void RefreshSidebarState()
        {
            LoadSidebarPreference();
            if (Sidebar != null)
            {
                Sidebar.SetInitialState(_isSidebarExpanded);
            }
        }

        /// <summary>
        /// Updates the logo text visibility based on sidebar state
        /// </summary>
        /// <param name="isVisible">Whether the logo text should be visible</param>
        private void UpdateLogoTextVisibility(bool isVisible)
        {
            if (LogoWrapper != null)
            {
                foreach (var item in LogoWrapper.Children)
                {
                    if (item is TextBlock textBlock && textBlock.Name == "LogoText")
                    {
                        textBlock.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the logo wrapper width based on current sidebar state
        /// </summary>
        private void InitializeLogoWrapperWidth()
        {
            if (LogoWrapper != null)
            {
                LogoWrapper.Width = _isSidebarExpanded ? 240 : 60;
            }
        }

    }
}