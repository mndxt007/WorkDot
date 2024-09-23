using System.Text.Json.Serialization;
using WorkDot.Api.Models;

namespace WorkDot.Services.Models
{
    public class PlanModel
    {
        public EmailDetails Message { get; set; } = default!;
        public string Action { get; set; } = "Demo_Action";
        public string Response { get; set; } = "Take Action on the email";
        public string Sentiment { get; set; } = "Default Sentiment";
        public int Priority { get; set; } = 5;
        [JsonPropertyName("conversationId")]
        public string ConversationId { get; set; }

    }

    public class Actions
    {

        public string? Category1 { get; set; }
        public string? Category2 { get; set; }
        public string? Category3 { get; set; }
        public string? Category4 { get; set; }
        public string? Folder { get; set; }


        public Actions()
        {
            Category1 = "Follow-up";
            Category2 = "Attention-Needed";
            Category3 = "Acknowledgement";
            Category4 = "Case-Closure";
            Folder = "IIS Discussions";
        }
    }
}