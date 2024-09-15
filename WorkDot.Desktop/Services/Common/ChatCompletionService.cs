using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WorkDot.Services.Common
{
    public class ChatCompletionService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IConfiguration _configuration;

        private readonly string _systemPrompt;
        private readonly ChatHistory _chatHistory;

        public ChatCompletionService(IConfiguration configuration, Kernel kernel, IChatCompletionService chatCompletionService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));

            _systemPrompt = _configuration.GetValue<string>("Prompts:SystemPrompt")
                           ?? throw new InvalidOperationException("SystemPrompt is not configured.");

            _chatHistory = new ChatHistory();
            _chatHistory.AddSystemMessage(_systemPrompt);
        }

        public async Task<string> GetResponseAsync(string input, bool preserveHistory = false)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or whitespace.", nameof(input));

            if (!preserveHistory)
            {
                _chatHistory.Clear();
                _chatHistory.AddSystemMessage(_systemPrompt);
            }

            _chatHistory.AddUserMessage(input);

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatMessage = await _chatCompletionService.GetChatMessageContentAsync(_chatHistory, executionSettings, _kernel);
            var response = chatMessage?.Content ?? throw new InvalidOperationException("No response received from the AI.");

            _chatHistory.AddAssistantMessage(response);

            return response;
        }
    }
}