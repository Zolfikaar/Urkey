using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Urkey.WPF.Helpers
{
    public sealed class GlobalHotkeyService : IDisposable
    {
        private const int HotkeyId = 0x5533;
        private const int WmHotkey = 0x0312;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;
        private const uint ModWin = 0x0008;

        private readonly Window _window;
        private readonly Action _callback;
        private HwndSource? _source;
        private bool _registered;

        public GlobalHotkeyService(Window window, Action callback)
        {
            _window = window;
            _callback = callback;
        }

        public void Update(bool enabled, string modifier, string keyName)
        {
            Unregister();
            if (!enabled)
                return;

            var helper = new WindowInteropHelper(_window);
            if (helper.Handle == IntPtr.Zero)
                helper.EnsureHandle();

            _source ??= HwndSource.FromHwnd(helper.Handle);
            if (_source == null)
                return;

            _source.AddHook(WndProc);

            if (!TryParse(modifier, keyName, out uint mods, out uint vk))
                return;

            _registered = RegisterHotKey(helper.Handle, HotkeyId, mods, vk);
            if (!_registered)
                DiagnosticLog.Warn("Fast search hotkey is already in use by another application.");
        }

        public void Dispose()
        {
            Unregister();
        }

        private void Unregister()
        {
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
            }

            if (!_registered)
                return;

            try
            {
                var helper = new WindowInteropHelper(_window);
                if (helper.Handle != IntPtr.Zero)
                    UnregisterHotKey(helper.Handle, HotkeyId);
            }
            catch
            {
                // ignore
            }

            _registered = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                _callback();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static bool TryParse(string modifier, string keyName, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;

            foreach (var part in (modifier ?? string.Empty).Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    mods |= ModControl;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    mods |= ModAlt;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    mods |= ModShift;
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase))
                    mods |= ModWin;
            }

            if (mods == 0)
                mods = ModControl | ModAlt;

            if (!Enum.TryParse(keyName, ignoreCase: true, out Key key) || key == Key.None)
                key = Key.A;

            vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            return vk != 0;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
