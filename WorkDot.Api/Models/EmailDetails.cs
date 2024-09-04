using System.Text.Json.Serialization;

namespace WorkDot.Api.Models
{
    public class EmailDetails
    {
        public string BodyPreview { get; set; }
        public string Subject { get; set; }
        public List<string> Recipients { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        [JsonPropertyName("conversationId")]
        public string ConverstionId { get; set; }
    }
}
