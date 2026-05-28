using AxiApp.Pages;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AxiApp.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private MainWindow? _main;

        public DashboardPage()
        {
            InitializeComponent();
        }

        // MainWindow passes itself as the navigation parameter
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MainWindow main)
            {
                _main = main;

                // Subscribe to connection changes to update the dot
                _main.Serial.ConnectionChanged += OnConnectionChanged;

                // Reflect current state immediately
                UpdateDot(_main.Serial.IsConnected);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_main != null)
                _main.Serial.ConnectionChanged -= OnConnectionChanged;
        }

        // ─────────────────────────────────────────────
        //  CONNECTION DOT
        // ─────────────────────────────────────────────
        private void OnConnectionChanged(bool connected)
        {
            DispatcherQueue.TryEnqueue(() => UpdateDot(connected));
        }

        // ─────────────────────────────────────────────
        //  CARD CLICK
        // ─────────────────────────────────────────────
        private void AxiDeckCard_Click(object sender, RoutedEventArgs e)
        {
            _main?.OpenDetailPanel(typeof(AxiDeckPage));
        }

        private void PorcelliDashCard_Click(object sender, RoutedEventArgs e)
        {
            _main?.OpenDetailPanel(typeof(PorcelliDashPage));
        }

        private void UpdateDot(bool connected)
        {
            var brush = connected
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 50, 50));

            AxiDeckDot.Fill = brush;
            PorcelliDashDot.Fill = brush;
        }
    }
}