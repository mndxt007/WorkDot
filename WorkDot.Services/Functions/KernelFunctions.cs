using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using WorkDot.Services.Models;
using WorkDot.Services.Services;

namespace WorkDot.Services.Functions
{
    public class KernelFunctions
    {
        private readonly IServiceProvider _serviceProvider;

        public KernelFunctions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction("graph_demails")]
        [Description("Fetches/Retrieves/Gets/Shows Emails based on the user input criteria.")]
        public async Task<WidgetModel> RetrieveEmailAsync([Description(Prompts.EmailParamDescription)] string queryParmeter)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _graphService = scope.ServiceProvider.GetRequiredService<GraphService>();
                var _kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
                var messages = await _graphService.GetUserEmailsWithRawQueryAsync(queryParmeter);
                if (messages.Count == 0)
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Data = null
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
                        Data = plans
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
                var tasks = await _graphService.GetUserTasksWithRawQueryAsync(toDoList, queryParams);
                if (tasks.Count == 0)
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Plan,
                        Data = null
                    };
                }
                else
                {
                    return new WidgetModel()
                    {
                        Widget = WidgetType.Todo,
                        Data = tasks
                    };
                }
            }
        }
      
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
