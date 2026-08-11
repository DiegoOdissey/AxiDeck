using System;
using System.Collections.Generic;
using Windows.Media.Control;

namespace TrussiApp
{
    // Shared action execution — used by BindingManager (AxiDeck)
    // and PorcelliBindingManager (PorcelliBoard)
    internal static class ActionExecutor
    {
        // ─────────────────────────────────────────────
        //  MEDIA CONTROL
        // ─────────────────────────────────────────────
        public static async void ExecuteMedia(MediaCommand cmd)
        {
            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager
                    .RequestAsync();
                var session = manager?.GetCurrentSession();
                if (session == null)
                {
                    Console.WriteLine("[Action] No active media session.");
                    return;
                }

                switch (cmd)
                {
                    case MediaCommand.PlayPause:
                        await session.TryTogglePlayPauseAsync();
                        Console.WriteLine("[Action] Media: PlayPause");
                        break;
                    case MediaCommand.Next:
                        await session.TrySkipNextAsync();
                        Console.WriteLine("[Action] Media: Next");
                        break;
                    case MediaCommand.Previous:
                        await session.TrySkipPreviousAsync();
                        Console.WriteLine("[Action] Media: Previous");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Action] Media error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  APP LAUNCH
        // ─────────────────────────────────────────────
        public static void ExecuteLaunchApp(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                Console.WriteLine($"[Action] Launched: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Action] Launch error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  OPEN WEBSITE
        // ─────────────────────────────────────────────
        public static void ExecuteOpenWebsite(string url)
        {
            try
            {
                string target = url.Contains("://") ? url : $"https://{url}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                Console.WriteLine($"[Action] Opened website: {target}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Action] Open website error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  KEYBOARD SHORTCUT
        // ─────────────────────────────────────────────
        public static void ExecuteShortcut(string keys)
        {
            try
            {
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

                foreach (byte vk in vkCodes)
                    KeybdEvent(vk, 0, 0, 0);

                for (int i = vkCodes.Count - 1; i >= 0; i--)
                    KeybdEvent(vkCodes[i], 0, 2, 0);

                Console.WriteLine($"[Action] Shortcut sent: {keys}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Action] Shortcut error: {ex.Message}");
            }
        }

        private static byte GetVkFromName(string name)
        {
            if (name.Length == 1)
            {
                char c = char.ToUpper(name[0]);
                if (c >= 'A' && c <= 'Z') return (byte)c;
                if (c >= '0' && c <= '9') return (byte)c;
            }

            if (name.StartsWith("F") && int.TryParse(name[1..], out int fn))
                return (byte)(0x6F + fn);

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

        public static void KeybdEvent(byte vk, byte scan, uint flags, int extra)
            => keybd_event(vk, scan, flags, extra);

        // ─────────────────────────────────────────────
        //  VOLUME CONTROL  (knob-only, kept here for AxiDeck)
        // ─────────────────────────────────────────────
        private static readonly AudioSessionManager _audioSessions = new();

        public static void ExecuteVolume(bool isMaster, string processName, int direction)
        {
            if (direction == 0) return;

            if (isMaster)
            {
                byte vk = direction > 0 ? (byte)0xAF : (byte)0xAE;
                KeybdEvent(vk, 0, 0, 0);
                KeybdEvent(vk, 0, 2, 0);
                Console.WriteLine($"[Action] Master volume {(direction > 0 ? "up" : "down")}");
            }
            else if (!string.IsNullOrWhiteSpace(processName))
            {
                _audioSessions.AdjustVolume(processName, direction);
            }
        }
    }
}