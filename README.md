# WorkDot

**WorkDot** is a compact AI-powered embedded device designed to supercharge your productivity by simplifying the way you manage tasks, emails, meetings, and more—hands-free and distraction-free.

![WorkDot Device](Resources/0.png)

---

## 🚀 Overview

In early 2024, the tech landscape saw the emergence of a new wave of "Personal AI Devices"—innovations like the Rabbit R1 and Humane AI Pin. These devices promised seamless, natural-language interactions for everyday tasks like ordering food, booking cabs, or controlling smart homes.

**But what about work?**

As working professionals and embedded systems enthusiasts, we envisioned a device tailored specifically for workplace productivity. Enter **WorkDot**—an embedded AI device that fits in your palm and transforms how you plan your day, manage emails, and interact with your work data.

---

## 🧠 What It Does

**WorkDot** acts as your personal work assistant by:

- Processing voice commands to manage emails, meetings, to-do lists, and schedules.
- Summarizing and prioritizing tasks with AI intelligence.
- Delivering an immersive, screen-based experience—no phone, no distractions.
- Syncing seamlessly with a desktop companion app for setup and history tracking.

> _Say: “Plan my work today” — and instantly get a curated, AI-summarized view of your day._

---

## 🌟 Features

- 📡 **Cloud-Connected AI**: Utilizes cloud infrastructure to offload heavy AI computation.
- 🧠 **Smart Voice Interface**: Responds to natural language commands.
- 🧭 **Task Management**: Prioritizes and displays your day’s agenda with summaries.
- 🖥️ **Desktop Companion App**: For easy setup and reviewing past conversations.
- 🪶 **Minimalist UI**: Designed for quick insights, not distractions.

![UI Preview](Resources/2.png)  
![Desktop App](Resources/3.png)

---

## 🎥 Demo

Watch it in action:  
![](./Resources/Work.mp4)
#### [Direct Link](https://raw.githubusercontent.com/mndxt007/WorkDot/master/resources/work.mp4)

---

## 🔧 For Hackers & DIYers

We built WorkDot using the [**M5Stack Core2 ESP32 IoT Development Kit**](https://shop.m5stack.com/products/m5stack-core2-esp32-iot-development-kit), chosen for its robust feature set in a compact form:

### 🛠️ Hardware Highlights

- ESP32 with built-in Wi-Fi/Bluetooth  
- 16MB Flash, 8MB PSRAM  
- Capacitive touchscreen, speaker, vibration motor  
- Battery + power management  
- TF card slot (up to 16GB)

### 🧑‍💻 Embedded Development Takeaways

Building real-time voice interaction on a 4MB RAM device was no small feat! Some key learnings:

- 🔁 **WebSocket Continuation Frames**  
  - [RFC 6455](https://datatracker.ietf.org/doc/html/rfc6455#section-5.2)  
  - Custom implementation to stream audio using continuation frames  
  - [WebSocketsClient.cpp](./WorkDot.M5Stack/src/WebSockets/WebSockets.cpp)

- ⏱️ **RTOS Task Management & Sync**  
  - Used FreeRTOS tasks, semaphores, and queues for async operations  
  - [Main.cpp](./WorkDot.M5Stack/src/Main.cpp)  
  - [ESP-IDF RTOS Guide](https://docs.espressif.com/projects/esp-idf/en/v4.3/esp32/api-reference/system/freertos.html)

- 🖼️ **UI on Embedded Systems**  
  - Built with [LVGL](https://lvgl.io/)  
  - Designed using [SquareLine Studio](https://squareline.io/) (Figma-compatible)

---

## 🧩 Backend & Services

While the device handles UI and audio capture, the heavy lifting is done in the cloud:

- 🎤 **Speech to Text**: [Azure Speech SDK](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-sdk)
- 🧠 **AI Orchestration**: [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- 📅 **Data Access**: [Microsoft Graph SDK](https://learn.microsoft.com/en-us/graph/sdks/sdks-overview)
- 🌐 **ASP.NET Core WebSocket Backend**
  - [WorkDot.Api](./WorkDot.Api/)
  - [WorkDot.Services](./WorkDot.Services/)
  - [WorkDot.Desktop](./WorkDot.Desktop/) (built with .NET MAUI)

---

## 🌍 Impact

WorkDot demonstrates the remarkable potential of bringing AI to the edge:

- Enables productivity on ultra-lightweight hardware.
- Unlocks hands-free, natural interaction with work data.
- Paves the way for AI-powered wearables and other embedded devices.

---

## 🔮 Future Directions

- Improve voice recognition accuracy and latency.
- Broaden integrations (e.g., Slack, Trello, Jira, Outlook).
- Expand UI customization with smart widgets.
- Enhance enterprise compatibility and data security.

---

## 🛠️ Project Structure

```bash
WorkDot/
├── WorkDot.M5Stack/      # Embedded firmware (ESP32)
├── WorkDot.Api/          # Backend Web API (ASP.NET Core)
├── WorkDot.Services/     # Cloud orchestration & AI logic
├── WorkDot.Desktop/      # Desktop companion app (MAUI)
└── Resources/            # Images, demo videos, assets
