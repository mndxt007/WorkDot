/*
*******************************************************************************
* Copyright (c) 2021 by M5Stack
*                  Equipped with M5Core2 sample source code
*                          配套  M5Core2 示例源代码
* Visit for more information: https://docs.m5stack.com/en/core/core2
* 获取更多资料请访问: https://docs.m5stack.com/zh_CN/core/core2
*
* Describe: BasicHTTPClient.
* Date: 2021/8/4
******************************************************************************
*/
#include <M5Core2.h>
#include <Arduino.h>
// #include <WiFi.h>
// #include <WiFiMulti.h>
#include <HTTPClient.h>
#include <Secrets.h>
#include <WiFiManager.h>
#include <ArduinoWebsockets.h>
using namespace websockets;

// WiFiMulti wifiMulti;
HTTPClient http;
WiFiManagerParameter custom_text_box("GET_URL", "Enter the GET URL", "http://example.com/index.html", 50);
char microphonedata0[1024 * 4];
int data_offset = 0;
bool ranAlready = true;
const char *websockets_server = "192.168.1.7";
const int port = 5189;
const char *path = "/ws";
WebsocketsClient client;

void HttpTest();
void ButtonRead(void *pvParameters);
void DisplayInit();

void onMessageCallback(WebsocketsMessage message)
{
    Serial.print("Got Message: ");
    Serial.println(message.data());
    M5.Lcd.setCursor(0, 320);
    M5.Lcd.setTextSize(1);
    M5.Lcd.println(message.data());
}

void onEventsCallback(WebsocketsEvent event, String data)
{
    if (event == WebsocketsEvent::ConnectionOpened)
    {
        Serial.println("Connnection Opened");
    }
    else if (event == WebsocketsEvent::ConnectionClosed)
    {
        Serial.println("Connnection Closed");
    }
    else if (event == WebsocketsEvent::GotPing)
    {
        Serial.println("Got a Ping!");
    }
    else if (event == WebsocketsEvent::GotPong)
    {
        Serial.println("Got a Pong!");
    }
}

void setup()
{
    M5.begin(true, true, true, true, kMBusModeOutput,
             true);            // Init M5Core2.  初始化 M5Core2
    //M5.Axp.SetSpkEnable(true); // Enable speaker power.  启用扬声器电源
    M5.Spk.InitI2SSpeakOrMic(MODE_MIC);
    DisplayInit();
    // wifiMulti.addAP("SSID",
    //                "PASS");  // Storage wifi configuration information. added Wifi Manager
    M5.Lcd.print("\nConnecting Wifi...\n"); // print format output string on lcd
    WiFiManager wm;

    // reset settings - wipe stored credentials for testing
    // these are stored by the esp library
    // wm.resetSettings();
    wm.addParameter(&custom_text_box);
    // M5.Buttons.addHandler(StartWifi, )
    bool res;
    // res = wm.autoConnect(); // auto generated AP name from chipid
    // res = wm.autoConnect("AutoConnectAP"); // anonymous ap
    res = wm.autoConnect(SSID, PASS); // password protected ap

    if (!res)
    {
        Serial.println("Failed to connect");
        // ESP.restart();
    }
    else
    {
        // if you get here you have connected to the WiFi
        Serial.println("connected...yeey :)");
        client.onMessage(onMessageCallback);
        client.onEvent(onEventsCallback);

        // Connect to server
        if (client.connect(websockets_server, port, path))
        {
            //client.send("Hi Server!");
            // Send a ping
            client.ping();
            xTaskCreatePinnedToCore(ButtonRead, "task2", 4096, NULL, tskIDLE_PRIORITY, NULL, 0);
        }
        // Send a message
    }
}

void loop()
{
    client.poll();
}

void HttpTest()
{
    M5.Lcd.setCursor(0, 0); // Set the cursor at (0,0).
    M5.Lcd.print("[HTTP] begin...\n");
    http.begin(custom_text_box.getValue()); // configure traged server and url
    M5.Lcd.print("[HTTP] GET...\n");
    int httpCode = http.GET(); // start connection and send HTTP header.
    Serial.println("Going to light sleep for 5 seconds.");
    if (httpCode >
        0)
    { // httpCode will be negative on error.
        M5.Lcd.printf("[HTTP] GET... code: %d\n", httpCode);

        if (httpCode ==
            HTTP_CODE_OK)
        { // file found at server.
            String payload = http.getString();
            M5.Lcd.println(payload); // Print files read on the server
        }
    }
    else
    {
        M5.Lcd.printf("[HTTP] GET... failed, error: %s\n",
                      http.errorToString(httpCode).c_str());
    }
    http.end();
    delay(5000);
    M5.Lcd.clear(); // clear the screen
}

void ButtonRead(void *pvParameters)
{
    while (1)
    {
        M5.update();
        if (M5.BtnA.pressedFor(500))
        {
            M5.Axp.SetVibration(true); // Open the vibration.   开启震动马达
            vTaskDelay(100);
            M5.Axp.SetVibration(false);
            data_offset = 0;
            Serial.println("Before Speak");
            Serial.println("After Speak");
            size_t byte_read;
            while (1)
            {
                M5.Lcd.setCursor(0, 200);
                M5.Lcd.println("Recording......");
                Serial.println("Recording......");
                // M5.Lcd.clearDisplay
                i2s_read(Speak_I2S_NUMBER,
                         (char *)(microphonedata0 + data_offset), DATA_SIZE,
                         &byte_read, (100 / portTICK_RATE_MS));
                data_offset += 1024;
                if (data_offset == 1024 * 4)
                {
                    Serial.println("Sending buffer");
                    client.sendBinary(microphonedata0, 1024 * 4);
                    data_offset=0;
                }
                if (M5.BtnA.wasReleasefor(1000))
                {
                    M5.Lcd.setCursor(0, 120);
                    // Serial.print("buffer exceeded? - ");
                    Serial.println("ButtonA released");
                    M5.Lcd.printf("Out of while loop");
                    break;
                }
                M5.update();
            }
            //size_t bytes_written;
            //M5.Spk.InitI2SSpeakOrMic(MODE_SPK);
            //i2s_write(Speak_I2S_NUMBER, microphonedata0, data_offset,
            //          &bytes_written, portMAX_DELAY);
            vTaskDelay(1000);
            // M5.Axp.PowerOff();
            DisplayInit();
        }
    }
}

void DisplayInit()
{                             // Initialize the display. 显示屏初始化
    M5.Lcd.fillScreen(WHITE); // Set the screen background color to white.
                              // 设置屏幕背景色为白色
    M5.Lcd.setTextColor(
        BLACK);            // Set the text color to black.  设置文字颜色为黑色
    M5.Lcd.setTextSize(2); // Set font size to 2.  设置字体大小为2
    M5.Lcd.setTextColor(RED);
    M5.Lcd.setCursor(0,
                     10);       // Set the cursor at (10,10).  将光标设在（10，10）处
    M5.Lcd.printf("Recorder!"); // The screen prints the formatted string and
                                // wraps it.  屏幕打印格式化字符串并换行
    M5.Lcd.setTextColor(BLACK);
    M5.Lcd.setCursor(0, 26);
    M5.Lcd.printf("Press Left Button to recording!");
    delay(100); // delay 100ms.  延迟100ms
}
