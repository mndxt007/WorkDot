using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using EmailParameters = Microsoft.Graph.Users.Item.Messages.MessagesRequestBuilder.MessagesRequestBuilderGetQueryParameters;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using System.Web;
using WorkDot.Api.Models;

namespace WorkDot.Api.Services
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

        public async Task<List<EmailDetails>> GetUserEmailsWithRawQueryAsync(string queryParms)
        {
            try
            {
                var baseUrl = "https://graph.microsoft.com/v1.0/users/leeg@MODERNCOMMS884601.onmicrosoft.com/messages";
                var fullUrl = $"{baseUrl}?{queryParms}&$select=bodyPreview,subject,toRecipients,receivedDateTime";

                var requestInformation = new RequestInformation
                {
                    HttpMethod = Method.GET,
                    UrlTemplate = fullUrl
                };

                var response = await _graphClient.RequestAdapter.SendAsync(requestInformation, MessageCollectionResponse.CreateFromDiscriminatorValue);

                if (response?.Value != null)
                {
                    return response.Value.Select(m => new EmailDetails
                    {
                        BodyPreview = m.BodyPreview,
                        Subject = m.Subject,
                        Recipients = m.ToRecipients?.Select(r => r.EmailAddress?.Address).ToList(),
                        ReceivedDateTime = m.ReceivedDateTime?.DateTime ?? DateTime.MinValue
                    }).ToList();
                }

                return new List<EmailDetails>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving emails: {ex.Message}");
                return new List<EmailDetails>();
            }
        }
    }
}
