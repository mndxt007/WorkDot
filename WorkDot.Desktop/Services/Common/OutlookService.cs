using Microsoft.Office.Interop.Outlook;
using System.Runtime.InteropServices;
using WorkDot.Models;
using Application = Microsoft.Office.Interop.Outlook.Application;
namespace WorkDot.Services.Common
{
    public class OutlookService
    {
        public Application _outlookApp;
        private NameSpace _namespace;
        Selection _selection;
        public Explorer _explorer;
        MailItem mailItem;

        public OutlookService()
        {
            _outlookApp = new Application();
            _namespace = _outlookApp.GetNamespace("MAPI");
            _namespace.Logon(null, null, false, false);
        }

        public EmailContext GetEmailDataAsync()
        {
            var emailContext = new EmailContext();
            try
            {
                _explorer = _outlookApp.ActiveExplorer();

                //explorer.SelectionChange += Explorer_SelectionChange;
                _selection = _explorer.Selection;
                if (_selection.Count > 0)
                {
                    mailItem = (MailItem)_selection[1];

                }
                if (mailItem != null)
                {
                    // print the subject of the email
                    emailContext.EntryID = mailItem.EntryID;
                    emailContext.EmailBody = mailItem.Body;
                    emailContext.RecievedTime = mailItem.ReceivedTime;
                    emailContext.Subject = mailItem.Subject;
                    emailContext.UserName = mailItem.UserProperties.Session.CurrentUser.Name;
                    emailContext.SenderEmail = mailItem.SenderName;
                    AddressEntry currentUserAddressEntry = mailItem.UserProperties.Session.CurrentUser.AddressEntry;

                    if (currentUserAddressEntry.Type == "EX")
                    {
                        // This is an Exchange user. Use the ExchangeUser object to get the SMTP address.
                        ExchangeUser exchangeUser = currentUserAddressEntry.GetExchangeUser();
                        if (exchangeUser != null)
                        {
                            emailContext.UserEmail = exchangeUser.PrimarySmtpAddress;
                        }
                    }
                    else
                    {
                        // This is not an Exchange user. Just use the address.
                        emailContext.UserEmail = currentUserAddressEntry.Address;
                    }


                    return emailContext;

                }

                else
                {
                    emailContext.Error = "No email found";
                    return emailContext;
                }
            }
            catch (System.Exception ex)
            {
                emailContext.Error = "No email found";
                return emailContext;
            }
            //return await _jsRuntime.InvokeAsync<string>("getEmailData", includeFullConversation);


        }

        public EmailContext GetEmailDataAsync(MailItem mailItem)
        {
            var emailContext = new EmailContext();
            try
            {

                if (mailItem != null)
                {
                    // print the subject of the email
                    emailContext.EntryID = mailItem.EntryID;
                    emailContext.EmailBody = mailItem.Body;
                    emailContext.RecievedTime = mailItem.ReceivedTime;
                    emailContext.Subject = mailItem.Subject;
                    emailContext.UserName = mailItem.UserProperties.Session.CurrentUser.Name;
                    emailContext.SenderEmail = mailItem.SenderName;
                    AddressEntry currentUserAddressEntry = mailItem.UserProperties.Session.CurrentUser.AddressEntry;

                    if (currentUserAddressEntry.Type == "EX")
                    {
                        // This is an Exchange user. Use the ExchangeUser object to get the SMTP address.
                        ExchangeUser exchangeUser = currentUserAddressEntry.GetExchangeUser();
                        if (exchangeUser != null)
                        {
                            emailContext.UserEmail = exchangeUser.PrimarySmtpAddress;
                        }
                    }
                    else
                    {
                        // This is not an Exchange user. Just use the address.
                        emailContext.UserEmail = currentUserAddressEntry.Address;
                    }


                    return emailContext;

                }

                else
                {
                    emailContext.Error = "No email found";
                    return emailContext;
                }
            }
            catch (System.Exception ex)
            {
                emailContext.Error = "No email found";
                return emailContext;
            }
            //return await _jsRuntime.InvokeAsync<string>("getEmailData", includeFullConversation);


        }


        public async Task<string> GetUserAsync()
        {
            var currentUser = _outlookApp.Session.CurrentUser;

            // Return the user's email
            return currentUser.AddressEntry.Address;
        }

        //public async Task<string> Html2TextAsync(string html, bool includeFullConversation)
        //{
        //    // return await _jsRuntime.InvokeAsync<string>("html2text", html, includeFullConversation);
        //}

        public void ReplyAll(string entryID, string chatGPTResponse)
        {
            MailItem mailItem = _outlookApp.GetNamespace("MAPI").GetItemFromID(entryID) as MailItem;
            var reply = mailItem.ReplyAll();

            // Set the body of the reply

            // Set the body of the reply and add the signature
            //reply.HTMLBody = chatGPTResponse + "\n\n" + reply.Body;
            reply.Display();
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