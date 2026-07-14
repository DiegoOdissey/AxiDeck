# AxiDeck

A custom hardware control deck built around an Arduino Nano — physical knobs and buttons that talk to your PC over serial.

```
┌──────────────────────────────────┐
│  [ btn ]   [ btn ]   [ btn ]     │
│                                  │
│  [ btn ]   [ btn ]   [ btn ]     |
|                                  |
|      [ OLED ]      [ OLED ]      |
|                                  |
|  [ knob 1]          [ knob 2]    |
└──────────────────────────────────┘

        ↕  Data transmission with USB / Serial

   AxiApp (Windows tray/GUI)
```

---

## Repository Structure

```
AxiDeck/
├── firmware/          Arduino sketch (.ino) — runs on the Nano
├── AxiApp/            C# WinUI 3 desktop app (current)
└── AxiApp-python/     Python tray app (deprecated)
```

---

## Firmware

Written for the **Arduino Nano**. Handles:

- Serial handshake (`CONNECT`)
- Time sync (`TIME:HH:MM`)
- Knob and button event reporting back to the host
- OLED screen managing with TCA multiplexer

Flash with the Arduino IDE or CLI.

## External libraries required:
- Wire
- Adafuit_GFX
- Adafruit_SSD1306

---

## AxiApp — Windows Desktop (C# / WinUI 3)

The current host application. Runs on Windows 10/11.

**Requirements**
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/) runtime
- .NET 9

**Run from source**

```bash
cd AxiApp
dotnet run
```

**Features**
- Auto-detects the Arduino on any COM port
- Serial handshake + periodic time sync
- Reconnects automatically on disconnect
- Collapsible serial log
- Native Fluent / WinUI 3 UI

---

## AxiApp Python — Deprecated

The original prototype, built with `pyserial` and `pystray`. Kept here for reference.

```bash
cd AxiApp-python
pip install pyserial pystray pillow
python main.py
```

Superseded by the C# version. Not actively maintained.

---

## Serial Protocol (examples)

| Direction     | Message         | Description                        |
|---------------|-----------------|------------------------------------|
| PC → Arduino  | `CONNECT`       | Handshake on connection            |
| PC → Arduino  | `TIME:HH:MM`    | Current time sync (every 30s)      |
| Arduino → PC  | `KNOB1+`        | Knob 1 turned clockwise            |
| Arduino → PC  | `BTN1`          | Button 1 pressed                   |

---

## License

MIT
