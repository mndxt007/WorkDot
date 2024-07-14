using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NAudio.Wave;
using System.Net.WebSockets;
using System.Text;

namespace AiChatApi.Controllers
{
    public class SpeechController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SpeechController> _logger;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ChatHistory _chatHistory;

        public SpeechController(
            IConfiguration configuration,
            ILogger<SpeechController> logger,
            IHostEnvironment environment,
            IChatCompletionService chatCompletionService)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _chatCompletionService = chatCompletionService;
            _chatHistory = [];
            _chatHistory.AddSystemMessage(_configuration["SystemPrompt"]!);
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

        #region WebSocket Methods
        
        private async Task ReceiveAudio(WebSocket webSocket)
        {
            try
            {
                while (!webSocket.CloseStatus.HasValue)
                {
                    var buffer = new byte[1024 * 4];
                    using var memoryStream = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        memoryStream.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot/wavfiles/", $"audio_{DateTime.Now.Ticks}.wav");
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    if (!IsWavFile(memoryStream))
                    {
                        var waveStream = new RawSourceWaveStream(memoryStream, new WaveFormat(16000, 1));
                        WaveFileWriter.CreateWaveFile(filePath, waveStream);
                    }
                    else
                    {
                        SaveFile(memoryStream, filePath);
                    }

                    var speechRecognitionResult = await ConvertSpeechToText(filePath);

                    if (speechRecognitionResult != null && speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
                    {
                        await SendRecognizedResponse(webSocket, speechRecognitionResult.Text);
                    }
                    else
                    {
                        await SendUnrecognizedResponse(webSocket);
                    }
                }

                await webSocket.CloseAsync(webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private async Task SendRecognizedResponse(WebSocket webSocket, string recognizedText)
        {
            var userTextBuffer = Encoding.UTF8.GetBytes($"\nYou: {recognizedText}\nAI:");
            await webSocket.SendAsync(new ArraySegment<byte>(userTextBuffer), WebSocketMessageType.Text, true, CancellationToken.None);

            var completion = GetChatCompletion(recognizedText);
            var response = new StringBuilder();
            await foreach (var item in completion)
            {
                await webSocket.SendAsync(item.ToByteArray(), WebSocketMessageType.Text, true, CancellationToken.None);
                response.Append(item.Content);
                await Task.Delay(100);
            }

            _chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response.ToString()));
        }

        private async Task SendUnrecognizedResponse(WebSocket webSocket)
        {
            var textBuffer = Encoding.UTF8.GetBytes("Sorry, I couldn't recognize your speech. Please try again!");
            await webSocket.SendAsync(new ArraySegment<byte>(textBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        #endregion
        
        #region Chat Completion Methods

        private IAsyncEnumerable<StreamingChatMessageContent> GetChatCompletion(string speechText)
        {
            _chatHistory.AddUserMessage(speechText);
            return _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory);
        }
        
        #endregion

        #region Speech Recognition Methods

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
                var result = await speechRecognizer.RecognizeOnceAsync();
                LogRecognitionResult(result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return default!;
            }
        }
        private void LogRecognitionResult(SpeechRecognitionResult result)
        {
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                _logger.LogInformation($"RECOGNIZED: Text={result.Text}");
            }
            else
            {
                switch (result.Reason)
                {
                    case ResultReason.NoMatch:
                        _logger.LogWarning("NOMATCH: Speech could not be recognized.");
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        _logger.LogError($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            _logger.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        }
                        break;
                }
            }
        }
       
        #endregion
       
        #region Utility Methods

        private bool IsWavFile(MemoryStream memoryStream)
        {
            var headerBytes = new byte[4];
            memoryStream.Read(headerBytes, 0, 4);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var header = Encoding.ASCII.GetString(headerBytes);
            return header == "RIFF";
        }

        private void SaveFile(MemoryStream memoryStream, string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            memoryStream.CopyTo(fileStream);
        }

        #endregion
    }
}