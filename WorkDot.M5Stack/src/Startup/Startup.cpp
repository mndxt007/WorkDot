#include <M5Unified.h>
#include <WiFiManager.h>
#include <Secrets.h>
#include "Startup.h"
#include "WebSockets\WebSocketsClient.h"
#include <FS.h>
#include <SPIFFS.h>
#include <ArduinoJson.h>
#include <Graphics/ui.h>
#include <lvgl.h>

#define JSON_CONFIG_FILE "/test_config.json"
#define LV_CONF_INCLUDE_SIMPLE

constexpr int32_t HOR_RES = 320;
constexpr int32_t VER_RES = 240;
std::string setupMessage = std::string("\nPlease connect to ") + SSID + "Hotspot and setup device. Device will be paused until this is done.";

auto serverIP = WiFiManagerParameter("server_ip", "Server IP", "127.0.0.1", 50);
JsonDocument json;
lv_display_t *display;
lv_indev_t *indev;

void setupLogging()
{
    // refering sample - https://github.com/m5stack/M5Unified/blob/master/examples/Basic/LogOutput/LogOutput.ino
    M5.setLogDisplayIndex(0);
    M5.Display.setTextSize(2);
    /// use wrapping from bottom edge to top edge.
    M5.Display.setTextWrap(true, true);
    /// use scrolling.
    M5.Display.setTextScroll(true);
    // use touch to scroll.
    /// Example of M5Unified log output class usage.
    /// Unlike ESP_LOGx, the M5.Log series can output to serial, display, and user callback function in a single line of code.

    /// You can set Log levels for each output destination.
    /// ESP_LOG_ERROR / ESP_LOG_WARN / ESP_LOG_INFO / ESP_LOG_DEBUG / ESP_LOG_VERBOSE
    M5.Log.setLogLevel(m5::log_target_serial, ESP_LOG_VERBOSE);
    M5.Log.setLogLevel(m5::log_target_display, ESP_LOG_NONE);
    //  M5.Log.setLogLevel(m5::log_target_callback, ESP_LOG_INFO);

    /// Set up user-specific callback functions.
    // M5.Log.setCallback(user_made_log_callback);

    /// You can color the log or not.
    M5.Log.setEnableColor(m5::log_target_serial, true);
    //M5.Log.setEnableColor(m5::log_target_display, true);
    M5.Log.setEnableColor(m5::log_target_callback, true);

    /// You can set the text to be added to the end of the log for each output destination.
    /// ( default value : "\n" )
    M5.Log.setSuffix(m5::log_target_serial, "\n");
    //M5.Log.setSuffix(m5::log_target_display, "\n");
    M5.Log.setSuffix(m5::log_target_callback, "\n");

    // `M5.Log()` can be used to output a simple log
    //   M5.Log(ESP_LOG_ERROR, "M5.Log error log"); /// ERROR level output
    //   M5.Log(ESP_LOG_WARN    , "M5.Log warn log");     /// WARN level output
    //   M5.Log(ESP_LOG_INFO    , "M5.Log info log");     /// INFO level output
    //   M5.Log(ESP_LOG_DEBUG   , "M5.Log debug log");    /// DEBUG level output
    //   M5.Log(ESP_LOG_VERBOSE , "M5.Log verbose log");  /// VERBOSE level output

    // `M5_LOGx` macro can be used to output a log containing the source file name, line number, and function name.
    // M5_LOGE("M5_LOGE error log"); /// ERROR level output with source info
    //   M5_LOGW("M5_LOGW warn log");      /// WARN level output with source info
    //   M5_LOGI("M5_LOGI info log");      /// INFO level output with source info
    //   M5_LOGD("M5_LOGD debug log");     /// DEBUG level output with source info
    //   M5_LOGV("M5_LOGV verbose log");   /// VERBOSE level output with source info

    // `M5.Log.printf()` is output without log level and without suffix and is output to all serial, display, and callback.
    // M5.Log.printf("M5.Log.printf non level output\n");
}

bool setupWifiManager(WiFiManager &wm)
{
    // sample - https://dronebotworkshop.com/wifimanager/
    wm.setSaveParamsCallback(saveConfigFile);
    wm.addParameter(&serverIP);
    wm.setWebServerCallback(onWebServerStart);
    if (SPIFFS.begin(false) || SPIFFS.begin(true))
    {
        M5.Log(ESP_LOG_DEBUG, "mounted file system");
    }
        else
    {
        // Error mounting file system
        M5.Log(ESP_LOG_DEBUG, "Failed to mount FS");
    }
    bool res = wm.autoConnect(SSID, PASS); // password protected AP
    if (!loadConfigFile())
    {
        M5.Log(ESP_LOG_DEBUG, "SPIFF File not found");
        wm.startConfigPortal(SSID, PASS);
    }
    if (!res)
    {
        M5.Log(ESP_LOG_INFO, "Failed to connect");
        M5.Log(ESP_LOG_INFO, "Try restarting Core2");
    }
    else
    {
        M5.Log(ESP_LOG_INFO, "Wifi Connected!");
        lv_imagebutton_set_state(ui_Wifi, LV_IMAGEBUTTON_STATE_CHECKED_PRESSED);

    }
    return res;
}

void onWebServerStart()
{
    M5.Log(ESP_LOG_INFO, setupMessage.c_str());
    //lv_textarea_set_text(ui_SetupMessage, setupMessage.c_str());
}

void my_display_flush(lv_display_t *disp, const lv_area_t *area, uint8_t *px_map)
{
    uint32_t w = (area->x2 - area->x1 + 1);
    uint32_t h = (area->y2 - area->y1 + 1);

    lv_draw_sw_rgb565_swap(px_map, w * h);
    M5.Display.pushImageDMA<uint16_t>(area->x1, area->y1, w, h, (uint16_t *)px_map);
    lv_disp_flush_ready(disp);
}

uint32_t my_tick_function()
{
    return (esp_timer_get_time() / 1000LL);
}

void my_touchpad_read(lv_indev_t *drv, lv_indev_data_t *data)
{
    M5.update();
    auto count = M5.Touch.getCount();

    if (count == 0)
    {
        data->state = LV_INDEV_STATE_RELEASED;
    }
    else
    {
        auto touch = M5.Touch.getDetail(0);
        data->state = LV_INDEV_STATE_PRESSED;
        data->point.x = touch.x;
        data->point.y = touch.y;
    }
}

void setupUI()
{
    lv_init();

    lv_tick_set_cb(my_tick_function);

    display = lv_display_create(HOR_RES, VER_RES);
    lv_display_set_flush_cb(display, my_display_flush);

    static lv_color_t buf1[HOR_RES * 15];
    lv_display_set_buffers(display, buf1, nullptr, sizeof(buf1), LV_DISPLAY_RENDER_MODE_PARTIAL);

    indev = lv_indev_create();
    lv_indev_set_type(indev, LV_INDEV_TYPE_POINTER);

    lv_indev_set_read_cb(indev, my_touchpad_read);

    // lv_display_set_rotation(display,LV_DISPLAY_ROTATION_90);
    // M5.Display.setRotation(90);
    ui_init();
    lv_textarea_set_text(ui_SetupMessage, setupMessage.c_str());
}

bool loadConfigFile()
// Load existing configuration file
{
    // Uncomment if we need to format filesystem
    // SPIFFS.format();

    // Read configuration from FS json
    M5.Log(ESP_LOG_DEBUG, "Mounting File System...");

    // May need to make it begin(true) first time you are using SPIFFS

    if (SPIFFS.exists(JSON_CONFIG_FILE))
    {
        // The file exists, reading and loading
        M5.Log(ESP_LOG_DEBUG, "reading config file");
        File configFile = SPIFFS.open(JSON_CONFIG_FILE, "r");
        if (configFile)
        {
            M5.Log(ESP_LOG_DEBUG, "Opened configuration file");
            // StaticJsonDocument<512> json;
            DeserializationError error = deserializeJson(json, configFile);
            serializeJsonPretty(json, Serial);
            if (!error)
            {
                M5.Log(ESP_LOG_DEBUG, "Parsing JSON");

                serverIP.setValue(json["serverIp"], 50);
                // testNumber = json["testNumber"].as<int>();

                return true;
            }
            else
            {
                // Error loading JSON data
                M5.Log(ESP_LOG_DEBUG, "Failed to load json config");
            }
        }
    }

    return false;
}

void saveConfigFile()
// Save Config in JSON format
{
    M5.Log(ESP_LOG_DEBUG, "Saving configuration...");

    // Create a JSON document
    // StaticJsonDocument<512> json;
    //   json["testString"] = testString;
    //   json["testNumber"] = testNumber;
    json["serverIp"] = serverIP.getValue();

    // Open config file
    File configFile = SPIFFS.open(JSON_CONFIG_FILE, "w");
    if (!configFile)
    {
        // Error, file did not open
        M5.Log(ESP_LOG_DEBUG, "failed to open config file for writing");
    }

    // Serialize JSON data to write to file
    serializeJsonPretty(json, Serial);
    if (serializeJson(json, configFile) == 0)
    {
        // Error writing file
        M5.Log(ESP_LOG_DEBUG, "Failed to write to file");
    }
    // Close file
    configFile.close();
    M5.Log(ESP_LOG_INFO, "Configuration saved.");
    //M5.delay(2000);
    //ESP.restart();
}
