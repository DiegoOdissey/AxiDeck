using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace TrussiApp
{
    // ─────────────────────────────────────────────
    //  DATA MODELS
    // ─────────────────────────────────────────────
    public enum ActionType
    {
        None,
        Media,
        LaunchApp,
        Shortcut,
        Volume,
        OpenWebsite
    }

    public enum MediaCommand { PlayPause, Next, Previous }
    public enum VolumeTarget { Master, Discord, Gaming, Media, Browser }

    public class InputBinding
    {
        public ActionType Action { get; set; } = ActionType.None;
        public MediaCommand MediaCmd { get; set; } = MediaCommand.PlayPause;
        public string AppPath { get; set; } = "";
        public string ShortcutKeys { get; set; } = "";
        public VolumeTarget VolumeTarget { get; set; } = VolumeTarget.Master;
        public string WebsiteUrl { get; set; } = "";   // ← new
        public string Label { get; set; } = "";  // shown on deck screen
    }

    public class BindingProfile
    {
        public InputBinding[] Buttons { get; set; } = new InputBinding[6];
        public InputBinding[] Knobs { get; set; } = new InputBinding[2];

        public BindingProfile()
        {
            for (int i = 0; i < 6; i++) Buttons[i] = new InputBinding();
            for (int i = 0; i < 2; i++) Knobs[i] = new InputBinding();
        }
    }

    // ─────────────────────────────────────────────
    //  MANAGER
    // ─────────────────────────────────────────────
    public class BindingManager
    {
        private static string SavePath =>
            Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                "bindings.json");

        public BindingProfile Profile { get; private set; } = new();

        // ─────────────────────────────────────────────
        //  LOAD / SAVE
        // ─────────────────────────────────────────────
        public void Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Console.WriteLine("[Bindings] No save file found — using defaults.");
                    return;
                }

                string json = File.ReadAllText(SavePath);
                var loaded = JsonSerializer.Deserialize<BindingProfile>(json);
                if (loaded != null)
                {
                    Profile = loaded;
                    Console.WriteLine($"[Bindings] Loaded from {SavePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Load failed: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                Console.WriteLine($"[Bindings] Saving to: {SavePath}");
                string json = JsonSerializer.Serialize(Profile,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
                Console.WriteLine("[Bindings] Saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Save failed: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  EXECUTE — button press
        // ─────────────────────────────────────────────
        public void ExecuteButton(int index)
        {
            if (index < 0 || index >= 6) return;
            var binding = Profile.Buttons[index];
            Console.WriteLine($"[Bindings] Execute button {index + 1}: {binding.Action}");
            Execute(binding, 0);
        }

        // ─────────────────────────────────────────────
        //  EXECUTE — knob turn (direction: +1 or -1)
        // ─────────────────────────────────────────────
        public void ExecuteKnob(int index, int direction)
        {
            if (index < 0 || index >= 2) return;
            var binding = Profile.Knobs[index];
            Console.WriteLine(
                $"[Bindings] Execute knob {index + 1} " +
                $"{(direction > 0 ? "CW" : "CCW")}: {binding.Action}");
            Execute(binding, direction);
        }

        // ─────────────────────────────────────────────
        //  EXECUTE — core dispatcher
        // ─────────────────────────────────────────────
        private void Execute(InputBinding binding, int direction)
        {
            switch (binding.Action)
            {
                case ActionType.None:
                    break;
                case ActionType.Media:
                    ActionExecutor.ExecuteMedia(binding.MediaCmd);
                    break;
                case ActionType.LaunchApp:
                    if (!string.IsNullOrEmpty(binding.AppPath))
                        ActionExecutor.ExecuteLaunchApp(binding.AppPath);
                    break;
                case ActionType.Shortcut:
                    if (!string.IsNullOrEmpty(binding.ShortcutKeys))
                        ActionExecutor.ExecuteShortcut(binding.ShortcutKeys);
                    break;
                case ActionType.Volume:
                    ActionExecutor.ExecuteVolume(binding.VolumeTarget, direction);
                    break;
                case ActionType.OpenWebsite:
                    if (!string.IsNullOrWhiteSpace(binding.WebsiteUrl))
                        ActionExecutor.ExecuteOpenWebsite(binding.WebsiteUrl);
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  MEDIA CONTROL
        // ─────────────────────────────────────────────
        private static async void ExecuteMedia(MediaCommand cmd)
        {
            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager
                    .RequestAsync();
                var session = manager?.GetCurrentSession();
                if (session == null)
                {
                    Console.WriteLine("[Bindings] No active media session.");
                    return;
                }

                switch (cmd)
                {
                    case MediaCommand.PlayPause:
                        await session.TryTogglePlayPauseAsync();
                        Console.WriteLine("[Bindings] Media: PlayPause");
                        break;
                    case MediaCommand.Next:
                        await session.TrySkipNextAsync();
                        Console.WriteLine("[Bindings] Media: Next");
                        break;
                    case MediaCommand.Previous:
                        await session.TrySkipPreviousAsync();
                        Console.WriteLine("[Bindings] Media: Previous");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Media error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  APP LAUNCH
        // ─────────────────────────────────────────────
        private static void ExecuteLaunchApp(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                Console.WriteLine($"[Bindings] Launched: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Launch error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  KEYBOARD SHORTCUT
        //  Uses Win32 SendInput via P/Invoke
        // ─────────────────────────────────────────────
        private static void ExecuteShortcut(string keys)
        {
            try
            {
                // Parse "Ctrl+Shift+A" style strings
                var parts = keys.Split('+');
                var vkCodes = new List<byte>();

                foreach (var part in parts)
                {
                    byte vk = part.Trim().ToLower() switch
                    {
                        "ctrl" => 0x11,
                        "shift" => 0x10,
                        "alt" => 0x12,
                        "win" => 0x5B,
                        _ => GetVkFromName(part.Trim())
                    };
                    if (vk != 0) vkCodes.Add(vk);
                }

                // Press all keys down
                foreach (byte vk in vkCodes)
                    KeybdEvent(vk, 0, 0, 0);

                // Release all keys up (reverse order)
                for (int i = vkCodes.Count - 1; i >= 0; i--)
                    KeybdEvent(vkCodes[i], 0, 2, 0);  // KEYEVENTF_KEYUP = 2

                Console.WriteLine($"[Bindings] Shortcut sent: {keys}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Shortcut error: {ex.Message}");
            }
        }

        private static byte GetVkFromName(string name)
        {
            // Single letter or digit
            if (name.Length == 1)
            {
                char c = char.ToUpper(name[0]);
                if (c >= 'A' && c <= 'Z') return (byte)c;
                if (c >= '0' && c <= '9') return (byte)c;
            }

            // Function keys
            if (name.StartsWith("F") && int.TryParse(name[1..], out int fn))
                return (byte)(0x6F + fn);  // F1 = 0x70

            return name.ToLower() switch
            {
                "space" => 0x20,
                "enter" => 0x0D,
                "tab" => 0x09,
                "esc" => 0x1B,
                "delete" => 0x2E,
                "home" => 0x24,
                "end" => 0x23,
                "up" => 0x26,
                "down" => 0x28,
                "left" => 0x25,
                "right" => 0x27,
                _ => 0
            };
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan,
                                               uint dwFlags, int dwExtraInfo);

        private static void KeybdEvent(byte vk, byte scan, uint flags, int extra)
            => keybd_event(vk, scan, flags, extra);

        // ─────────────────────────────────────────────
        //  VOLUME CONTROL
        //  direction: +1 = louder, -1 = quieter
        // ─────────────────────────────────────────────
        private static void ExecuteVolume(VolumeTarget target, int direction)
        {
            if (direction == 0) return;

            if (target == VolumeTarget.Master)
            {
                // Use Windows media keys for master volume
                byte vk = direction > 0 ? (byte)0xAF : (byte)0xAE; // VK_VOLUME_UP/DOWN
                KeybdEvent(vk, 0, 0, 0);
                KeybdEvent(vk, 0, 2, 0);
                Console.WriteLine($"[Bindings] Master volume {(direction > 0 ? "up" : "down")}");
            }
            else
            {
                // Per-app volume via NAudio (stub — requires NAudio NuGet package)
                // Will be wired up when NAudio is added
                Console.WriteLine(
                    $"[Bindings] App volume {(direction > 0 ? "up" : "down")} " +
                    $"for {target} — NAudio not yet wired.");
            }
        }

        // ─────────────────────────────────────────────
        //  OPEN WEBSITE
        // ─────────────────────────────────────────────
        private static void ExecuteOpenWebsite(string url)
        {
            try
            {
                // Ensure it has a scheme, otherwise Process.Start treats it as a file path
                string target = url.Contains("://") ? url : $"https://{url}";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                Console.WriteLine($"[Bindings] Opened website: {target}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bindings] Open website error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  LABEL HELPERS  (for sending to deck)
        // ─────────────────────────────────────────────
        public string[] GetButtonLabels()
        {
            var labels = new string[6];
            for (int i = 0; i < 6; i++)
                labels[i] = Profile.Buttons[i].Label;
            return labels;
        }
    }
}