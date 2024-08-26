using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AiChatApi.KernelPlugins
{
    public class SKFunctions
    {
        [KernelFunction("graph_call")]
        [Description("Makes call Graph based on Graph URL generated")]
         public async Task<string> MakeGraphCallAsync(string graphEndpoint)
        {
            // Implement the logic to make the Graph API call here
            // For example, you can use HttpClient to make the request

           Console.WriteLine("Making Graph API call to: " + graphEndpoint);
            return "Sucess";
        }
       
    }
}
