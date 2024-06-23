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

// WiFiMulti wifiMulti;
HTTPClient http;
WiFiManagerParameter custom_text_box("GET_URL", "Enter the GET URL", "http://example.com/index.html", 50);

void HttpTest();
void ButtonRead(void *pvParameters);

bool ranAlready = true;

void setup()
{
    M5.begin(); // Init M5Core2.
    // wifiMulti.addAP("SSID",
    //                "PASS");  // Storage wifi configuration information. added Wifi Manager
    xTaskCreatePinnedToCore(ButtonRead, "task2", 4096, NULL, tskIDLE_PRIORITY, NULL, 0);
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
       
    }
}

void loop()
{
    if(ranAlready)
    {
         HttpTest();
         ranAlready = false;
    }
    vTaskDelay(5000);
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
        M5.update(); // Read the press state of the key.  读取按键 A, B, C 的状态
        if (M5.BtnA.wasReleased() || M5.BtnA.pressedFor(1000, 200))
        {
            M5.Lcd.print('A');
        }
        else if (M5.BtnB.wasReleased() || M5.BtnB.pressedFor(1000, 200))
        {
            M5.Lcd.print('B');
        }
        else if (M5.BtnC.wasReleased() || M5.BtnC.pressedFor(1000, 200))
        {
            M5.Lcd.print('C');
        }
        else if (M5.BtnB.wasReleasefor(700))
        {
            M5.Lcd.clear(WHITE); // Clear the screen and set white to the
                                 // background color.  清空屏幕并将白色设置为底色
            M5.Lcd.setCursor(0, 0);
        }
    }
}
