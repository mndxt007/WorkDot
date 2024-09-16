using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

        //[KernelFunction("graph_datetime")]
        //[Description("Retrieves the current datetime in UTC used for Graph queryparamters")]
        //public DateTimeOffset GetCurrentUtcTime()
        //   => DateTime.UtcNow;

        [KernelFunction("graph_demails")]
        [Description("Fetches/Retrieves/Gets/Shows Emails based on the user input criteria.")]
        public async Task<WidgetModel> RetrieveEmailAsync([Description(Prompts.EmailParamDescription)] string queryParmeter)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _graphService = scope.ServiceProvider.GetRequiredService<GraphService>();
                var _kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
                var messages = await _graphService.GetUserEmailsWithRawQueryAsync(queryParmeter);
                if(messages.Count == 0)
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Payload = null
                    };
                }
                else
                {
                    var argument = new KernelArguments()
                    {
                        ["actions"] = JsonSerializer.Serialize(new Actions()),
                        ["messages"] = JsonSerializer.Serialize(messages)
                    };
                    var result = await _kernel.InvokeAsync<string>("plugins", "email_summary", argument);
                    var plans = JsonSerializer.Deserialize<List<PlanModel>>(result!)!;
                    plans.ForEach(plan =>
                    plan.Message = messages.Find(message => message.ConverstionId == plan.ConversationId)!);

                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Payload = plans
                    };
                } 
            }
        }

        [KernelFunction("graph_dtodo")]
        [Description("Fetches/Retrieves/Gets/Shows To-Do tasks based on the user input criteria.")]
        public async Task<WidgetModel> RetrieveToDoAsync([Description(Prompts.ToDoListParamDescription)] string toDoList, [Description(Prompts.ToDoParamDescription)] string queryParams)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _graphService = scope.ServiceProvider.GetRequiredService<GraphService>();
                var _kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
                var tasks = await _graphService.GetUserTasksWithRawQueryAsync(toDoList,queryParams);
                if (tasks.Count == 0)
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Payload = null
                    };
                }
                else
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Payload = tasks
                    };
                }
            }
        }

#pragma warning disable SKEXP0001
        public class FunctionCallsFilter() : IAutoFunctionInvocationFilter
        {
            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
            {

                await next(context);
                if (context.Function.Name.StartsWith("graph_d"))
                {
                    context.Terminate = true;
                }
            }
        }


    }
}
