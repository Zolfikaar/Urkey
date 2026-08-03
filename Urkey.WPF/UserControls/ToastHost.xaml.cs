using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.UserControls
{
    public partial class ToastHost : UserControl
    {
        public ToastHost()
        {
            InitializeComponent();
            Loaded += (_, _) => ToastService.Register(this);
            Unloaded += (_, _) => ToastService.Unregister(this);
        }

        public void Enqueue(string message, ToastType type, int durationMs)
        {
            var card = BuildToast(message, type);
            ToastItems.Items.Add(card);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
            card.BeginAnimation(OpacityProperty, fadeIn);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1200, durationMs)) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
                fadeOut.Completed += (_, _) => ToastItems.Items.Remove(card);
                card.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private static Border BuildToast(string message, ToastType type)
        {
            var (accent, icon) = type switch
            {
                ToastType.Success => (ResolveBrush("SuccessColor", Color.FromRgb(0x81, 0xC7, 0x84)), "✓"),
                ToastType.Warning => (ResolveBrush("AccentGold", Color.FromRgb(0xD4, 0xAF, 0x56)), "!"),
                ToastType.Error => (ResolveBrush("ErrorColor", Color.FromRgb(0xEF, 0x53, 0x50)), "✕"),
                _ => (ResolveBrush("PrimaryColor", Color.FromRgb(0x7C, 0x6A, 0xF0)), "i")
            };

            var accentBar = new Border
            {
                Width = 4,
                Background = accent,
                CornerRadius = new CornerRadius(8, 0, 0, 8)
            };

            var iconBadge = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = accent,
                Margin = new Thickness(0, 0, 10, 0),
                Child = new TextBlock
                {
                    Text = icon,
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var text = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ResolveBrush("TextColor", Colors.White)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 10, 14, 10)
            };
            content.Children.Add(iconBadge);
            content.Children.Add(text);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(accentBar, 0);
            Grid.SetColumn(content, 1);
            grid.Children.Add(accentBar);
            grid.Children.Add(content);

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                MinWidth = 240,
                MaxWidth = 380,
                Opacity = 0,
                Background = ResolveBrush("ElevatedBgColor", Color.FromRgb(0x2A, 0x2D, 0x3E)),
                BorderBrush = ResolveBrush("BorderColor", Color.FromRgb(0x3A, 0x3D, 0x52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 2,
                    Opacity = 0.35
                },
                Child = grid,
                IsHitTestVisible = false
            };
        }

        private static Brush ResolveBrush(string key, Color fallback)
        {
            try
            {
                if (Application.Current?.TryFindResource(key) is Brush brush)
                    return brush;
            }
            catch
            {
                // ignore
            }

            return new SolidColorBrush(fallback);
        }
    }
}
