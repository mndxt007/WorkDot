using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NAudio.Wave;
using System;
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
        private readonly IChatCompletionService _chatCompletionService;
        private ChatHistory chatHistory = [];

        public SpeechController(IConfiguration configuration, ILogger<SpeechController> logger, IHostEnvironment environment, IChatCompletionService chatCompletionService)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _chatCompletionService = chatCompletionService;
            chatHistory.AddSystemMessage(_configuration["SystemPrompt"]!);
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
                        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        memoryStream.CopyTo(fileStream);
                    }

                    var speechRecognitionResult = await ConvertSpeechToText(filePath);
                    if (speechRecognitionResult != null && speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
                    {
                        var textBuffer = Encoding.UTF8.GetBytes($"\nYou : {speechRecognitionResult.Text} \nAI :");
                        await webSocket.SendAsync(new ArraySegment<byte>(textBuffer), WebSocketMessageType.Text, true, CancellationToken.None);

                        var completion = GetChatCompletion(speechRecognitionResult.Text);
                        await foreach (var response in completion)
                        {
                            await webSocket.SendAsync(response.ToByteArray(), WebSocketMessageType.Text, true, CancellationToken.None);
                            await Task.Delay(100);
                        }
                    }
                    else
                    {
                        var textBuffer = Encoding.UTF8.GetBytes("Sorry, I couldn't recognize your speech. Please try again!");
                        await webSocket.SendAsync(new ArraySegment<byte>(textBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }

                await webSocket.CloseAsync(webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private IAsyncEnumerable<StreamingChatMessageContent> GetChatCompletion(string speechText)
        {
            chatHistory.AddUserMessage(speechText);
            return _chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory);
        }

        private async Task<SpeechRecognitionResult> ConvertSpeechToText(string wavFileInput)
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
                    return result;
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
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return default!;
            }
        }
    }
}