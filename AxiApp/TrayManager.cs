using Microsoft.UI.Xaml;
using System;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace AxiApp
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _menu;
        private MainWindow? _window;
        private bool _suppressClose = true;

        public bool MinimizeToTray { get; set; } = true;

        public void Initialize(MainWindow window)
        {
            _window = window;

            // Build context menu
            _menu = new ContextMenuStrip();
            _menu.Items.Add("Apri AxiApp", null, (_, _) => ShowWindow());
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Esci", null, (_, _) => ExitApp());

            // Build tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "AxiApp",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _trayIcon.DoubleClick += (_, _) => ShowWindow();

            // Hook window close
            window.AppWindow.Closing += OnWindowClosing;

            Console.WriteLine("[Tray] Initialized.");
        }

        private void OnWindowClosing(
            Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            if (!MinimizeToTray || !_suppressClose) return;

            // Cancel the close and hide instead
            args.Cancel = true;
            HideWindow();
        }

        public void ShowWindow()
        {
            _window?.DispatcherQueue.TryEnqueue(() =>
            {
                _window.AppWindow.Show();
                _window.AppWindow.Show();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                appWindow.Show();
                Console.WriteLine("[Tray] Window shown.");
            });
        }

        public void HideWindow()
        {
            _window?.DispatcherQueue.TryEnqueue(() =>
            {
                _window.AppWindow.Hide();
                Console.WriteLine("[Tray] Window hidden to tray.");
            });
        }

        public void Notify(string title, string message)
        {
            _trayIcon?.ShowBalloonTip(3000, title, message,
                ToolTipIcon.None);
            Console.WriteLine($"[Tray] Notification: {title} — {message}");
        }

        public void UpdateTooltip(string text)
        {
            if (_trayIcon != null)
                _trayIcon.Text = text.Length > 63
                    ? text[..63]
                    : text;
        }

        private void ExitApp()
        {
            _suppressClose = false;
            _trayIcon?.Dispose();
            _window?.Close();
        }

        public void Dispose()
        {
            _trayIcon?.Dispose();
            _menu?.Dispose();
        }
    }
}