using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using AxiApp.Pages;

namespace AxiApp
{
    public sealed partial class MainWindow : Window
    {
        // ─────────────────────────────────────────────
        //  DEBUG
        // ─────────────────────────────────────────────
        private const bool DebugSimulateConnected = true;

        // ─────────────────────────────────────────────
        //  STATE
        // ─────────────────────────────────────────────
        public readonly SerialManager Serial = new();
        private bool _detailOpen = false;

        // ─────────────────────────────────────────────
        //  SETUP
        // ─────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            // Window size — compact on start
            SetWindowSize(1600, 800);

            // Force dark theme on the root
            RootGrid.RequestedTheme = ElementTheme.Dark;

            // Navigate to dashboard
            MainFrame.Navigate(typeof(DashboardPage), this,
                new SuppressNavigationTransitionInfo());

            // Start serial
            if (!DebugSimulateConnected)
                Serial.Start();
        }

        // ─────────────────────────────────────────────
        //  PANEL OPEN / CLOSE
        // ─────────────────────────────────────────────
        public void OpenDetailPanel(Type pageType)
        {
            if (!_detailOpen)
            {
                _detailOpen = true;
                DetailCol.Width = new GridLength(480);
                DetailDivider.Visibility = Visibility.Visible;
            }

            DetailFrame.Navigate(pageType, this,
                new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                });
        }

        public void CloseDetailPanel()
        {
            _detailOpen = false;
            DetailCol.Width = new GridLength(0);
            DetailDivider.Visibility = Visibility.Collapsed;
            DetailFrame.Content = null;
        }

        // ─────────────────────────────────────────────
        //  SIDEBAR NAV
        // ─────────────────────────────────────────────
        private void NavDevices_Click(object sender, RoutedEventArgs e)
        {
            SetNavActive(NavDevices, NavSettings);
            CloseDetailPanel();
            MainFrame.Navigate(typeof(DashboardPage), this,
                new SuppressNavigationTransitionInfo());
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            SetNavActive(NavSettings, NavDevices);
            CloseDetailPanel();
            // Future: MainFrame.Navigate(typeof(SettingsPage), this, ...);
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            // Future: check GitHub releases API
        }

        private static void SetNavActive(
            Microsoft.UI.Xaml.Controls.Button active,
            Microsoft.UI.Xaml.Controls.Button inactive)
        {
            active.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                      Microsoft.UI.ColorHelper.FromArgb(255, 34, 34, 34));
            active.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                      Microsoft.UI.Colors.White);
            inactive.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                      Microsoft.UI.Colors.Transparent);
            inactive.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                      Microsoft.UI.ColorHelper.FromArgb(255, 136, 136, 136));
        }

        // ─────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────
        private void SetWindowSize(int width, int height)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        }
    }
}