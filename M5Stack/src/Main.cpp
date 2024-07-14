#include <M5Unified.h>
#include <WiFiManager.h>
#include "Startup\Startup.h"
#include "Audio\I2SMEMSSampler.h"
#include <HTTPClient.h>
#include "WebSockets\WebSocketsClient.h"
#include <Secrets.h>
#include "perfmon.h"

// defines
//================
#define SERVER_URL "https://192.168.1.7:7083/i2s_samples"

// statics
//================
static constexpr const size_t record_size = 10000;
static constexpr const size_t record_samplerate = 16000;
static int16_t *rec_data;

// globals
//==================
WiFiManager wm;
I2SSampler *i2sSampler = nullptr;
HTTPClient httpClient;
WebSocketsClient webSocket;
const int SAMPLE_SIZE = 16384;
// uint8_t *samples = nullptr;
static TaskHandle_t wsTaskHandle = NULL;

// Methods Declaration
//===================
void user_made_log_callback(esp_log_level_t, bool, const char *);
void sendDataHttp(uint8_t *bytes, size_t count);
void sendDataW(bool first, bool last, uint8_t *bytes, size_t count);
void quickVibrate();
void webSocketEvent(WStype_t type, uint8_t *payload, size_t length);
void webSocketTask(void *parameter);

// Arduino Methods
//==================
void setup(void)
{
    auto cfg = M5.config();
    M5.begin(cfg);
    setupLogging();
    if (setupWifiManager(wm)) // used the samples from https://dronebotworkshop.com/wifimanager/
    {
        M5.Log(ESP_LOG_DEBUG, "Setting up Websockets");
        webSocket.onEvent(webSocketEvent);
        webSocket.begin("192.168.137.1", 5189, "/ws");
        // To-do - Remove hard coding
        webSocket.setReconnectInterval(5000);
    }
    // setupAudio(i2sSampler); // fork of https://github.com/atomic14/esp32_audio/tree/master

    // using M5.Mic
    //auto miccfg = M5.Mic.config();
    M5.Mic.begin();
    // miccfg.noise_filter_level = (miccfg.noise_filter_level + 8) & 255;
    // M5.Mic.config(miccfg);

    // Audio Setup
    //=======================
    // Create the samples buffer
    rec_data = (typeof(rec_data))heap_caps_malloc(record_size * sizeof(int16_t), MALLOC_CAP_8BIT);
    memset(rec_data, 0, record_size * sizeof(int16_t));

    // Tasks
    //=====================
    xTaskCreate(webSocketTask, "WebSocketTask", 4096, NULL, 1, &wsTaskHandle);
}

void loop(void)
{
    M5.update();
    // webSocket.loop();
    if (M5.BtnA.wasHold())
    {
        if (M5.Mic.record(rec_data, record_size, record_samplerate, false))
        {
            sendDataW(true, false, (uint8_t *)rec_data, record_size * sizeof(uint16_t));
            quickVibrate();
            M5.Log(ESP_LOG_INFO, "\nRecording Started....");
        }
        else
        {
            M5.Log(ESP_LOG_ERROR, "Record failed");
        }
        while (!M5.BtnB.wasHold())
        {
            if (M5.Mic.record(rec_data, record_size, record_samplerate, false))
            {
                sendDataW(false, false, (uint8_t *)rec_data, record_size * sizeof(uint16_t));
                M5.Log(ESP_LOG_INFO, "....");
                M5.update();
            }
            else
            {
                M5.Log(ESP_LOG_ERROR, "Record failed");
            }
            // webSocket.loop();
        }
        quickVibrate();
        sendDataW(false, true, (uint8_t *)rec_data, 0);
        M5.Log(ESP_LOG_INFO, "Recording Ended.");
    }
    if (M5.BtnPWR.isPressed() || M5.BtnC.wasHold())
    {
        quickVibrate();
        wm.startConfigPortal(SSID, PASS);
    }
    M5.delay(200);
}

// Method Definition
//===================================
void user_made_log_callback(esp_log_level_t log_level, bool use_color, const char *log_text)
{
// You can also create your own callback function to output log contents to a file,WiFi,and more other destination
#if defined(ARDUINO)
/*
if (SD.begin(GPIO_NUM_4, SPI, 25000000))
{
    auto file = SD.open("/logfile.txt", FILE_APPEND);
    file.print(log_text);
    file.close();
    SD.end();
}
//*/
#endif
}

void sendDataW(bool first, bool last, uint8_t *bytes, size_t count)
{
    webSocket.sendBIN(first, last, bytes, count, false);
}

void sendDataHttp(uint8_t *bytes, size_t count)
{
    // send them off to the server
    httpClient.begin(SERVER_URL);
    httpClient.addHeader("content-type", "application/octet-stream");
    // see if the above 2 lines are necessary every time.
    httpClient.POST(bytes, count);
    httpClient.end();
}

void quickVibrate()
{
    M5.Power.setVibration(250);
    M5.delay(100);
    M5.Power.setVibration(0);
}

void webSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
    switch (type)
    {
    case WStype_DISCONNECTED:
        M5.Log(ESP_LOG_ERROR, "Server not connected, retrying..\n");
        break;
    case WStype_CONNECTED:
        M5.Log(ESP_LOG_INFO, "Server Connected");
        break;
    case WStype_TEXT:
        // M5.Log(ESP_LOG_INFO,"Response: %s\n", payload);
        if (payload != nullptr)
        {
            M5.Display.printf("%s", payload);
        }
        break;
    case WStype_BIN:
        // Handle binary data if needed
        break;
    case WStype_ERROR:
    case WStype_FRAGMENT_TEXT_START:
    case WStype_FRAGMENT_BIN_START:
    case WStype_FRAGMENT:
    case WStype_FRAGMENT_FIN:
        break;
    }
}

void webSocketTask(void *parameter)
{
    while (true)
    {
        webSocket.loop();
        vTaskDelay(10 / portTICK_PERIOD_MS); // Adjust delay as needed
    }
}

/// for ESP-IDF
#if !defined(ARDUINO) && defined(ESP_PLATFORM)
extern "C"
{
    void loopTask(void *)
    {
        setup();
        for (;;)
        {
            loop();
        }
        vTaskDelete(NULL);
    }

    void app_main()
    {
        xTaskCreatePinnedToCore(loopTask, "loopTask", 8192, NULL, 1, NULL, 1);
    }
}
#endif
