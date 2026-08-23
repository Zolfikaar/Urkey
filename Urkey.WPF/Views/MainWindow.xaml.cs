using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;
using Urkey.WPF.UserControls;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Pages;
using static Urkey.WPF.ViewModels.MainViewModel;

namespace Urkey.WPF.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public enum StartupPage
        {
            Home,
            Settings
        }

        private bool _isSidebarExpanded;
        private readonly MainViewModel _mainViewModel;
        private GlobalHotkeyService? _fastHotkey;

        public event PropertyChangedEventHandler? PropertyChanged;
        public static event EventHandler<bool>? SidebarStateChanged;

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

        /// <summary>
        /// Explicitly set the sidebar expanded state. Performs animation then sets state/persistence and event.
        /// </summary>
        public void SetSidebarExpanded(bool expand)
        {
            if (IsSidebarExpanded == expand)
            {
                // Ensure persistence even if state matches
                App.Settings.SidebarExpanded = IsSidebarExpanded;
                SettingsHelper.SaveSettings(App.Settings);
                SidebarStateChanged?.Invoke(this, IsSidebarExpanded);
                return;
            }

            PerformAnimation();

            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            delay.Tick += (s, args) =>
            {
                delay.Stop();
                IsSidebarExpanded = expand;
                Sidebar.UpdateToggleIcon(IsSidebarExpanded);
                UpdateLogoTextVisibility(IsSidebarExpanded);
                App.Settings.SidebarExpanded = IsSidebarExpanded;
                SettingsHelper.SaveSettings(App.Settings);
                SidebarStateChanged?.Invoke(this, IsSidebarExpanded);
            };
            delay.Start();
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainWindow(StartupPage startupPage = StartupPage.Home)
        {
            InitializeComponent();

            var langCode = App.Settings?.Language ?? "en";
            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            MainContentArea.Navigated += MainContentArea_Navigated;
            Sidebar.OnNavigationRequested += Sidebar_OnNavigationRequested;
            Sidebar.OnToggleRequested += (_, _) => ToggleSidebar_Click(null!, null!);

            // 🎨 طبّق الثيم فقط (اللغة طُبقت قبل الإنشاء)
            ThemeManager.ApplyTheme(App.Settings!.Theme);

            // 🧠 تهيئة ViewModel
            _mainViewModel = new MainViewModel();
            DataContext = _mainViewModel;

            // 📐 تهيئة الشريط الجانبي
            _isSidebarExpanded = App.Settings.SidebarExpanded;
            InitializeSidebarVisualState();

            // 🧭 التنقل الأولي حسب السيناريو
            NavigateInitialPage(startupPage);

            Closed += (_, _) =>
            {
                IdleLockService.Stop();
                _fastHotkey?.Dispose();
            };
            IdleLockService.Start();

            InputBindings.Add(new KeyBinding(new RelayCommand<object>(_ => FocusGlobalSearch()), Key.F, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new RelayCommand<object>(_ => OnLockVaultClick(this, new RoutedEventArgs())), Key.L, ModifierKeys.Control));

            SourceInitialized += (_, _) => RefreshFastHotkey();
        }

        public Page? CurrentContent => MainContentArea.Content as Page;

        public void FocusGlobalSearch()
        {
            SearchBox?.Focus();
            SearchBox?.SelectAll();
        }

        public void RefreshFastHotkey()
        {
            _fastHotkey ??= new GlobalHotkeyService(this, OnFastHotkeyPressed);
            _fastHotkey.Update(
                App.Settings.FastHotkeyEnabled,
                App.Settings.FastHotkeyModifier,
                App.Settings.FastHotkeyKey);
        }

        private void OnFastHotkeyPressed()
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
            NavigateToPage(new AllEntries(), "AllEntries");
            Dispatcher.BeginInvoke(FocusGlobalSearch, DispatcherPriority.Input);
        }

        private void OnLockVaultClick(object sender, RoutedEventArgs e)
        {
            DiagnosticLog.Info("Vault locked from header");
            App.LockAndShowUnlockWindow();
        }

        private void NavigateInitialPage(StartupPage startupPage)
        {
            switch (startupPage)
            {
                case StartupPage.Settings:
                    NavigateToSettings();
                    break;

                default:
                    NavigateToHome();
                    break;
            }

            UpdateViewTogglerForContent();
        }

        private void NavigateToSettings()
        {
            MainContentArea.Content = new Settings
            {
                DataContext = _mainViewModel
            };

            _mainViewModel.CurrentPage = NavigationTarget.Settings; // MainViewModel.PageType.Settings;
            UpdateSidebarActiveButton(NavigationTarget.Settings);
        }

        private void NavigateToHome()
        {
            // Home sets its own HomeViewModel in the constructor — do not overwrite.
            MainContentArea.Content = new Home();

            _mainViewModel.CurrentPage = NavigationTarget.Home;
            UpdateSidebarActiveButton(NavigationTarget.Home);
        }

        /// <summary>
        /// Programmatic navigation used by the README screenshot harness.
        /// </summary>
        public void NavigateToPage(Page page, string? sidebarButtonName = null)
        {
            if (page is FrameworkElement view && view.DataContext == null)
                view.DataContext = _mainViewModel;

            MainContentArea.Content = page;
            UpdateViewTogglerForContent();
            Sidebar.RefreshCounts();

            if (!string.IsNullOrWhiteSpace(sidebarButtonName))
                Sidebar.ActivateNavButton(sidebarButtonName);
        }

        private void UpdateSidebarActiveButton(NavigationTarget page)
        {
            Sidebar.SetIsActive(Sidebar.Home, page == NavigationTarget.Home);
            Sidebar.SetIsActive(Sidebar.Settings, page == NavigationTarget.Settings);
        }



        private void Sidebar_OnNavigationRequested(object? sender, SidebarNavigationEventArgs e)
        {
            if (e.View is FrameworkElement view)
            {
                // Only set DataContext if it's not already set (some pages set their own ViewModel)
                if (view.DataContext == null)
                    view.DataContext = _mainViewModel;
            }

            MainContentArea.Content = e.View;
            UpdateViewTogglerForContent();
        }

        public void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            PerformAnimation();

            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            delay.Tick += (s, args) =>
            {
                delay.Stop();
                IsSidebarExpanded = !IsSidebarExpanded;
                Sidebar.UpdateToggleIcon(IsSidebarExpanded);
                UpdateLogoTextVisibility(IsSidebarExpanded);

                App.Settings.SidebarExpanded = IsSidebarExpanded;
                SettingsHelper.SaveSettings(App.Settings);

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
            if (LogoWrapper != null)
                LogoWrapper.BeginAnimation(WidthProperty, animation);
        }

        private void InitializeSidebarVisualState()
        {
            SidebarContainer.Width = _isSidebarExpanded ? 240 : 60;
            Logo.Width = SidebarContainer.Width;
            if (LogoWrapper != null)
                LogoWrapper.Width = SidebarContainer.Width;

            Sidebar.UpdateToggleIcon(_isSidebarExpanded);
            UpdateLogoTextVisibility(_isSidebarExpanded);
            SidebarStateChanged?.Invoke(this, _isSidebarExpanded);
        }

        private void UpdateLogoTextVisibility(bool isVisible)
        {
            if (LogoText != null)
                LogoText.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }


        private void UpdateViewTogglerForContent()
        {
            if (GlobalViewToggler == null) return;

            var content = MainContentArea.Content as FrameworkElement;
            var dc = content?.DataContext;

            // Only entry list/grid pages expose ISupportsViewMode (not Home / Generator / Password Check / Settings).
            bool supportsViewMode = dc is ISupportsViewMode;
            GlobalViewToggler.Visibility = supportsViewMode ? Visibility.Visible : Visibility.Collapsed;
            GlobalViewToggler.DataContext = supportsViewMode ? dc : null;
        }

        private void MainContentArea_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateViewTogglerForContent();
        }

        private void OnGlobalSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var text = (sender as TextBox)?.Text ?? string.Empty;

                var content = MainContentArea.Content as FrameworkElement;
                var dc = content?.DataContext;
                if (dc == null) return;

                var prop = dc.GetType().GetProperty("SearchText");
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(dc, text);
                }
            }
            catch { }
        }

    }
}
