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
                // Clipboard might be unavailable; fail silently.
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
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                await dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (Clipboard.ContainsText() &&
                            string.Equals(Clipboard.GetText(), text, StringComparison.Ordinal))
                        {
                            Clipboard.Clear();
                        }
                    }
                    catch
                    {
                        // Clipboard might be unavailable; ignore.
                    }
                });
            }
            catch
            {
                // App may be shutting down.
            }
        }

        /// <summary>
        /// Cancel pending clear timers (e.g. on lock/logout). Does not clear clipboard content.
        /// </summary>
        public static void CancelPendingClear()
        {
            _clearCts?.Cancel();
            _clearCts?.Dispose();
            _clearCts = null;
        }
    }
}
