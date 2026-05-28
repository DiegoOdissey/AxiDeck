using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AxiApp.Pages
{
    public sealed partial class AxiDeckPage : Page
    {
        private MainWindow? _main;

        public AxiDeckPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MainWindow main)
            {
                _main = main;
                _main.Serial.ConnectionChanged += OnConnectionChanged;
                UpdateBadge(_main.Serial.IsConnected);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_main != null)
                _main.Serial.ConnectionChanged -= OnConnectionChanged;
        }

        // ─────────────────────────────────────────────
        //  STATUS BADGE
        // ─────────────────────────────────────────────
        private void OnConnectionChanged(bool connected)
        {
            DispatcherQueue.TryEnqueue(() => UpdateBadge(connected));
        }

        private void UpdateBadge(bool connected)
        {
            if (connected)
            {
                StatusBadge.Background = new SolidColorBrush(
                                                 ColorHelper.FromArgb(255, 20, 60, 20));
                StatusBadgeText.Text = "Connected";
                StatusBadgeText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(
                                                 ColorHelper.FromArgb(255, 40, 20, 20));
                StatusBadgeText.Text = "Disconnected";
                StatusBadgeText.Foreground = new SolidColorBrush(
                                                 ColorHelper.FromArgb(255, 200, 50, 50));
            }
        }

        // ─────────────────────────────────────────────
        //  BUTTON HANDLERS
        // ─────────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _main?.CloseDetailPanel();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Future: write mappings to device
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _main?.CloseDetailPanel();
        }

        private void UpdateFirmware_Click(object sender, RoutedEventArgs e)
        {
            // Future: flash firmware over serial
        }

        private void FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            // Future: send reset command, confirm dialog first
        }
    }
}