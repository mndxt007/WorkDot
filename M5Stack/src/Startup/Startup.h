#include "WebSockets\WebSocketsClient.h"

void setupLogging();
bool setupWifiManager(WiFiManager &wm);
void onWebServerStart();
void setupWebsockets(WebSocketsClient webSocket, WebSocketsClient::WebSocketClientEvent webSocketEvent);
bool loadConfigFile();
void saveConfigFile();