using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Urkey.WPF.Helpers
{
    /// <summary>
    /// Locks the vault after a period of user inactivity (mouse/keyboard).
    /// AutoLockMinutes = 0 means disabled.
    /// </summary>
    public static class IdleLockService
    {
        private static DispatcherTimer? _timer;
        private static DateTime _lastActivityUtc = DateTime.UtcNow;
        private static bool _running;

        public static void Start()
        {
            if (_running) return;
            _running = true;
            _lastActivityUtc = DateTime.UtcNow;

            InputManager.Current.PreProcessInput += OnPreProcessInput;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public static void Stop()
        {
            if (!_running) return;
            _running = false;

            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer = null;
            }
        }

        public static void NotifyActivity()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        private static void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseEventArgs or KeyboardEventArgs or StylusEventArgs)
                _lastActivityUtc = DateTime.UtcNow;
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            int minutes = App.Settings.AutoLockMinutes;
            if (minutes <= 0)
                return;

            if (!Urkey.Core.Managers.VaultManager.IsUnlocked)
                return;

            if (DateTime.UtcNow - _lastActivityUtc < TimeSpan.FromMinutes(minutes))
                return;

            Stop();
            Application.Current?.Dispatcher.InvokeAsync(App.LockAndShowUnlockWindow);
        }
    }
}
