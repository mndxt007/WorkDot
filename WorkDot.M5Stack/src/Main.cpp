#include <M5Unified.h>
#include <WiFiManager.h>
#include "Startup\Startup.h"
#include "WebSockets\WebSocketsClient.h"
#include <Secrets.h>
#include <ArduinoJson.h>
#include <Graphics/ui.h>
#include <lvgl.h>
#include "esp_task_wdt.h"

// defines
//================
#define SERVER_IP "192.168.29.145"
#define QUEUE_LENGTH 10
#define ITEM_SIZE sizeof(char[1024])

// statics
//================
static constexpr const size_t record_size = 10000;
static constexpr const size_t record_samplerate = 16000;
static int16_t *rec_data;
static TaskHandle_t wsTaskHandle = NULL;
static TaskHandle_t configTaskHandle = NULL;

// globals
//==================
WiFiManager wm;
WebSocketsClient webSocket;
const int SAMPLE_SIZE = 16384;
// uint8_t *samples = nullptr;
extern JsonDocument json;
bool recording = false;
SemaphoreHandle_t xMutex;
QueueHandle_t payloadQueue;

// Methods Declaration
//===================
void user_made_log_callback(esp_log_level_t, bool, const char *);
void sendDataHttp(uint8_t *bytes, size_t count);
void sendDataW(bool first, bool last, uint8_t *bytes, size_t count);
void quickVibrate();
void webSocketEvent(WStype_t type, uint8_t *payload, size_t length);
void webSocketTask(void *parameter);
void lvtask(void *parameter);
void startRecording(lv_event_t *e);
void stopRecording(lv_event_t *e);
void startConfig(lv_event_t *e);
void my_log_cb(lv_log_level_t level, const char *buf);
void update_textarea_async(void *param);
void start_config_task(void *param);
void restart(lv_event_t *e);

// Arduino Methods
//==================
void setup(void)
{
    auto cfg = M5.config();
    M5.begin(cfg);
    setupLogging();
    setupUI();
    xTaskCreatePinnedToCore(lvtask, "lvtask", 8192, NULL, 2, NULL, 0);
    // Disabling due to watch dog timer resets:
    esp_task_wdt_delete(xTaskGetIdleTaskHandleForCPU(0));

    if (setupWifiManager(wm)) // used the samples from https://dronebotworkshop.com/wifimanager/
    {
        const char *serverIp = json["serverIp"]; // Extract the server IP as a C-string
        if (serverIp != nullptr)
        {
            M5.Log(ESP_LOG_DEBUG, "Server IP - %s", serverIp);
        }
        else
        {
            M5.Log(ESP_LOG_DEBUG, "Server IP not found in JSON");
            serverIp = SERVER_IP;
        }
        M5.Log(ESP_LOG_DEBUG, "Setting up Websockets with Server %s", serverIp);
        webSocket.onEvent(webSocketEvent);
        webSocket.begin(serverIp, 5189, "/ws");
        // To-do - Remove hard coding
        webSocket.setReconnectInterval(5000);
    }

    // using M5.Mic
    // auto miccfg = M5.Mic.config();
    M5.Mic.begin();
    // miccfg.noise_filter_level = (miccfg.noise_filter_level + 8) & 255;
    // M5.Mic.config(miccfg);

    // Audio Setup
    //=======================
    // Create the samples buffer
    rec_data = (typeof(rec_data))heap_caps_malloc(record_size * sizeof(int16_t), MALLOC_CAP_8BIT);
    memset(rec_data, 0, record_size * sizeof(int16_t));

    // UI Setup
    //========================
#if LV_USE_LOG != 0
    lv_log_register_print_cb(my_log_cb);
#endif
    xMutex = xSemaphoreCreateMutex();
    if (xMutex == NULL)
    {
        M5.Log(ESP_LOG_ERROR, "Mutex failed");
    }
    lv_textarea_set_cursor_click_pos(ui_TextArea1, false);
    lv_obj_add_event_cb((lv_obj_t *)ui_Image12, startRecording, LV_EVENT_LONG_PRESSED, NULL);
    lv_obj_add_event_cb((lv_obj_t *)ui_RecordSmall, startRecording, LV_EVENT_LONG_PRESSED, NULL);
    lv_obj_add_event_cb((lv_obj_t *)ui_Image12, stopRecording, LV_EVENT_RELEASED, NULL);
    lv_obj_add_event_cb((lv_obj_t *)ui_RecordSmall, stopRecording, LV_EVENT_RELEASED, NULL);
    lv_obj_add_event_cb((lv_obj_t *)ui_Setup, startConfig, LV_EVENT_SCREEN_LOADED, NULL);
    lv_obj_add_event_cb((lv_obj_t *)ui_BackButton, restart, LV_EVENT_CLICKED, NULL);

    // Queue
    // ======================
    payloadQueue = xQueueCreate(QUEUE_LENGTH, ITEM_SIZE);
    // Tasks
    //=====================
    // xTaskCreate(webSocketTask, "WebSocketTask", 4096, NULL, 1, &wsTaskHandle);
    // xTaskCreatePinnedToCore(record, "recordTask", 8192, NULL, 2, NULL, 0);
    xTaskCreatePinnedToCore(webSocketTask, "WebSocketTask", 8192, NULL, 1, &wsTaskHandle, 1);
}

void loop(void)
{
    if (recording)
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
        while (recording)
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
    // if (M5.BtnPWR.isPressed() || M5.BtnC.wasHold())
    // {
    //     quickVibrate();
    //     wm.startConfigPortal(SSID, PASS);
    // }
    vTaskDelay(10 / portTICK_PERIOD_MS);
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

void startRecording(lv_event_t *e)
{
    recording = true;
    M5.Log(ESP_LOG_INFO, "Recording enabled");
}

void quickVibrate()
{
    M5.Power.setVibration(250);
    M5.delay(100);
    M5.Power.setVibration(0);
}

void stopRecording(lv_event_t *e)
{
    recording = false;
    M5.Log(ESP_LOG_INFO, "Recording disabled");
}

void startConfig(lv_event_t *e)
{
    quickVibrate();
    xTaskCreate(start_config_task, "configTask", 8192, NULL, 1, &configTaskHandle);
}

void restart(lv_event_t *e)
{
    M5.delay(2000);
    // restart Esp32
    esp_restart();
}

void start_config_task(void *param)
{
    vTaskSuspend(wsTaskHandle);
    wm.startConfigPortal(SSID, PASS);
    lv_textarea_set_text(ui_SetupMessage, "Setup complete, restarting...");
    esp_restart();
    // restart Esp32
}

void webSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
    switch (type)
    {
    case WStype_DISCONNECTED:
        M5.Log(ESP_LOG_ERROR, "Server not connected, retrying..\n");
        lv_imagebutton_set_state(ui_ServerConn, LV_IMAGEBUTTON_STATE_CHECKED_RELEASED);
        break;
    case WStype_CONNECTED:
        M5.Log(ESP_LOG_INFO, "Server Connected");
        lv_imagebutton_set_state(ui_ServerConn, LV_IMAGEBUTTON_STATE_CHECKED_PRESSED);
        break;
    case WStype_TEXT:
        // M5.Log(ESP_LOG_INFO,"Response: %s\n", payload);
        if (payload != nullptr)
        {
            // M5.Log(ESP_LOG_INFO, "Incoming data start");

            if (xSemaphoreTake(xMutex, portMAX_DELAY) == pdTRUE)
            {
                xQueueSend(payloadQueue, (void *)payload, portMAX_DELAY);
                xSemaphoreGive(xMutex);
            }

            lv_async_call(update_textarea_async, NULL);
            // M5.Log(ESP_LOG_INFO, "Incoming data end");
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

void lvtask(void *parameter)
{
    while (1)
    {
        lv_task_handler();
        vTaskDelay(1); // Adjust delay as needed
    }
}

void my_log_cb(lv_log_level_t level, const char *buf)
{
    M5.Log(ESP_LOG_INFO, buf);
}

void update_textarea_async(void *param)
{
    char payload[1024];
    if (xSemaphoreTake(xMutex, portMAX_DELAY) == pdTRUE)
    {
        if (xQueueReceive(payloadQueue, &payload, 0) == pdTRUE)
        {
            lv_textarea_add_text(ui_TextArea1, payload);
        }
        xSemaphoreGive(xMutex);
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
