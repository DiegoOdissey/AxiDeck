using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppExtensions;
using Windows.Media.Playback;

namespace AxiApp
{
    public class SerialManager
    {
        // Config
        private const int BaudRate = 9600;
        private const string HandshakeMsg = "CONNECT";
        private const string TimePrefix = "TIME:";
        private const int TimeInterval = 30;
        private const int ReconnectDelay = 5000; // ms

        // Events
        public event Action<bool>? ConnectionChanged;
        public event Action<string>? StatusChanged;
        public event Action<string>? MessageReceived;

        // State
        private SerialPort? _port;
        private CancellationTokenSource _cts = new();
        private readonly object _lock = new();
        public bool IsConnected {get; private set; }

        // Starts the background connection loop
        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ConnectionLoop(_cts.Token));
        }

        // Stops and closes the port
        public void Stop()
        {
            _cts.Cancel();
            ClosePort();
        }

        // Closes the port so the loop reconnects automatically
        public void Reset()
        {
            ClosePort();
        }

        // Connection loop
        private async Task ConnectionLoop(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                string? portName = FindArduinoPort();
                if(portName == null)
                {
                    RaiseStatus("[INFO] Searching for device...");
                    await Task.Delay(ReconnectDelay, ct).ContinueWith(_ => { });
                    continue;
                }

                RaiseStatus($"[INFO] Connecting on {portName}...");

                try
                {
                    var serial = new SerialPort(portName, BaudRate) { ReadTimeout = 2000 };
                    serial.Open();

                    await Task.Delay(2000, ct).ContinueWith(_ => { });

                    serial.WriteLine(HandshakeMsg);

                    lock (_lock)
                    {
                        _port = serial;
                        IsConnected = true;
                    }

                    ConnectionChanged?.Invoke(true);
                    RaiseStatus($"[INFO] Connected successfully on {portName}");

                    SendTime();
                    var lastTimeSend = DateTime.Now;

                    while(!ct.IsCancellationRequested)
                    {
                        if((DateTime.Now - lastTimeSend).TotalSeconds >= TimeInterval)
                        {
                            SendTime();
                            lastTimeSend = DateTime.Now;
                        }

                        lock (_lock)
                        {
                            if (_port is { IsOpen: true } && _port.BytesToRead > 0)
                            {
                                try
                                {
                                    string line = _port.ReadLine().Trim();
                                    if (!string.IsNullOrEmpty(line)) MessageReceived?.Invoke(line);
                                }
                                catch (TimeoutException) {}
                            }
                        }

                        bool alive;
                        lock (_lock) {alive = _port is { IsOpen: true }; }
                        if (!alive) break;

                        await Task.Delay(50, ct).ContinueWith(_ => {});
                    }
                }

                catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.IO.IOException)
                {
                    RaiseStatus($"[ERROR] {ex.Message}");
                }
                finally
                {
                    ClosePort();
                    ConnectionChanged?.Invoke(false);
                    RaiseStatus("[INFO] Disconnected - retrying");
                }

                await Task.Delay(ReconnectDelay, ct).ContinueWith(_ => { });
            }
        }

        // Helpers (tf is this)
        private static string? FindArduinoPort()
        {
            foreach (string name in SerialPort.GetPortNames())
            {
                return name;
            }
            return null;
        }

        private void SendTime()
        {
            string msg = $"{TimePrefix}{DateTime.Now:HH:mm}";
            Send(msg);
        }

        private void Send(string msg)
        {
            lock (_lock)
            {
                try
                {
                    if(_port is { IsOpen : true })
                    {
                        _port.WriteLine(msg);
                    }
                }
                catch(Exception ex) { RaiseStatus($"[ERROR] Error whilst sending message: {ex.Message}"); }
            }
        }

        private void ClosePort()
        {
            lock (_lock)
            {
                try { _port?.Close(); }
                catch(Exception ex) { RaiseStatus($"[ERROR] Error whilst closing port. {ex.Message}"); }
                _port = null;
                IsConnected = false;
            }
        }

        private void RaiseStatus(string text) => StatusChanged?.Invoke(text);
    }
}