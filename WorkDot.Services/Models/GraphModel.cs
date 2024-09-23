using System.Text.Json.Serialization;

namespace WorkDot.Services.Models
{
    public class EmailItem
    {
        public string BodyPreview { get; set; } = default!;
        public string From { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public List<string> Recipients { get; set; } = default!;
        public DateTime ReceivedDateTime { get; set; }
        [JsonPropertyName("conversationId")]
        public string ConverstionId { get; set; } = default!;
    }

    public class TodoItem
    {
        public string Title { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime DueDateTime { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
    }
}
