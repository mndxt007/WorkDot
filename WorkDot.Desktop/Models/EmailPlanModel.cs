namespace WorkDot.Models
{
    public class EmailPluginModel
    {
        public EmailMessage Message { get; set; } = default!;
        public string Action { get; set; } = "Demo Action";
        public string Response { get; set; } = "Take Action on the email";
        public string Sentiment { get; set; } = "Default Sentiment";
        public int Priority { get; set; } = 5;
    }
}