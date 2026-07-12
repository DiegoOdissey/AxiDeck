using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using AxiApp;

namespace AxiApp.Pages
{
    // Which input is currently selected
    public enum InputType { Button, Knob }

    public class InputSelection
    {
        public InputType Type { get; init; }
        public int Index { get; init; }  // 0-based
        public string Label => Type == InputType.Button
                                  ? $"Tasto {Index + 1}"
                                  : $"Knob {Index + 1}";
        public bool IsKnob => Type == InputType.Knob;
    }

    public sealed partial class AxiDeckPage : Page
    {
        private MainWindow? _main;
        private InputSelection? _selected;
        private bool _listeningForShortcut = false;

        // All buttons and knobs in the layout, keyed by Tag string
        private Dictionary<string, Button> _inputButtons = new();

        public AxiDeckPage()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────
        //  NAVIGATION
        // ─────────────────────────────────────────────
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MainWindow main)
            {
                _main = main;
                _main.Serial.ConnectionChanged += OnConnectionChanged;
                UpdateBadge(_main.Serial.IsConnected);
            }

            // Build tag → button map for highlight management
            _inputButtons = new Dictionary<string, Button>
            {
                ["BTN:0"] = Btn1,
                ["BTN:1"] = Btn2,
                ["BTN:2"] = Btn3,
                ["BTN:3"] = Btn4,
                ["BTN:4"] = Btn5,
                ["BTN:5"] = Btn6,
                ["KNOB:0"] = Knob1Btn,
                ["KNOB:1"] = Knob2Btn
            };
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_main != null)
                _main.Serial.ConnectionChanged -= OnConnectionChanged;
        }

        // ─────────────────────────────────────────────
        //  CONNECTION BADGE
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
                StatusBadgeText.Text = "Connesso";
                StatusBadgeText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(
                                                 ColorHelper.FromArgb(255, 40, 20, 20));
                StatusBadgeText.Text = "Disconnesso";
                StatusBadgeText.Foreground = new SolidColorBrush(
                                                 ColorHelper.FromArgb(255, 200, 50, 50));
            }
        }

        // ─────────────────────────────────────────────
        //  INPUT SELECTION
        // ─────────────────────────────────────────────
        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;

            // Deselect previous
            ClearAllHighlights();

            // Parse tag — "BTN:0" or "KNOB:0"
            var parts = tag.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int index)) return;

            bool isKnob = parts[0] == "KNOB";
            _selected = new InputSelection
            {
                Type = isKnob ? InputType.Knob : InputType.Button,
                Index = index
            };

            // Highlight selected button
            btn.Background = new SolidColorBrush(
                                   ColorHelper.FromArgb(255, 40, 80, 140));
            btn.BorderBrush = new SolidColorBrush(
                                   ColorHelper.FromArgb(255, 80, 140, 220));

            // Update mapping panel
            ShowMappingPanel(_selected);

            Console.WriteLine($"[UI] Selected: {_selected.Label}");
        }

        private void ClearAllHighlights()
        {
            foreach (var btn in _inputButtons.Values)
            {
                btn.Background = new SolidColorBrush(
                                      ColorHelper.FromArgb(255, 37, 37, 37));
                btn.BorderBrush = new SolidColorBrush(
                                      ColorHelper.FromArgb(255, 58, 58, 58));
            }
        }

        // ─────────────────────────────────────────────
        //  MAPPING PANEL
        // ─────────────────────────────────────────────
        private void ShowMappingPanel(InputSelection sel)
        {
            MappingTitle.Text = $"Modificando {sel.Label}";
            MappingTitle.Foreground = new SolidColorBrush(Colors.White);
            EmptyState.Visibility = Visibility.Collapsed;
            MappingPanel.Visibility = Visibility.Visible;

            VolumeOption.Visibility = sel.IsKnob
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Load existing binding from profile
            InputBinding? binding = sel.Type == InputType.Button
                ? _main?.Bindings.Profile.Buttons[sel.Index]
                : _main?.Bindings.Profile.Knobs[sel.Index];

            if (binding == null)
            {
                ActionTypeCombo.SelectedIndex = 0;
                LabelBox.Text = "";
                HideAllSubPanels();
                return;
            }

            // Populate label
            LabelBox.Text = binding.Label;

            // Populate action type dropdown
            string actionTag = binding.Action switch
            {
                ActionType.Media => "media",
                ActionType.LaunchApp => "app",
                ActionType.Shortcut => "shortcut",
                ActionType.Volume => "volume",
                _ => "none"
            };

            foreach (ComboBoxItem item in ActionTypeCombo.Items)
            {
                if (item.Tag?.ToString() == actionTag)
                {
                    ActionTypeCombo.SelectedItem = item;
                    break;
                }
            }

            // Populate sub-panels
            HideAllSubPanels();
            switch (binding.Action)
            {
                case ActionType.Media:
                    MediaPanel.Visibility = Visibility.Visible;
                    string mediaTag = binding.MediaCmd switch
                    {
                        MediaCommand.Next => "next",
                        MediaCommand.Previous => "prev",
                        _ => "playpause"
                    };
                    foreach (ComboBoxItem item in MediaCombo.Items)
                        if (item.Tag?.ToString() == mediaTag)
                        { MediaCombo.SelectedItem = item; break; }
                    break;

                case ActionType.LaunchApp:
                    AppPanel.Visibility = Visibility.Visible;
                    AppPathBox.Text = binding.AppPath;
                    break;

                case ActionType.Shortcut:
                    ShortcutPanel.Visibility = Visibility.Visible;
                    ShortcutBox.Text = binding.ShortcutKeys;
                    break;

                case ActionType.Volume:
                    VolumePanel.Visibility = Visibility.Visible;
                    string volTag = binding.VolumeTarget == VolumeTarget.Master
                                    ? "master" : "app";
                    foreach (ComboBoxItem item in VolumeTypeCombo.Items)
                        if (item.Tag?.ToString() == volTag)
                        { VolumeTypeCombo.SelectedItem = item; break; }

                    if (binding.VolumeTarget != VolumeTarget.Master)
                    {
                        VolumeAppCombo.Visibility = Visibility.Visible;
                        string appTag = binding.VolumeTarget.ToString().ToLower();
                        foreach (ComboBoxItem item in VolumeAppCombo.Items)
                            if (item.Tag?.ToString() == appTag)
                            { VolumeAppCombo.SelectedItem = item; break; }
                    }
                    break;
            }
        }

        private void HideAllSubPanels()
        {
            MediaPanel.Visibility = Visibility.Collapsed;
            AppPanel.Visibility = Visibility.Collapsed;
            ShortcutPanel.Visibility = Visibility.Collapsed;
            VolumePanel.Visibility = Visibility.Collapsed;
        }

        private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HideAllSubPanels();

            if (ActionTypeCombo.SelectedItem is not ComboBoxItem item) return;
            string tag = item.Tag?.ToString() ?? "none";

            switch (tag)
            {
                case "media": MediaPanel.Visibility = Visibility.Visible; break;
                case "app": AppPanel.Visibility = Visibility.Visible; break;
                case "shortcut": ShortcutPanel.Visibility = Visibility.Visible; break;
                case "volume": VolumePanel.Visibility = Visibility.Visible; break;
            }

            Console.WriteLine($"[UI] Action type: {tag}");
        }

        private void VolumeTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VolumeTypeCombo.SelectedItem is not ComboBoxItem item) return;
            bool isApp = item.Tag?.ToString() == "app";
            VolumeAppCombo.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  APP PICKER
        // ─────────────────────────────────────────────
        private async void SelectApp_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            // WinUI 3 requires associating the picker with the window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_main!);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".lnk");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                AppPathBox.Text = file.Path;
                Console.WriteLine($"[UI] App selected: {file.Path}");
            }
        }

        // ─────────────────────────────────────────────
        //  SHORTCUT LISTENER
        // ─────────────────────────────────────────────
        private void ListenShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (_listeningForShortcut)
            {
                _listeningForShortcut = false;
                ListenButton.Content = "Ascolta";
                this.KeyDown -= OnShortcutKeyDown;
            }
            else
            {
                _listeningForShortcut = true;
                ListenButton.Content = "Stop";
                ShortcutBox.Text = "";
                this.KeyDown += OnShortcutKeyDown;
                Console.WriteLine("[UI] Listening for shortcut...");
            }
        }

        private void OnShortcutKeyDown(object sender,
            Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Build modifier string
            var mods = new System.Text.StringBuilder();
            var coreWin = Microsoft.UI.Input.InputKeyboardSource
                              .GetKeyStateForCurrentThread(
                                  Windows.System.VirtualKey.Control);
            var shiftW = Microsoft.UI.Input.InputKeyboardSource
                              .GetKeyStateForCurrentThread(
                                  Windows.System.VirtualKey.Shift);
            var altW = Microsoft.UI.Input.InputKeyboardSource
                              .GetKeyStateForCurrentThread(
                                  Windows.System.VirtualKey.Menu);

            bool ctrl = coreWin.HasFlag(
                             Windows.UI.Core.CoreVirtualKeyStates.Down);
            bool shift = shiftW.HasFlag(
                             Windows.UI.Core.CoreVirtualKeyStates.Down);
            bool alt = altW.HasFlag(
                             Windows.UI.Core.CoreVirtualKeyStates.Down);

            // Skip if only modifier key pressed alone
            var key = e.Key;
            if (key == Windows.System.VirtualKey.Control ||
                key == Windows.System.VirtualKey.Shift ||
                key == Windows.System.VirtualKey.Menu)
                return;

            if (ctrl) mods.Append("Ctrl+");
            if (shift) mods.Append("Shift+");
            if (alt) mods.Append("Alt+");
            mods.Append(key.ToString());

            ShortcutBox.Text = mods.ToString();
            _listeningForShortcut = false;
            ListenButton.Content = "Ascolta";
            this.KeyDown -= OnShortcutKeyDown;   // ← was Page.KeyDown

            Console.WriteLine($"[UI] Shortcut captured: {ShortcutBox.Text}");
            e.Handled = true;
        }

        // ─────────────────────────────────────────────
        //  BOTTOM BUTTONS
        // ─────────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _main?.CloseDetailPanel();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null || _main == null) return;

            // Build the binding from current UI state
            var binding = new InputBinding();
            binding.Label = LabelBox.Text.Trim();

            if (ActionTypeCombo.SelectedItem is ComboBoxItem actionItem)
            {
                binding.Action = actionItem.Tag?.ToString() switch
                {
                    "media" => ActionType.Media,
                    "app" => ActionType.LaunchApp,
                    "shortcut" => ActionType.Shortcut,
                    "volume" => ActionType.Volume,
                    _ => ActionType.None
                };
            }

            // Media command
            if (binding.Action == ActionType.Media &&
                MediaCombo.SelectedItem is ComboBoxItem mediaItem)
            {
                binding.MediaCmd = mediaItem.Tag?.ToString() switch
                {
                    "next" => MediaCommand.Next,
                    "prev" => MediaCommand.Previous,
                    _ => MediaCommand.PlayPause
                };
            }

            // App path
            if (binding.Action == ActionType.LaunchApp)
                binding.AppPath = AppPathBox.Text;

            // Shortcut
            if (binding.Action == ActionType.Shortcut)
                binding.ShortcutKeys = ShortcutBox.Text;

            // Volume
            if (binding.Action == ActionType.Volume &&
                VolumeTypeCombo.SelectedItem is ComboBoxItem volItem)
            {
                binding.VolumeTarget = volItem.Tag?.ToString() switch
                {
                    "app" when VolumeAppCombo.SelectedItem is ComboBoxItem appItem
                        => appItem.Tag?.ToString() switch
                        {
                            "discord" => VolumeTarget.Discord,
                            "gaming" => VolumeTarget.Gaming,
                            "media" => VolumeTarget.Media,
                            "browser" => VolumeTarget.Browser,
                            _ => VolumeTarget.Master
                        },
                    _ => VolumeTarget.Master
                };
            }

            // Save into profile
            if (_selected.Type == InputType.Button)
                _main.Bindings.Profile.Buttons[_selected.Index] = binding;
            else
                _main.Bindings.Profile.Knobs[_selected.Index] = binding;

            // Persist to disk
            _main.Bindings.Save();

            // Send updated labels to deck
            _main.Serial.SendAllLabels(_main.Bindings.GetButtonLabels());

            Console.WriteLine($"[UI] Binding saved for {_selected.Label}: {binding.Action}");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _main?.CloseDetailPanel();
        }

        private void UpdateFirmware_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[UI] Update firmware clicked.");
        }

        private void FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[UI] Factory reset clicked.");
        }
    }
}