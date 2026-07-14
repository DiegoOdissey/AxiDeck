using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AxiApp
{
    public static class ConsoleManager
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private static bool _isOpen = false;
        private static TextWriter? _nullWriter = null;
        private static StreamWriter? _consoleWriter = null;

        public static bool IsOpen => _isOpen;

        public static void Show()
        {
            if (_isOpen) return;
            AllocConsole();

            _consoleWriter = new StreamWriter(Console.OpenStandardOutput())
            { AutoFlush = true };
            Console.SetOut(_consoleWriter);
            Console.SetError(new StreamWriter(Console.OpenStandardError())
            { AutoFlush = true });

            Console.Title = "AxiApp — Developer Console";
            Console.WriteLine("[Dev] Developer console enabled.");
            Console.WriteLine("[Dev] All log output will appear here.");
            Console.WriteLine(new string('─', 60));
            _isOpen = true;
        }

        public static void Hide()
        {
            if (!_isOpen) return;

            Console.WriteLine("[Dev] Closing developer console.");

            // Redirect output to null BEFORE freeing the console
            // so any subsequent Console.WriteLine doesn't crash
            _nullWriter = new StreamWriter(Stream.Null) { AutoFlush = true };
            Console.SetOut(_nullWriter);
            Console.SetError(_nullWriter);

            // Now safe to free
            FreeConsole();
            _consoleWriter?.Dispose();
            _consoleWriter = null;
            _isOpen = false;
        }
    }
}