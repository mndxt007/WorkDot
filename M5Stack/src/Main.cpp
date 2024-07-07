#include <M5Unified.h>
#include <WiFiManager.h>
#include <Secrets.h>

// defines
#define SERVER_URL "http://192.168.1.7:5025/i2s_samples"

// globals
WiFiManager wm;

// Methods Declaration
void user_made_log_callback(esp_log_level_t, bool, const char *);
void setupLogging();
void setupWifiManager();

void setup(void)
{
    M5.begin();
    setupLogging();
    setupWifiManager();
}

void loop(void)
{
    M5.update();
}

//Method Definition

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

void setupLogging()
{
    // refering sample - https://github.com/m5stack/M5Unified/blob/master/examples/Basic/LogOutput/LogOutput.ino
    M5.setLogDisplayIndex(0);
    M5.Display.setTextSize(2);
    /// use wrapping from bottom edge to top edge.
    M5.Display.setTextWrap(true, true);
    /// use scrolling.
    M5.Display.setTextScroll(true);

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
    //M5_LOGE("M5_LOGE error log"); /// ERROR level output with source info
    //   M5_LOGW("M5_LOGW warn log");      /// WARN level output with source info
    //   M5_LOGI("M5_LOGI info log");      /// INFO level output with source info
    //   M5_LOGD("M5_LOGD debug log");     /// DEBUG level output with source info
    //   M5_LOGV("M5_LOGV verbose log");   /// VERBOSE level output with source info

    // `M5.Log.printf()` is output without log level and without suffix and is output to all serial, display, and callback.
    // M5.Log.printf("M5.Log.printf non level output\n");
}

void setupWifiManager()
{
    // sample - https://dronebotworkshop.com/wifimanager/
    // To do - On demand Autoconfig - https://github.com/tzapu/WiFiManager/tree/master/examples/OnDemand
    bool res = wm.autoConnect(SSID, PASS); // password protected AP
    if (!res)
    {
        M5.Log(ESP_LOG_INFO, "Failed to connect");
        M5.Log(ESP_LOG_INFO, "Try restarting Core2");
    }
    else
    {
        M5.Log(ESP_LOG_INFO, "Wifi Connected!");
    }
}
