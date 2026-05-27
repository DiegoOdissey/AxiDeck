using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace AxiApp
{
    public sealed partial class MainWindow : Window
    {
        // ─────────────────────────────────────────────
        //  DEBUG
        // ─────────────────────────────────────────────
        private const bool DebugSimulateConnected = true; // ← flip to true to preview connected UI

        // ─────────────────────────────────────────────
        //  SETUP
        // ─────────────────────────────────────────────
        private readonly SerialManager _serial = new();
        private bool _logVisible = false;

        public MainWindow()
        {
            InitializeComponent();

            // Window minimum size
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(420, 220));

            if (DebugSimulateConnected)
            {
                // Bypass serial entirely — just paint the UI as connected
                SimulateConnected();
                return;
            }

            _serial.ConnectionChanged += OnConnectionChanged;
            _serial.StatusChanged += OnStatusChanged;
            _serial.MessageReceived += OnMessageReceived;
            _serial.Start();
        }

        // ─────────────────────────────────────────────
        //  DEBUG HELPER
        // ─────────────────────────────────────────────
        private void SimulateConnected()
        {
            StatusDot.Fill = new SolidColorBrush(Colors.LimeGreen);
            StatusLabel.Text = "Connected on COM3 (simulated)";
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            ResetButton.IsEnabled = true;
            AppendLog("[debug] Simulated connection active.");
        }

        // ─────────────────────────────────────────────
        //  SERIAL EVENT HANDLERS
        // ─────────────────────────────────────────────
        private void OnConnectionChanged(bool connected)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusDot.Fill = connected
                    ? new SolidColorBrush(Colors.LimeGreen)
                    : new SolidColorBrush(Colors.Gray);

                ConnectButton.IsEnabled = !connected;
                DisconnectButton.IsEnabled = connected;
                ResetButton.IsEnabled = connected;
            });
        }

        private void OnStatusChanged(string text)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusLabel.Text = text;
                AppendLog($"[status] {text}");
            });
        }

        private void OnMessageReceived(string line)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendLog($"[arduino] {line}");
            });
        }

        // ─────────────────────────────────────────────
        //  BUTTON HANDLERS
        // ─────────────────────────────────────────────
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("[app] Reconnecting...");
            _serial.Start();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("[app] Reset requested.");
            _serial.Reset();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("[app] Disconnected.");
            _serial.Stop();

            DispatcherQueue.TryEnqueue(() =>
            {
                StatusDot.Fill = new SolidColorBrush(Colors.Gray);
                StatusLabel.Text = "Disconnected (manual)";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
            });
        }

        private void LogToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _logVisible = !_logVisible;
            LogPanel.Visibility = _logVisible ? Visibility.Visible : Visibility.Collapsed;
            LogToggleButton.Content = _logVisible ? "▼  Hide log" : "▶  Show log";

            // Grow/shrink window with the panel
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(420, _logVisible ? 420 : 220));
        }

        // ─────────────────────────────────────────────
        //  LOG HELPER
        // ─────────────────────────────────────────────
        private void AppendLog(string line)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogBox.Text += $"{timestamp}  {line}\n";
            LogScroller.ChangeView(null, LogScroller.ScrollableHeight, null);
        }
    }
}