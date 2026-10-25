using AmethystWindows.DesktopWindowsManager;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AmethystWindows
{
    internal sealed class LayoutNotificationWindow : Window
    {
        private readonly DispatcherTimer closeTimer;

        public LayoutNotificationWindow(Layout layout)
        {
            Title = "AmethystWindows layout";
            Width = 360;
            Height = 86;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(238, 31, 34, 40)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 12, 20, 12),
                Child = new TextBlock
                {
                    Text = layout.DisplayName(),
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 25,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };

            closeTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(1400)
            };
            closeTimer.Tick += CloseTimer_Tick;
            Closed += LayoutNotificationWindow_Closed;
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            closeTimer.Stop();
            Close();
        }

        private void LayoutNotificationWindow_Closed(object? sender, EventArgs e)
        {
            closeTimer.Stop();
        }

        public new void Show()
        {
            base.Show();
            closeTimer.Start();
        }
    }
}
