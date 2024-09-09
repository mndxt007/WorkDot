using Microsoft.Office.Interop.Outlook;
using System.Runtime.InteropServices;
using WorkDot.Models;
using Application = Microsoft.Office.Interop.Outlook.Application;
using Exception = System.Exception;
using EmailMessage = WorkDot.Models.EmailMessage;
using Microsoft.SemanticKernel;
using System.ComponentModel;
namespace WorkDot.Services.Common
{
    public class OutlookService
    {
        public Application _outlookApp;
        private NameSpace _namespace;

        public OutlookService()
        {
            _outlookApp = new Application();
            _namespace = _outlookApp.GetNamespace("MAPI");
            _namespace.Logon(null, null, false, false);
        }

        public EmailMessage GetSelectedEmail()
        {
            try
            {
                Explorer explorer = _outlookApp.ActiveExplorer();

                if (explorer.Selection.Count > 0)
                {
                    object selectedItem = explorer.Selection[1];

                    if (selectedItem is MailItem mailItem)
                    {
                        return MapToEmailMessage(mailItem);
                    }
                    else
                    {
                        Console.WriteLine("The selected item is not an email.");
                        return null!;
                    }
                }
                else
                {
                    Console.WriteLine("No item is currently selected.");
                    return null!;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null!;
            }
        }

        public List<AppointmentItem> GetAppointmentsForToday()
        {
            var appointments = new List<AppointmentItem>();
            try
            {
                var calendar = _outlookApp.GetNamespace("MAPI").GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
                var items = calendar.Items;
                items.IncludeRecurrences = true;
                items.Sort("[Start]", Type.Missing);

                foreach (AppointmentItem appt in items)
                {
                    if (appt.Start.Date == DateTime.Now.Date)
                    {
                        appointments.Add(appt);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving today's appointments: {ex.Message}");
            }

            return appointments;
        }

        public void ReplyToEmail(string entryID, string body)
        {
            var mailItem = _outlookApp.Session.GetItemFromID(entryID) as MailItem;
            if (mailItem != null)
            {
                var replyItem = mailItem.Reply();
                replyItem.Body = body;
                replyItem.Display();
            }
        }

        public List<EmailMessage> SearchEmails(string searchTerm, string folderName = "Inbox")
        {
            var folder = _outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            if (folderName != "Inbox")
            {
                folder = folder.Folders[folderName];
            }

            var filter = $"@SQL=\"urn:schemas:httpmail:subject\" like '%{searchTerm}%' OR " +
                         $"\"urn:schemas:httpmail:textdescription\" like '%{searchTerm}%'";

            var items = folder.Items.Restrict(filter);
            return MapToEmailMessages(items.Cast<MailItem>().ToList());
        }

        public List<EmailMessage> GetEmailsFromSender(string senderEmail, string folderName = "Inbox")
        {
            var folder = _outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            if (folderName != "Inbox")
            {
                folder = folder.Folders[folderName];
            }

            var filter = $"@SQL=\"urn:schemas:httpmail:fromemail\" = '{senderEmail}'";
            var items = folder.Items.Restrict(filter);
            return MapToEmailMessages(items.Cast<MailItem>().ToList());
        }

        public void CreateDraft(string subject, string body, string toRecipients)
        {
            var mailItem = _outlookApp.CreateItem(OlItemType.olMailItem) as MailItem;
            mailItem!.Subject = subject;
            mailItem.Body = body;
            mailItem.To = toRecipients;
            mailItem.Save();
        }

        public void SendEmail(string subject, string body, string toRecipients)
        {
            var mailItem = _outlookApp.CreateItem(OlItemType.olMailItem) as MailItem;
            mailItem!.Subject = subject;
            mailItem.Body = body;
            mailItem.To = toRecipients;
            mailItem.Send();
        }

        public List<string> GetFolderNames()
        {
            var inbox = _outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            return inbox.Folders.Cast<Folder>().Select(f => f.Name).ToList();
        }

        public void MoveEmailToFolder(string entryID, string folderName)
        {
            var mailItem = _outlookApp.Session.GetItemFromID(entryID) as MailItem;
            var inbox = _outlookApp.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            var targetFolder = inbox.Folders[folderName];
            mailItem?.Move(targetFolder);
        }

        public static EmailMessage MapToEmailMessage(MailItem mailItem)
        {
            if (mailItem == null)
                return null!;

            var email = new EmailMessage
            {
                EntryID = mailItem.EntryID,
                Subject = mailItem.Subject,
                Body = mailItem.Body,
                ReceivedTime = mailItem.ReceivedTime,
                SenderName = mailItem.SenderName,
                SenderEmailAddress = mailItem.SenderEmailAddress,
                IsRead = !mailItem.UnRead
            };

            // Map recipients
            if (mailItem.Recipients != null)
            {
                foreach (Recipient recipient in mailItem.Recipients)
                {
                    switch (recipient.Type)
                    {
                        case (int)OlMailRecipientType.olTo:
                            email.To.Add(recipient.Address);
                            break;
                        case (int)OlMailRecipientType.olCC:
                            email.CC.Add(recipient.Address);
                            break;
                        case (int)OlMailRecipientType.olBCC:
                            email.BCC.Add(recipient.Address);
                            break;
                    }
                }
            }

            // Map attachments
            if (mailItem.Attachments != null)
            {
                email.Attachments = mailItem.Attachments.Cast<Attachment>()
                    .Select(a => new AttachmentInfo
                    {
                        FileName = a.FileName,
                        Size = a.Size
                    })
                    .ToList();
            }

            return email;
        }

        public static List<EmailMessage> MapToEmailMessages(IEnumerable<MailItem> mailItems)
        {
            return mailItems.Select(MapToEmailMessage).ToList();
        }

        public void Dispose()
        {
            _namespace.Logoff();
            _outlookApp.Quit();
            Marshal.ReleaseComObject(_namespace);
            Marshal.ReleaseComObject(_outlookApp);
        }
    }
}