// AxiDeck Firmware v1.3

// Per il mio carissimo amico Axel, che ormai conosco da penso più di 10 anni...
// E' il primo regalo che faccio con le mie mani per qualcuno. E probabilmente
// lo farò di nuovo. Anche se non penso ci sia la possibilità che tu legga questo
// codice, buon compleanno <3

#include <Wire.h>
#include <EEPROM.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include "Fonts/FreeMonoBold9pt7b.h"
#include "Fonts/Org_01.h"
#include "Fonts/Picopixel.h"

#define OLED_ADDR     0x3C
#define EEPROM_MAGIC  0xAD
#define EEPROM_START  0
#define LABEL_COUNT   6
#define LABEL_MAXLEN  9
#define PING_INTERVAL 5000
#define PING_TIMEOUT  10000

Adafruit_SSD1306 display(128, 32, &Wire, -1);

// ─────────────────────────────────────────────
//  PINS
// ─────────────────────────────────────────────
const int clkPin1 = 2;
const int dtPin1  = 3;
const int clkPin2 = 4;
const int dtPin2  = 5;
const int btnPins[6] = {6, 7, 8, 9, 10, 11};

// ─────────────────────────────────────────────
//  STATE
// ─────────────────────────────────────────────
bool isConnected = false;
bool musicActive = false;

int  lastClkState1 = HIGH; // Initialized to HIGH for INPUT_PULLUP
int  lastClkState2 = HIGH; // Initialized to HIGH for INPUT_PULLUP

const unsigned long DEBOUNCE_MS = 50;
unsigned long btnLastChange[6]  = {0};
bool          btnStable[6]      = {false};
bool          btnRaw[6]         = {false};
bool          btnState[6]       = {false};

char currentTime[6]   = "00:00";
char trackTitle[22]   = "";
char trackArtist[22]  = "";
char trackDuration[6] = "0:00";
int  trackProgress    = 0;

char btnLabels[LABEL_COUNT][LABEL_MAXLEN];
unsigned long lastPingSent     = 0;
unsigned long lastPongReceived = 0;

bool dashDirty   = true;
bool labelsDirty = true;

// ─────────────────────────────────────────────
//  BITMAPS
// ─────────────────────────────────────────────
static const unsigned char trussi_T[] PROGMEM = {
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
  0xff,0xff,0xff,0xfe,0xff,0xff,0xff,0xfc,0xff,0xff,0xff,0xf8,0xff,0xff,0xff,0xf0,
  0xff,0xff,0xff,0xe0,0xff,0xff,0xff,0xc0,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,
  0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,
  0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,
  0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,0x00,0x1f,0xe0,0x00,
  0x00,0x1f,0xc0,0x00,0x00,0x1f,0x80,0x00,0x00,0x1f,0x00,0x00,0x00,0x1e,0x00,0x00,
  0x00,0x1c,0x00,0x00,0x00,0x18,0x00,0x00,0x00,0x10,0x00,0x00,0x00,0x00,0x00,0x00
};
static const unsigned char PROGMEM image_hour_glass_75_bits[] = {
  0xff,0xe0,0x40,0x40,0x40,0x40,0x51,0x40,0x5f,0x40,0x2e,0x80,0x15,0x00,
  0x0a,0x00,0x0a,0x00,0x11,0x00,0x24,0x80,0x44,0x40,0x4e,0x40,0x5f,0x40,
  0x7f,0xc0,0xff,0xe0
};
static const unsigned char PROGMEM image_hour_glass_75_bits_rotated[] = {
  0x80,0x01,0xf8,0x1f,0xc4,0x21,0xe2,0x59,0xf1,0xb1,0xfc,0x71,
  0xf1,0xb1,0xe2,0x59,0xc4,0x21,0xf8,0x1f,0x80,0x01
};

// ─────────────────────────────────────────────
//  SCROLL STATE
// ─────────────────────────────────────────────
struct ScrollState {
  int  x;
  int  targetX;
  bool scrolling;
  bool returning;
};
ScrollState titleScroll  = {0, 0, false, false};
ScrollState artistScroll = {0, 0, false, false};
unsigned long lastScrollTick   = 0;
unsigned long scrollPauseUntil = 0;
const int SCROLL_SPEED_MS      = 40;
const int SCROLL_PAUSE_MS      = 1500;
const int SCROLL_VIEWPORT = 94;

// ─────────────────────────────────────────────
//  BUTTON LABEL GRID LAYOUT
// ─────────────────────────────────────────────
const int btnX[3]  = {0, 43, 86};
const int btnY[2]  = {22, 31};
const int btnBoxW  = 40;
const int btnBoxH  = 9;

// ─────────────────────────────────────────────
//  TCA MULTIPLEXER
// ─────────────────────────────────────────────
void tcaSelect(uint8_t channel) {
  Wire.beginTransmission(0x70);
  Wire.write(1 << channel);
  Wire.endTransmission();
}

// ─────────────────────────────────────────────
//  EEPROM
// ─────────────────────────────────────────────
void loadLabelsFromEEPROM() {
  if (EEPROM.read(EEPROM_START) != EEPROM_MAGIC) {
    for (int i = 0; i < LABEL_COUNT; i++)
      btnLabels[i][0] = '\0';
    return;
  }
  int addr = EEPROM_START + 1;
  for (int i = 0; i < LABEL_COUNT; i++) {
    for (int c = 0; c < LABEL_MAXLEN; c++)
      btnLabels[i][c] = EEPROM.read(addr++);
    btnLabels[i][LABEL_MAXLEN - 1] = '\0';
  }
}

void saveLabelsToEEPROM() {
  EEPROM.write(EEPROM_START, EEPROM_MAGIC);
  int addr = EEPROM_START + 1;
  for (int i = 0; i < LABEL_COUNT; i++)
    for (int c = 0; c < LABEL_MAXLEN; c++)
      EEPROM.write(addr++, btnLabels[i][c]);
}

// ─────────────────────────────────────────────
//  BOOT SCREEN
// ─────────────────────────────────────────────
void showBootScreen() {
  Serial.println("Starting boot screen sequence");
  tcaSelect(0);
  display.clearDisplay();
  display.drawBitmap(48, 0, trussi_T, 32, 32, SSD1306_WHITE);
  display.display();

  tcaSelect(1);
  display.clearDisplay();
  display.setTextSize(2);
  display.setTextColor(SSD1306_WHITE);
  display.drawRect(5, 5, 118, 25, SSD1306_WHITE);
  display.setCursor(20, 10);
  display.println("AxiDeck");
  display.display();
  delay(3500);
  Serial.println("Ended boot screen sequence");
}

// ─────────────────────────────────────────────
//  WAITING SCREEN
// ─────────────────────────────────────────────
void showWaitingScreen() {
  tcaSelect(0);
  display.clearDisplay();
  display.display();

  tcaSelect(1);
  unsigned long lastToggle = 0;
  bool frame = false;
  while (!isConnected) {
    if (Serial.available() > 0) {
      String msg = Serial.readStringUntil('\n');
      msg.trim();
      if (msg == "CONNECT") {
        isConnected      = true;
        lastPongReceived = millis();
        Serial.println("ACK");
        break;
      }
    }

    if (millis() - lastToggle >= 500) {
      lastToggle = millis();
      frame = !frame;

      display.clearDisplay();
      display.setTextSize(1);
      display.setFont(NULL);
      display.setTextColor(SSD1306_WHITE);
      display.setCursor(2, 4);

      if (frame) {
        display.print("Attendo risposta PC..");
        display.drawBitmap(55, 17, image_hour_glass_75_bits_rotated, 16, 11, 1);
      } else {
        display.print("Attendo risposta PC.");
        display.drawBitmap(58, 14, image_hour_glass_75_bits, 11, 16, 1);
      }
      display.display();
    }
  }
  Serial.println("PC Connected!");
}

// ─────────────────────────────────────────────
//  SCROLL HELPERS
// ─────────────────────────────────────────────
int getOrg01TextWidth(const char* text) {
  int w = 0;
  while (*text++) w += 5;
  return w;
}

void resetScrollState() {
  int titleW  = getOrg01TextWidth(trackTitle);
  int artistW = getOrg01TextWidth(trackArtist);
  titleScroll.x         = 0;
  titleScroll.targetX   = max(0, titleW  - SCROLL_VIEWPORT);
  titleScroll.scrolling = titleW  > SCROLL_VIEWPORT;
  titleScroll.returning = false;

  artistScroll.x         = 0;
  artistScroll.targetX   = max(0, artistW - SCROLL_VIEWPORT);
  artistScroll.scrolling = artistW > SCROLL_VIEWPORT;
  artistScroll.returning = false;

  scrollPauseUntil = 0;
  lastScrollTick   = 0;
}

void tickScroll() {
  if (!musicActive) return;
  if (millis() < scrollPauseUntil) return;
  if (millis() - lastScrollTick < SCROLL_SPEED_MS) return;
  lastScrollTick = millis();

  bool changed = false;
  auto tickOne = [&](ScrollState& s) {
    if (!s.scrolling) return;
    if (!s.returning) {
      if (s.x < s.targetX) {
        s.x++;
        changed = true;
      } else {
        scrollPauseUntil = millis() + SCROLL_PAUSE_MS;
        s.returning = true;
      }
    } else {
      if (s.x > 0) {
        s.x--;
        changed = true;
      } else {
        scrollPauseUntil = millis() + SCROLL_PAUSE_MS;
        s.returning = false;
      }
    }
  };

  tickOne(titleScroll);
  tickOne(artistScroll);
  if (changed) dashDirty = true;
}

void drawScrollText(const char* text, int scrollX, int y, int maxW) {
  int charW   = 5;
  int startCh = scrollX / charW;
  int pixOff  = scrollX % charW;
  display.setCursor(-pixOff, y);

  int maxChars = (maxW / charW) + 2;
  int printed  = 0;
  const char* p = text + startCh;
  while (*p && printed < maxChars) {
    display.print(*p++);
    printed++;
  }
}

// ─────────────────────────────────────────────
//  DASHBOARDS
// ─────────────────────────────────────────────
void drawSongDashboard() {
  tcaSelect(0);
  display.clearDisplay();
  display.setTextSize(1);
  display.setTextWrap(false);
  display.setTextColor(SSD1306_WHITE);
  display.setFont(&Org_01);
  drawScrollText(trackTitle,  titleScroll.x,  7, SCROLL_VIEWPORT);
  drawScrollText(trackArtist, artistScroll.x, 15, SCROLL_VIEWPORT);
  display.drawRect(0, 21, 92, 10, SSD1306_WHITE);
  int filled = (int)((trackProgress / 100.0f) * 88.0f);
  if (filled > 0)
    display.fillRect(2, 23, filled, 6, SSD1306_WHITE);
  display.setFont(&Picopixel);
  display.setCursor(96, 27);
  display.print(trackDuration);
  display.display();
}

void drawMainDashboard() {
  tcaSelect(0);
  display.clearDisplay();
  display.setTextWrap(false);
  display.setTextColor(SSD1306_WHITE);
  display.setFont(&FreeMonoBold9pt7b);
  display.setCursor(14, 23);
  display.print(currentTime);
  display.drawLine(0, 26, 127, 26, SSD1306_WHITE);
  display.setFont(&Picopixel);
  display.setCursor(0, 31);
  display.print(isConnected ? "PC connesso" : "Standalone");
  display.display();
}

void drawButtonDashboard() {
  tcaSelect(1);
  display.clearDisplay();
  display.setTextWrap(false);
  display.setTextColor(SSD1306_WHITE);
  display.setTextSize(1);
  display.setFont(NULL);
  display.setCursor(1, 2);
  display.print("AxiDeck");
  display.setCursor(96, 2);
  display.print(currentTime);
  display.drawLine(0, 11, 127, 11, SSD1306_WHITE);

  display.setFont(&Picopixel);
  for (int i = 0; i < LABEL_COUNT; i++) {
    int col = i % 3;
    int row = i / 3;
    int x   = btnX[col];
    int y   = btnY[row];
    char buf[LABEL_MAXLEN + 4];
    if (btnLabels[i][0] != '\0')
      snprintf(buf, sizeof(buf), "%d:%s", i + 1, btnLabels[i]);
    else
      snprintf(buf, sizeof(buf), "%d:", i + 1);

    if (btnState[i]) {
      display.fillRect(x, y - 6, btnBoxW, btnBoxH, SSD1306_WHITE);
      display.setTextColor(SSD1306_BLACK);
    } else {
      display.setTextColor(SSD1306_WHITE);
    }

    display.setCursor(x + 2, y);
    display.print(buf);
  }
  display.display();
}

void redrawScreens() {
  if (musicActive) {
    drawSongDashboard();
  } else {
    drawMainDashboard();
  }
  drawButtonDashboard();
  dashDirty   = false;
  labelsDirty = false;
}

// ─────────────────────────────────────────────
//  I2C BUS RECOVERY
// ─────────────────────────────────────────────
void recoverI2CBus() {
  Serial.println("[i2c] Timeout detected — running recovery...");
  Wire.end();
  pinMode(SDA, OUTPUT);
  pinMode(SCL, OUTPUT);
  digitalWrite(SDA, HIGH);
  for (int i = 0; i < 16; i++) {
    digitalWrite(SCL, LOW);  delayMicroseconds(5);
    digitalWrite(SCL, HIGH); delayMicroseconds(5);
  }
  digitalWrite(SDA, LOW);  delayMicroseconds(5);
  digitalWrite(SCL, HIGH); delayMicroseconds(5);
  digitalWrite(SDA, HIGH); delayMicroseconds(5);

  Wire.begin();
  Wire.setWireTimeout(3000, true);
  delay(50);
  tcaSelect(0); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);
  tcaSelect(1); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);
  dashDirty   = true;
  labelsDirty = true;
  Serial.println("[i2c] Recovery complete.");
}

// ─────────────────────────────────────────────
//  SERIAL PARSER
// ─────────────────────────────────────────────
void parseSerial(String msg) {
  msg.trim();
  if (msg == "CONNECT") {
    isConnected      = true;
    lastPongReceived = millis();
    Serial.println("ACK");
    dashDirty   = true;
    labelsDirty = true;
    return;
  }
  if (msg == "PING") {
    lastPongReceived = millis();
    Serial.println("PONG");
    return;
  }
  if (msg.startsWith("TIME:")) {
    msg.substring(5).toCharArray(currentTime, sizeof(currentTime));
    dashDirty   = true;
    labelsDirty = true;
    return;
  }
  if (msg.startsWith("TRACK:")) {
    String p  = msg.substring(6);
    int    p1 = p.indexOf('|');
    int    p2 = p.indexOf('|', p1 + 1);
    int    p3 = p.indexOf('|', p2 + 1);
    if (p1 < 0 || p2 < 0 || p3 < 0) return;
    p.substring(0,      p1).toCharArray(trackTitle,    sizeof(trackTitle));
    p.substring(p1 + 1, p2).toCharArray(trackArtist,   sizeof(trackArtist));
    p.substring(p2 + 1, p3).toCharArray(trackDuration, sizeof(trackDuration));
    trackProgress = p.substring(p3 + 1).toInt();
    musicActive = true;
    dashDirty   = true;
    resetScrollState();
    return;
  }
  if (msg == "NOTRACK") {
    musicActive = false;
    dashDirty   = true;
    return;
  }
  if (msg.startsWith("LABEL:")) {
    int colon2 = msg.indexOf(':', 6);
    if (colon2 < 0) return;
    int idx = msg.substring(6, colon2).toInt() - 1;
    if (idx < 0 || idx >= LABEL_COUNT) return;
    msg.substring(colon2 + 1).toCharArray(btnLabels[idx], LABEL_MAXLEN);
    saveLabelsToEEPROM();
    labelsDirty = true;
    return;
  }
  if (msg.startsWith("LABELS:")) {
    String p     = msg.substring(7);
    int    start = 0;
    for (int i = 0; i < LABEL_COUNT; i++) {
      int sep = (i < LABEL_COUNT - 1) ? p.indexOf('|', start) : p.length();
      if (sep < 0) sep = p.length();
      p.substring(start, sep).toCharArray(btnLabels[i], LABEL_MAXLEN);
      start = sep + 1;
    }
    saveLabelsToEEPROM();
    labelsDirty = true;
    return;
  }
}

// ─────────────────────────────────────────────
//  KNOBS (Updated for Active Low / Falling Edge detents)
// ─────────────────────────────────────────────
void handleKnobs() {
  int clk1 = digitalRead(clkPin1);
  if (clk1 != lastClkState1) {
    if (clk1 == LOW)   // falling edge = detent
      Serial.println(digitalRead(dtPin1) == HIGH ? "KNOB1+" : "KNOB1-");
  }
  lastClkState1 = clk1;

  int clk2 = digitalRead(clkPin2);
  if (clk2 != lastClkState2) {
    if (clk2 == LOW)   // falling edge = detent
      Serial.println(digitalRead(dtPin2) == HIGH ? "KNOB2+" : "KNOB2-");
  }
  lastClkState2 = clk2;
}

// ─────────────────────────────────────────────
//  BUTTONS (Updated for Active Low)
// ─────────────────────────────────────────────
void handleButtons() {
  unsigned long now     = millis();
  bool          changed = false;
  for (int i = 0; i < 6; i++) {
    // Pin goes LOW when button closes circuit to GND
    bool raw = (digitalRead(btnPins[i]) == LOW); 
    
    if (raw != btnRaw[i]) {
      btnRaw[i]        = raw;
      btnLastChange[i] = now;
    }

    if ((now - btnLastChange[i]) >= DEBOUNCE_MS && raw != btnStable[i]) {
      btnStable[i] = raw;
      btnState[i]  = raw;
      changed      = true;

      Serial.print("BTN:");
      Serial.print(i + 1);
      Serial.println(raw ? ":DOWN" : ":UP");
    }
  }

  if (changed) labelsDirty = true;
}

// ─────────────────────────────────────────────
//  PING / DISCONNECT WATCHDOG
// ─────────────────────────────────────────────
void handlePing() {
  unsigned long now = millis();
  if (isConnected && now - lastPingSent >= PING_INTERVAL) {
    lastPingSent = now;
    Serial.println("PING");
  }

  if (isConnected && now - lastPongReceived >= PING_TIMEOUT) {
    Serial.println("[sys] PC disconnected — returning to waiting screen.");
    isConnected = false;
    musicActive = false;
    showWaitingScreen();
    dashDirty   = true;
    labelsDirty = true;
  }
}

// ─────────────────────────────────────────────
//  SETUP
// ─────────────────────────────────────────────
void setup() {
  Wire.begin();
  Wire.setClock(5000);
  Wire.setWireTimeout(3000, true);
  Serial.begin(9600);

  tcaSelect(0); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);
  tcaSelect(1); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);

  // Encoder pins — Set to INPUT_PULLUP to use internal resistors to 5V
  pinMode(clkPin1, INPUT_PULLUP);
  pinMode(dtPin1,  INPUT_PULLUP);
  lastClkState1 = digitalRead(clkPin1);

  pinMode(clkPin2, INPUT_PULLUP);
  pinMode(dtPin2,  INPUT_PULLUP);
  lastClkState2 = digitalRead(clkPin2);

  // Button pins — Set to INPUT_PULLUP so they rest at HIGH until grounded
  for (int i = 0; i < 6; i++)
    pinMode(btnPins[i], INPUT_PULLUP);

  loadLabelsFromEEPROM();
  display.setTextSize(1);

  showBootScreen();
  showWaitingScreen();
  redrawScreens();
}

// ─────────────────────────────────────────────
//  LOOP
// ─────────────────────────────────────────────
void loop() {
  if (Serial.available() > 0) {
    String msg = Serial.readStringUntil('\n');
    parseSerial(msg);
  }

  handleKnobs();
  handleButtons();
  handlePing();
  tickScroll();

  if (dashDirty || labelsDirty) {
    redrawScreens();
    if (Wire.getWireTimeoutFlag()) {
      Wire.clearWireTimeoutFlag();
      recoverI2CBus();
      redrawScreens();
    }
  }
}