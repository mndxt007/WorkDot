using Microsoft.SemanticKernel;
using System.ComponentModel;
using WorkDot.Api.Models;
using WorkDot.Api.Services;

namespace AiChatApi.KernelPlugins
{
    public class KernelFunctions
    {
        private readonly IServiceProvider _serviceProvider;

        public KernelFunctions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction("graph_call")]
        [Description("Fetches/Retrieves/Gets/Shows Emails based on the user input criteria")]
        public async Task<List<EmailDetails>> RetrieveEmailAsync([Description("A Microsoft Graph API query string for retrieving emails based on the user input, following the format $top=[number]&$orderby=receivedDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified.")] string queryParmeter)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _graphService = scope.ServiceProvider.GetRequiredService<GraphService>();
                var messages = await _graphService.GetUserEmailsWithRawQueryAsync(queryParmeter);
                return messages;
            }
        }

        [KernelFunction("get_time")]
        [Description("Gets the current datetime for Graph call")]
        public DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }
#pragma warning disable SKEXP0001
        public class FunctionCallsFilter() : IAutoFunctionInvocationFilter
        {
            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
            {

                await next(context);
                var result = context.Result;
                if (context.Function.Name == "graph_call")
                {
                    context.Terminate = true;
                }
            }
        }
    }
}
