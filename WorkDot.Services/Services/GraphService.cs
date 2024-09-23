using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using WorkDot.Services.Models;

namespace WorkDot.Services.Services
{
    public partial class GraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<GraphService> _logger;

        public GraphService(GraphServiceClient graphClient, ILogger<GraphService> logger)
        {
            _graphClient = graphClient;
            _logger = logger;
        }

        public async Task<List<EmailItem>> GetUserEmailsWithRawQueryAsync(string queryParms)
        {
            //https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0#properties
            try
            {
                var baseUrl = "https://graph.microsoft.com/v1.0/users/leeg@M365x50769524.onmicrosoft.com/messages";
                var fullUrl = $"{baseUrl}?{queryParms}&$select=bodyPreview,subject,toRecipients,receivedDateTime,conversationId";

                var requestInformation = new RequestInformation
                {
                    HttpMethod = Method.GET,
                    UrlTemplate = fullUrl,
                };

                var response = await _graphClient.RequestAdapter.SendAsync(requestInformation, MessageCollectionResponse.CreateFromDiscriminatorValue);

                if (response?.Value != null)
                {
                    return response.Value.Select(m => new EmailItem
                    {
                        BodyPreview = m.BodyPreview,
                        Subject = m.Subject,
                        Recipients = m.ToRecipients?.Select(r => r.EmailAddress?.Address).ToList(),
                        ReceivedDateTime = m.ReceivedDateTime?.DateTime ?? DateTime.MinValue,
                        ConverstionId = m.ConversationId
                    }).ToList();
                }

                return new List<EmailItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving emails: {ex.Message}");
                return new List<EmailItem>();
            }
        }

        public async Task<List<TodoItem>> GetUserTasksWithRawQueryAsync(string toDoList, string queryParms)
        {
            //https://learn.microsoft.com/en-us/graph/api/resources/todotask?view=graph-rest-1.0

            try
            {
                var baseUrl = "https://graph.microsoft.com/v1.0/users/leeg@M365x50769524.onmicrosoft.com/todo/lists";
                toDoList = toDoList != null ? toDoList : "tasks";
                var fullUrl = $"{baseUrl}/{toDoList}/tasks?{queryParms}";//&$select=title,status,body,id,dueDateTime";
                var requestInformation = new RequestInformation
                {
                    HttpMethod = Method.GET,
                    UrlTemplate = fullUrl
                };
                var response = await _graphClient.RequestAdapter.SendAsync(requestInformation, TodoTaskCollectionResponse.CreateFromDiscriminatorValue);

                if (response?.Value != null)
                {
                    return response.Value.Select(m => new TodoItem
                    {
                        Title = m.Title,
                        //Body = m.Body,
                        Status = m.Status.ToString(),
                        DueDateTime = m.DueDateTime != null ? m.DueDateTime.ToDateTime() : DateTime.MaxValue,
                        Id = m.Id
                    }).ToList();
                }
                return new List<TodoItem>();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving tasks: {ex.Message}");
                return new List<TodoItem>();
            }
        }
    }
}
