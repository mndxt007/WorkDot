using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AiChatApi.KernelPlugins
{
    public class SKFunctions
    {
        [KernelFunction("graph_call")]
        [Description("Fetches/Retrieves my Emails on the current time and graph query and returns list of emails")]
         public async Task<string[]> MakeGraphCallAsync([Description("query to call graph")] string graphEndpoint)
        {
            // Implement the logic to make the Graph API call here
            // For example, you can use HttpClient to make the request

           Console.WriteLine("Making Graph API call to: " + graphEndpoint);

            return  ["""
                From: Manoj Dixit 
                Sent: 26 August 2024 23:51
                To: leegu@contoso.com
                Subject: RE: [EXTERNAL] RE: We've noticed several alerts... - TrackingID#2404090030005006

                Hello Lee,

                I hope you are doing well.

                I am following up to understand if you managed to collect in an occurrence of the issue. For any queries please reach. 

                I'm wondering if we can temporarily archive the case until further investigation is needed. Please rest assured that your satisfaction and the resolution of your issue remain our top priorities. By temporarily archiving the case, we can allocate our support resources more effectively and pause any unnecessary follow-ups until necessary. However, I want to emphasize that we will always be ready and more than willing to resume work on the case whenever you are ready.

                If you decide to proceed with the temporary archiving of your case, you can reopen this case within certain months or open a ticket referring this case. Simply send me an email with your case number whenever you're prepared to revisit the matter, and I will be more than happy to reopen it for you promptly.

                Please let me know your thoughts on this. We would appreciate your understanding and co-operation in this matter.
                """];
        }

        [KernelFunction("get_time")]
        [Description("Gets the current datetime for Graph call")]
        public async Task<DateTime> GetCurrentDateTime()
        {

            return System.DateTime.Now;
        }
    }
}
