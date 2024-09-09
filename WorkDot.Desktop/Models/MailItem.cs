
namespace WorkDot.Models
{
    public class EmailMessage
    {
        public string EntryID { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime ReceivedTime { get; set; }
        public string SenderName { get; set; }
        public string SenderEmailAddress { get; set; }
        public List<string> To { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
        public bool IsRead { get; set; }
        public List<AttachmentInfo> Attachments { get; set; }

        public EmailMessage()
        {
            To = new List<string>();
            CC = new List<string>();
            BCC = new List<string>();
            Attachments = new List<AttachmentInfo>();
        }
    }

    public class AttachmentInfo
    {
        public string FileName { get; set; }
        public long Size { get; set; }
    }
}
