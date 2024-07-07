#include <M5Unified.h>
#include <WiFiManager.h>
#include "Startup\Startup.h"


// defines
#define SERVER_URL "http://192.168.1.7:5025/i2s_samples"

// globals
WiFiManager wm;

// Methods Declaration
void user_made_log_callback(esp_log_level_t, bool, const char *);

void setup(void)
{
    M5.begin();
    setupLogging();
    setupWifiManager(wm);
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


