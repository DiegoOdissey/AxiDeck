// AxiDeck Firmware v0.5

// Per il mio carissimo amico Axel, che ormai conosco da penso più di 10 anni...
// E' il primo regalo che faccio con le mie mani per qualcuno. E probabilmente
// lo farò di nuovo. Anche se non penso ci sia la possibilità che tu legga questo
// codice, buon compleanno <3

#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include "Fonts/FreeMonoBold9pt7b.h"
#include "Fonts/Org_01.h"
#include <Fonts/Picopixel.h>

#define OLED_ADDR 0x3C

// Oled screen initialization
Adafruit_SSD1306 display(128, 32, &Wire, -1);

// Knob pins and variables
const int clkPin1 = 5;
const int dtPin1 = 4;
int lastClkState1;

const int clkPin2 = 7;
const int dtPin2 = 6;
int lastClkState2;

// Button pins
const int btnPins[6] = {A0, 12, 11, 10, 9, 8};
bool btnState[6] = {false, false, false, false, false, false};

// This variable hold the information of whether the AxiDeck is connected
// and is getting responses from the PC or not
bool isConnected = false;

/*
  KNOB1+     Knob 1 turned clockwise
  KNOB1-     Knob 1 turned anticlockwise

  KNOB2+     Knob 2 turned clockwise
  KNOB2-     Knob 2 turned anticlockwise
*/

// Setup function is here, LOOP is at the BOTTOM of the code
void setup() {
  Wire.begin();
  Serial.begin(9600);

  // OLED screens setup
  tcaSelect(0); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);
  tcaSelect(1); display.begin(SSD1306_SWITCHCAPVCC, OLED_ADDR);

  // Knob setup
  pinMode(clkPin1, INPUT);
  pinMode(dtPin1, INPUT);
  lastClkState1 = digitalRead(clkPin1);

  pinMode(clkPin2, INPUT);
  pinMode(dtPin2, INPUT);
  lastClkState2 = digitalRead(clkPin2);

  // Button setup
  for (int i = 0; i < 6; i++) {
    pinMode(btnPins[i], INPUT_PULLUP);
  }

  delay(100); 
  
  if (Serial.available() > 0) {
    String check = Serial.readStringUntil('\n');
    if (check.indexOf("CONNECT") >= 0) {
      isConnected = true;
    }
  }

  // If already connected via the reset handshake, maybe skip the long boot?
  if (!isConnected) {
    showBootScreen();
  }
  display.setTextSize(0);
  showWaitingScreen(); // This will exit immediately if isConnected is true
  showMainDashboard();
} 

void tcaSelect(uint8_t channel) {
  Wire.beginTransmission(0x70);
  Wire.write(1 << channel);
  Wire.endTransmission();
}

// Every bitmap is declared here

static const unsigned char trussi_T [] PROGMEM = {
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 
  0xff, 0xff, 0xff, 0xfe, 0xff, 0xff, 0xff, 0xfc, 0xff, 0xff, 0xff, 0xf8, 0xff, 0xff, 0xff, 0xf0, 
  0xff, 0xff, 0xff, 0xe0, 0xff, 0xff, 0xff, 0xc0, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 
  0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 
  0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 
  0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 0x00, 0x1f, 0xe0, 0x00, 
  0x00, 0x1f, 0xc0, 0x00, 0x00, 0x1f, 0x80, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x1e, 0x00, 0x00, 
  0x00, 0x1c, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
};
static const unsigned char PROGMEM image_music_pause_bits[] = {0xf9,0xf0,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0x89,0x10,0xf9,0xf0,0x00,0x00};
static const unsigned char PROGMEM image_usb_cable_connected_bits[] = {0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xc0,0x03,0xe0,0x04,0xc0,0x08,0x04,0xc8,0x06,0xff,0xff,0xc2,0x06,0x02,0x04,0x01,0x30,0x00,0xf8,0x00,0x30,0x00,0x00,0x00,0x00};
static const unsigned char PROGMEM image_hour_glass_75_bits[] = {0xff,0xe0,0x40,0x40,0x40,0x40,0x51,0x40,0x5f,0x40,0x2e,0x80,0x15,0x00,0x0a,0x00,0x0a,0x00,0x11,0x00,0x24,0x80,0x44,0x40,0x4e,0x40,0x5f,0x40,0x7f,0xc0,0xff,0xe0};
static const unsigned char PROGMEM image_hour_glass_75_bits_rotated[] = {0x80,0x01,0xf8,0x1f,0xc4,0x21,0xe2,0x59,0xf1,0xb1,0xfc,0x71,0xf1,0xb1,0xe2,0x59,0xc4,0x21,0xf8,0x1f,0x80,0x01};

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

void handleKnobs() {
  // Knob 1
  int currentClkState1 = digitalRead(clkPin1);
  if(currentClkState1 != lastClkState1) {
    if(digitalRead(dtPin1) != currentClkState1) {
      Serial.println("KNOB1+");
    } else {
      Serial.println("KNOB1-");
    }
  }
  lastClkState1 = currentClkState1;

  // Knob 2
  int currentClkState2 = digitalRead(clkPin2);
  if(currentClkState2 != lastClkState2) {
    if(digitalRead(dtPin2) != currentClkState2) {
      Serial.println("KNOB2+");
    } else {
      Serial.println("KNOB2-");
    }
  }
  lastClkState2 = currentClkState2;
}

void showWaitingScreen() {
  tcaSelect(1);
  unsigned long lastToggle = 0;
  bool frame = false;

  while (!isConnected) {
    // 1. Non-blocking Serial Check
    if (Serial.available() > 0) {
      String msg = Serial.readStringUntil('\n');
      msg.trim(); 
      if (msg == "CONNECT") {
        isConnected = true;
        break; 
      }
    }

    // 2. Non-blocking Animation (updates every 500ms)
    if (millis() - lastToggle >= 500) {
      lastToggle = millis();
      frame = !frame; // Switch between dots/icons

      display.clearDisplay();
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

void showMainDashboard() {
  // Code generated by Lopaka
  tcaSelect(0);
  display.setTextSize(0);
  display.clearDisplay();
  display.setTextColor(1);
  display.setTextWrap(false);
  display.setFont(&Org_01);
  display.setCursor(1, 6);
  display.print("AxiDeck - CONNESSO");
  display.drawLine(0, 9, 127, 9, 1);
  display.setFont(&Picopixel);
  display.setCursor(3, 18);
  display.print("BTN1");
  display.setCursor(3, 26);
  display.print("BTN4");
  display.setCursor(45, 26);
  display.print("BTN5");
  display.setCursor(45, 18);
  display.print("BTN2");
  display.setCursor(89, 26);
  display.print("BTN6");
  display.setCursor(89, 18);
  display.print("BTN3");

  display.display();


  tcaSelect(1);
  display.clearDisplay();
  display.setTextColor(1);
  display.setTextWrap(false);
  display.setFont(&FreeMonoBold9pt7b);
  display.setCursor(32, 15);
  display.print("12:15");
  display.drawBitmap(2, 17, image_music_pause_bits, 12, 16, 1);
  display.drawRect(21, 21, 104, 9, 1);
  display.fillRect(23, 23, 49, 5, 1);
  display.drawBitmap(111, 2, image_usb_cable_connected_bits, 16, 16, 1);

  display.display();
}

void handleButtons() {
  // Button label positions on display 0: {x, y}
  // Order: BTN1, BTN2, BTN3, BTN4, BTN5, BTN6
  const int btnX[6] = {3, 45, 89,  3, 45, 89};
  const int btnY[6] = {9, 11, 11, 19, 19, 19};
  // Width/height of each label background box
  const int boxW = 18;
  const int boxH = 9;

  bool changed = false;

  for (int i = 0; i < 6; i++) {
    bool pressed = (digitalRead(btnPins[i]) == LOW); // LOW = pressed (pullup)
    if (pressed != btnState[i]) {
      btnState[i] = pressed;
      changed = true;
    }
  }

  if (!changed) return; // Skip redraw if nothing changed

  tcaSelect(0);
  display.clearDisplay();

  // Redraw static elements
  display.setTextColor(1);
  display.setTextWrap(false);
  display.setFont(&Org_01);
  display.setCursor(1, 6);
  display.print("AxiDeck - CONNESSO");
  display.drawLine(0, 9, 127, 9, 1);

  // Redraw buttons with invert if pressed
  display.setFont(&Picopixel);
  const char* labels[6] = {"BTN1","BTN2","BTN3","BTN4","BTN5","BTN6"};

  for (int i = 0; i < 6; i++) {
    if (btnState[i]) {
      display.fillRect(btnX[i] - 1, btnY[i] - 1, boxW, boxH, SSD1306_WHITE);
      display.setTextColor(SSD1306_BLACK);
    } else {
      display.setTextColor(SSD1306_WHITE);
    }
    display.setCursor(btnX[i], btnY[i] + 7);
    display.print(labels[i]);
  }

  display.display();
}

void loop() {
  handleKnobs();
  handleButtons();
}
