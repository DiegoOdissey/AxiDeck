using System;
using System.IO;
using System.Text.Json;

namespace TrussiApp
{
    public class PorcelliBindingProfile
    {
        public InputBinding[] Buttons { get; set; } = new InputBinding[6];

        public PorcelliBindingProfile()
        {
            for (int i = 0; i < 6; i++) Buttons[i] = new InputBinding();
        }
    }

    public class PorcelliBindingManager
    {
        private static string SavePath =>
            Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                "porcelli_bindings.json");

        public PorcelliBindingProfile Profile { get; private set; } = new();

        public void Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Console.WriteLine("[PorcelliBindings] No save file found — using defaults.");
                    return;
                }
                string json = File.ReadAllText(SavePath);
                var loaded = JsonSerializer.Deserialize<PorcelliBindingProfile>(json);
                if (loaded != null)
                {
                    Profile = loaded;
                    Console.WriteLine($"[PorcelliBindings] Loaded from {SavePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PorcelliBindings] Load failed: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
                string json = JsonSerializer.Serialize(Profile,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
                Console.WriteLine($"[PorcelliBindings] Saved to {SavePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PorcelliBindings] Save failed: {ex.Message}");
            }
        }

        public void ExecuteButton(int index)
        {
            if (index < 0 || index >= 6) return;
            var binding = Profile.Buttons[index];
            Console.WriteLine($"[PorcelliBindings] Execute key {index + 1}: {binding.Action}");

            switch (binding.Action)
            {
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
                case ActionType.OpenWebsite:
                    if (!string.IsNullOrWhiteSpace(binding.WebsiteUrl))
                        ActionExecutor.ExecuteOpenWebsite(binding.WebsiteUrl);
                    break;
            }
        }

        public string[] GetKeyLabels()
        {
            var labels = new string[6];
            for (int i = 0; i < 6; i++)
                labels[i] = Profile.Buttons[i].Label;
            return labels;
        }
    }
}