
namespace WorkDot.Models
{
    public class EmailContext
    {
        public string? EntryID { get; set; }
        public string? EmailBody { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? SenderEmail { get; set; }

        public DateTime? RecievedTime { get; set; }

        public string? Subject { get; set; }

        public string? Error { get; set; }
    }
}
