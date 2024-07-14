#include <M5Unified.h>
#include <WiFiManager.h>
#include <Secrets.h>
#include "Startup.h"
#include "WebSockets\WebSocketsClient.h"
#include <FS.h>
#include <SPIFFS.h>
#include <ArduinoJson.h>

#define JSON_CONFIG_FILE "/test_config.json"


auto serverIP = WiFiManagerParameter("server_ip", "Server IP", "127.0.0.1", 50);
JsonDocument json;

void setupLogging()
{
    // refering sample - https://github.com/m5stack/M5Unified/blob/master/examples/Basic/LogOutput/LogOutput.ino
    M5.setLogDisplayIndex(0);
    M5.Display.setTextSize(2);
    /// use wrapping from bottom edge to top edge.
    M5.Display.setTextWrap(true, true);
    /// use scrolling.
    M5.Display.setTextScroll(true);
    //use touch to scroll.

    /// Example of M5Unified log output class usage.
    /// Unlike ESP_LOGx, the M5.Log series can output to serial, display, and user callback function in a single line of code.

    /// You can set Log levels for each output destination.
    /// ESP_LOG_ERROR / ESP_LOG_WARN / ESP_LOG_INFO / ESP_LOG_DEBUG / ESP_LOG_VERBOSE
    M5.Log.setLogLevel(m5::log_target_serial, ESP_LOG_VERBOSE);
    M5.Log.setLogLevel(m5::log_target_display, ESP_LOG_INFO);
    // M5.Log.setLogLevel(m5::log_target_callback, ESP_LOG_INFO);

    /// Set up user-specific callback functions.
    // M5.Log.setCallback(user_made_log_callback);

    /// You can color the log or not.
    M5.Log.setEnableColor(m5::log_target_serial, true);
    M5.Log.setEnableColor(m5::log_target_display, true);
    M5.Log.setEnableColor(m5::log_target_callback, true);

    /// You can set the text to be added to the end of the log for each output destination.
    /// ( default value : "\n" )
    M5.Log.setSuffix(m5::log_target_serial, "\n");
    M5.Log.setSuffix(m5::log_target_display, "\n");
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
    }
    return res;
}


void onWebServerStart()
{
    M5.Log(ESP_LOG_INFO, "\nPlease connect to %s and setup device", SSID);
}


bool loadConfigFile()
// Load existing configuration file
{
    // Uncomment if we need to format filesystem
    // SPIFFS.format();

    // Read configuration from FS json
    M5.Log(ESP_LOG_DEBUG, "Mounting File System...");

    // May need to make it begin(true) first time you are using SPIFFS
    if (SPIFFS.begin(false) || SPIFFS.begin(true))
    {
        M5.Log(ESP_LOG_DEBUG, "mounted file system");
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

                    serverIP.setValue(json["serverIp"], strlen(json["serverIp"]));
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
    }
    else
    {
        // Error mounting file system
        M5.Log(ESP_LOG_DEBUG, "Failed to mount FS");
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

    M5.Log(ESP_LOG_INFO, "Configuration saved. Restarting the Device!");
    M5.delay(2000);
    ESP.restart();

}
