using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace AiChatApi.Controllers
{
    public class SpeechController : Controller
    {
        private readonly IConfiguration _configuration;
        private IHostEnvironment _environment;
        private readonly ILogger _logger;
        public SpeechController(IConfiguration configuration, ILogger<SpeechController> logger, IHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        [Route("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ReceiveAudio(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task ReceiveAudio(WebSocket webSocket)
        {
            try
            {
                while (!webSocket.CloseStatus.HasValue)
                {

                    var buffer = new byte[1024 * 4];

                    using var memoryStream = new MemoryStream();
                    WebSocketReceiveResult result;
                    int count = 0;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        memoryStream.Write(buffer, 0, result.Count);
                        count++;
                    } while (!result.EndOfMessage);

                    var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot\\wavfiles\\", $"audio_{DateTime.Now.Ticks}.wav");

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var headerBytes = new byte[4];
                    await memoryStream.ReadAsync(headerBytes, 0, 4);
                    var header = Encoding.ASCII.GetString(headerBytes);

                    // If not, add a WAV header
                    if (header != "RIFF")
                    {
                        var s = new RawSourceWaveStream(memoryStream, new WaveFormat(16000, 1));
                        WaveFileWriter.CreateWaveFile(filePath, s);
                    }

                    else
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        //Save the audio file for debugging


                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.CopyTo(fileStream);
                        }
                    }
                        
                    

                    var speechText = await ConvertSpeechToText(filePath);
                    var textBuffer = Encoding.UTF8.GetBytes(speechText);
                    await webSocket.SendAsync(new ArraySegment<byte>(textBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                await webSocket.CloseAsync(webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private async Task<string> ConvertSpeechToText(string wavFileInput)
        {
            string subscriptionKey = _configuration["AzureSpeech:SubscriptionKey"]!;
            string region = _configuration["AzureSpeech:Region"]!;
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using var audioConfig = AudioConfig.FromWavFileInput(wavFileInput);
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            try
            {
                // Perform speech recognition
                var result = await speechRecognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    return result.Text;
                }
                else
                {
                    switch (result.Reason)
                    {
                        case ResultReason.RecognizedSpeech:
                            _logger.LogError($"RECOGNIZED: Text={result.Text}");
                            break;
                        case ResultReason.NoMatch:
                            _logger.LogError($"NOMATCH: Speech could not be recognized.");
                            break;
                        case ResultReason.Canceled:
                            var cancellation = CancellationDetails.FromResult(result);
                            _logger.LogError($"CANCELED: Reason={cancellation.Reason}");

                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                                _logger.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                                _logger.LogError($"CANCELED: Did you set the speech resource key and region values?");
                            }
                            break;
                    }
                    return $"Speech recognition failed: {result.Reason}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return "Speech recognition failed due to an exception.";
            }
        }
    }
}