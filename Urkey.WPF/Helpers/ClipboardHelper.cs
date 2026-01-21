using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Urkey.WPF.Helpers
{
    public static class ClipboardHelper
    {
        private static CancellationTokenSource? _clearCts;

        public static void CopyText(string text, int clearAfterSeconds)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                Application.Current?.Dispatcher.Invoke(() => Clipboard.SetText(text));
                ScheduleClear(text, clearAfterSeconds);
            }
            catch
            {
                // Clipboard might be unavailable; fail silently for MVP simplicity.
            }
        }

        private static void ScheduleClear(string text, int clearAfterSeconds)
        {
            _clearCts?.Cancel();
            _clearCts?.Dispose();
            _clearCts = null;

            if (clearAfterSeconds <= 0)
                return;

            var cts = new CancellationTokenSource();
            _clearCts = cts;
            _ = ClearLaterAsync(text, TimeSpan.FromSeconds(clearAfterSeconds), cts.Token);
        }

        private static async Task ClearLaterAsync(string text, TimeSpan delay, CancellationToken token)
        {
            try
            {
                await Task.Delay(delay, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText() &&
                        string.Equals(Clipboard.GetText(), text, StringComparison.Ordinal))
                    {
                        Clipboard.Clear();
                    }
                });
            }
            catch
            {
                // Clipboard might be unavailable; ignore for MVP.
            }
        }
    }
}
