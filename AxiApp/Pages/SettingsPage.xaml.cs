using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;

namespace AxiApp.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private MainWindow? _main;
        private bool _loading = true; // suppress toggle events during init

        public SettingsPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is not MainWindow main) return;
            _main = main;

            // Load current toggle states
            StartupToggle.IsOn = IsStartupEnabled();
            TrayToggle.IsOn = _main.Tray.MinimizeToTray;
            NotifyToggle.IsOn = _main.NotificationsEnabled;

            // Subscribe to serial events for device info
            _main.Serial.ConnectionChanged += OnConnectionChanged;
            _main.Serial.StatusChanged += OnStatusChanged;

            // Reflect current state
            UpdateDeviceInfo(_main.Serial.IsConnected,
                             _main.Serial.LastKnownPort,
                             _main.Serial.LastConnectedAt);

            _loading = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_main != null)
            {
                _main.Serial.ConnectionChanged -= OnConnectionChanged;
                _main.Serial.StatusChanged -= OnStatusChanged;
            }
        }

        // ─────────────────────────────────────────────
        //  TOGGLES
        // ─────────────────────────────────────────────
        private void StartupToggle_Toggled(object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SetStartupEnabled(StartupToggle.IsOn);
            Console.WriteLine($"[Settings] Startup with Windows: {StartupToggle.IsOn}");
        }

        private void TrayToggle_Toggled(object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading || _main == null) return;
            _main.Tray.MinimizeToTray = TrayToggle.IsOn;
            Console.WriteLine($"[Settings] Minimize to tray: {TrayToggle.IsOn}");
        }

        private void NotifyToggle_Toggled(object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading || _main == null) return;
            _main.NotificationsEnabled = NotifyToggle.IsOn;
            Console.WriteLine($"[Settings] Notifications: {NotifyToggle.IsOn}");
        }

        // ─────────────────────────────────────────────
        //  DEVICE INFO
        // ─────────────────────────────────────────────
        private void OnConnectionChanged(bool connected)
        {
            DispatcherQueue.TryEnqueue(() =>
                UpdateDeviceInfo(connected,
                                 _main?.Serial.LastKnownPort,
                                 _main?.Serial.LastConnectedAt));
        }

        private void OnStatusChanged(string _) { }

        private void UpdateDeviceInfo(bool connected, string? port, DateTime? lastAt)
        {
            PortLabel.Text = port ?? "—";
            StatusLabel.Text = connected ? "Connesso" : "Disconnesso";
            StatusLabel.Foreground = connected
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(ColorHelper.FromArgb(255, 200, 50, 50));
            LastConnectedLabel.Text = lastAt.HasValue
                ? lastAt.Value.ToString("dd/MM/yyyy HH:mm")
                : "—";
        }

        // ─────────────────────────────────────────────
        //  WINDOWS STARTUP (Registry)
        // ─────────────────────────────────────────────
        private const string StartupKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
            return key?.GetValue("AxiApp") != null;
        }

        private static void SetStartupEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            if (key == null) return;
            if (enable)
            {
                string exe = System.Diagnostics.Process.GetCurrentProcess()
                                   .MainModule!.FileName;
                key.SetValue("AxiApp", $"\"{exe}\"");
                Console.WriteLine($"[Settings] Startup registered: {exe}");
            }
            else
            {
                key.DeleteValue("AxiApp", false);
                Console.WriteLine("[Settings] Startup removed.");
            }
        }
    }
}