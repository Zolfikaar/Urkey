using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Urkey.WPF.UserControls;

namespace Urkey.WPF.Helpers
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Non-blocking toast notifications. Prefer this over MessageBox for info/success/warning/error.
    /// Yes/No confirmations still use MessageBox via <see cref="Confirm"/>.
    /// </summary>
    public static class ToastService
    {
        private static readonly List<WeakReference<ToastHost>> Hosts = new();

        public static void Register(ToastHost host)
        {
            Cleanup();
            Hosts.Add(new WeakReference<ToastHost>(host));
        }

        public static void Unregister(ToastHost host)
        {
            Hosts.RemoveAll(r => !r.TryGetTarget(out var h) || ReferenceEquals(h, host));
        }

        public static void Info(string message) => Show(message, ToastType.Info);
        public static void Success(string message) => Show(message, ToastType.Success);
        public static void Warning(string message) => Show(message, ToastType.Warning);
        public static void Error(string message) => Show(message, ToastType.Error);

        public static void Show(string message, ToastType type = ToastType.Info, int durationMs = 3200)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var app = Application.Current;
            if (app == null)
                return;

            void ShowCore()
            {
                var host = ResolveHost();
                if (host != null)
                {
                    host.Enqueue(message, type, durationMs);
                    return;
                }

                // Fallback: attach a transient host to the active window content.
                var window = GetTargetWindow();
                if (window?.Content is not FrameworkElement content)
                    return;

                if (content is Panel panel)
                {
                    var transient = new ToastHost
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(16),
                        IsHitTestVisible = false
                    };
                    Panel.SetZIndex(transient, 9999);
                    panel.Children.Add(transient);
                    Register(transient);
                    transient.Enqueue(message, type, durationMs);
                }
                else if (content is Decorator decorator && decorator.Child is FrameworkElement)
                {
                    var grid = new Grid();
                    var child = decorator.Child;
                    decorator.Child = null;
                    grid.Children.Add(child);
                    var transient = new ToastHost
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(16),
                        IsHitTestVisible = false
                    };
                    Panel.SetZIndex(transient, 9999);
                    grid.Children.Add(transient);
                    decorator.Child = grid;
                    Register(transient);
                    transient.Enqueue(message, type, durationMs);
                }
            }

            if (app.Dispatcher.CheckAccess())
                ShowCore();
            else
                app.Dispatcher.Invoke(ShowCore);
        }

        /// <summary>
        /// Blocking Yes/No confirmation (still uses MessageBox — toasts cannot collect a choice).
        /// </summary>
        public static bool Confirm(string message, string? title = null)
        {
            var result = MessageBox.Show(
                message,
                title ?? Loc.Get("ConfirmDelete_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            return result == MessageBoxResult.Yes;
        }

        private static ToastHost? ResolveHost()
        {
            Cleanup();
            for (int i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i].TryGetTarget(out var host) && host.IsLoaded)
                    return host;
            }

            var window = GetTargetWindow();
            return window == null ? null : FindHost(window);
        }

        private static Window? GetTargetWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
                   ?? app.MainWindow
                   ?? app.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
        }

        private static ToastHost? FindHost(DependencyObject root)
        {
            if (root is ToastHost host)
                return host;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var found = FindHost(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void Cleanup()
        {
            Hosts.RemoveAll(r => !r.TryGetTarget(out _));
        }
    }
}
