using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TrussiApp;
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        this.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender,
    Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Console.WriteLine($"[FATAL] Unhandled exception: {e.Exception}");
        e.Handled = true;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Restore developer mode if it was enabled
        try
        {
            string prefsPath = System.IO.Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                "prefs.json");

            if (System.IO.File.Exists(prefsPath))
            {
                string json = System.IO.File.ReadAllText(prefsPath);
                var prefs = System.Text.Json.JsonSerializer
                                   .Deserialize<Dictionary<string, string>>(json);
                if (prefs != null &&
                    prefs.TryGetValue("developerMode", out string? val) &&
                    val == "True")
                {
                    ConsoleManager.Show(); // Note: ensure ConsoleManager is defined in your project
                }

                if (prefs != null &&
                    prefs.TryGetValue("verboseTrackLogging", out string? trackVal) &&
                    trackVal == "True")
                {
                    LogSettings.VerboseTrackLogging = true;
                }
            }
        }
        catch { }

        // Fixed: changed m_window to _window to match the field declaration above
        _window = new MainWindow();
        _window.Activate();
    }
}