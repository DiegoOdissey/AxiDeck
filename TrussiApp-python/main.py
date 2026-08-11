"""
AxiDeck Backend v0.1
Windows tray app — handles serial handshake, time sync, and future GUI.
"""

import threading
import time
import serial
import serial.tools.list_ports
from datetime import datetime
import pystray
from PIL import Image, ImageDraw
import sys
import os


# ─────────────────────────────────────────────
#  CONFIG
# ─────────────────────────────────────────────
BAUD_RATE       = 9600
HANDSHAKE_MSG   = "CONNECT\n"
TIME_PREFIX     = "TIME:"          # Arduino will look for "TIME:HH:MM"
TIME_INTERVAL   = 30               # seconds between time updates
RECONNECT_DELAY = 5                # seconds between reconnect attempts
AUTO_DETECT_VID = 0x0403           # FTDI USB-Serial (Arduino Nano default)


# ─────────────────────────────────────────────
#  STATE
# ─────────────────────────────────────────────
class AxiDeckState:
    def __init__(self):
        self.ser: serial.Serial | None = None
        self.connected   = False
        self.port        = None
        self.lock        = threading.Lock()
        self.stop_event  = threading.Event()
        self.tray_icon   = None          # set after tray creation

state = AxiDeckState()


# ─────────────────────────────────────────────
#  SERIAL UTILITIES
# ─────────────────────────────────────────────
def find_arduino_port() -> str | None:
    """
    Tries to find the Arduino Nano automatically.
    Falls back to scanning all COM ports for a likely match.
    """
    ports = serial.tools.list_ports.comports()
    for p in ports:
        # Arduino Nano (FTDI or CH340 chips)
        if p.vid in (0x0403, 0x1A86) or "Arduino" in (p.description or ""):
            return p.device
    # Last resort: return first available COM port
    if ports:
        return ports[0].device
    return None


def send(msg: str):
    """Thread-safe serial write."""
    with state.lock:
        if state.ser and state.ser.is_open:
            try:
                state.ser.write((msg + "\n").encode())
            except serial.SerialException:
                pass


def send_time():
    """Send current time to Arduino in TIME:HH:MM format."""
    now = datetime.now().strftime("%H:%M")
    send(f"{TIME_PREFIX}{now}")


# ─────────────────────────────────────────────
#  CONNECTION MANAGER  (runs in background thread)
# ─────────────────────────────────────────────
def connection_loop():
    """
    Continuously tries to connect, performs handshake,
    then keeps the time updated. Reconnects on failure.
    """
    while not state.stop_event.is_set():
        port = find_arduino_port()

        if not port:
            print("[AxiDeck] No Arduino found. Retrying...")
            update_tray_tooltip("AxiDeck — Searching...")
            state.stop_event.wait(RECONNECT_DELAY)
            continue

        print(f"[AxiDeck] Found device on {port}. Connecting...")
        update_tray_tooltip(f"AxiDeck — Connecting ({port})...")

        try:
            ser = serial.Serial(port, BAUD_RATE, timeout=2)
            time.sleep(2)  # Wait for Arduino reset after serial open

            # ── Handshake ──
            ser.write(HANDSHAKE_MSG.encode())
            ser.flush()
            print("[AxiDeck] Handshake sent.")

            with state.lock:
                state.ser       = ser
                state.connected = True
                state.port      = port

            update_tray_icon(connected=True)
            update_tray_tooltip(f"AxiDeck — Connected ({port})")
            print(f"[AxiDeck] Connected on {port}.")

            # ── Send initial time immediately ──
            send_time()
            last_time_send = time.time()

            # ── Main loop: keep time updated, watch for disconnect ──
            while not state.stop_event.is_set():
                # Periodic time update
                if time.time() - last_time_send >= TIME_INTERVAL:
                    send_time()
                    last_time_send = time.time()

                # Check if port is still alive
                with state.lock:
                    alive = ser.is_open
                if not alive:
                    break

                # Read any incoming messages (knob/button events — for future use)
                with state.lock:
                    if ser.in_waiting:
                        try:
                            line = ser.readline().decode(errors="ignore").strip()
                            if line:
                                handle_incoming(line)
                        except serial.SerialException:
                            break

                state.stop_event.wait(0.05)  # 50ms polling — light on CPU

        except serial.SerialException as e:
            print(f"[AxiDeck] Serial error: {e}")

        finally:
            with state.lock:
                if state.ser:
                    try:
                        state.ser.close()
                    except Exception:
                        pass
                state.ser       = None
                state.connected = False
                state.port      = None

            update_tray_icon(connected=False)
            update_tray_tooltip("AxiDeck — Disconnected")
            print("[AxiDeck] Disconnected. Retrying...")

        state.stop_event.wait(RECONNECT_DELAY)


def handle_incoming(line: str):
    """
    Handle messages from Arduino (knob turns, button presses).
    Extend this later to trigger PC actions.
    """
    print(f"[AxiDeck] ← {line}")
    # Future: dispatch KNOB1+, KNOB2-, BTN1, etc. to actions here


# ─────────────────────────────────────────────
#  TRAY ACTIONS
# ─────────────────────────────────────────────
def action_open(icon, item):
    """Open the CustomTkinter GUI (stub for now)."""
    print("[AxiDeck] Open GUI — not yet implemented.")
    # Future: import gui; gui.launch()


def action_reset(icon, item):
    """
    Close and reopen the serial connection.
    Arduino resets on serial open, re-triggering its setup().
    """
    print("[AxiDeck] Reset requested.")
    with state.lock:
        if state.ser and state.ser.is_open:
            try:
                state.ser.close()
            except Exception:
                pass
            state.ser       = None
            state.connected = False
    # The connection_loop will automatically reconnect.


def action_disconnect(icon, item):
    """Disconnect and stay disconnected until app restart."""
    print("[AxiDeck] Disconnect requested.")
    state.stop_event.set()
    with state.lock:
        if state.ser and state.ser.is_open:
            try:
                state.ser.close()
            except Exception:
                pass
        state.ser       = None
        state.connected = False
    update_tray_icon(connected=False)
    update_tray_tooltip("AxiDeck — Disconnected (manual)")


def action_quit(icon, item):
    """Gracefully shut everything down."""
    print("[AxiDeck] Quitting.")
    state.stop_event.set()
    with state.lock:
        if state.ser and state.ser.is_open:
            try:
                state.ser.close()
            except Exception:
                pass
    icon.stop()


# ─────────────────────────────────────────────
#  TRAY ICON RENDERING
# ─────────────────────────────────────────────
def make_icon_image(connected: bool) -> Image.Image:
    """
    Draw a simple 64×64 tray icon.
    Green dot = connected, grey dot = disconnected.
    """
    size  = 64
    img   = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw  = ImageDraw.Draw(img)

    # Background rounded square
    bg_color = (30, 30, 30, 220)
    draw.rounded_rectangle([2, 2, size-2, size-2], radius=12, fill=bg_color)

    # "A" letter for AxiDeck
    draw.text((18, 10), "A", fill=(255, 255, 255, 255))

    # Status dot
    dot_color = (60, 220, 100) if connected else (140, 140, 140)
    draw.ellipse([42, 42, 58, 58], fill=dot_color)

    return img


def build_tray_menu():
    return pystray.Menu(
        pystray.MenuItem("Open AxiDeck", action_open, default=True),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Reset",        action_reset),
        pystray.MenuItem("Disconnect",   action_disconnect),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Quit",         action_quit),
    )


def update_tray_icon(connected: bool):
    if state.tray_icon:
        state.tray_icon.icon = make_icon_image(connected)


def update_tray_tooltip(text: str):
    if state.tray_icon:
        state.tray_icon.title = text


# ─────────────────────────────────────────────
#  ENTRY POINT
# ─────────────────────────────────────────────
def main():
    # Start connection manager in background
    conn_thread = threading.Thread(target=connection_loop, daemon=True)
    conn_thread.start()

    # Build and run tray icon (blocks main thread — required by pystray on Windows)
    icon = pystray.Icon(
        name    = "AxiDeck",
        icon    = make_icon_image(connected=False),
        title   = "AxiDeck — Starting...",
        menu    = build_tray_menu(),
    )
    state.tray_icon = icon
    icon.run()


if __name__ == "__main__":
    main()