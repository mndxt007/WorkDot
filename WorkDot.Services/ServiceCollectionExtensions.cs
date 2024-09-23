using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.SemanticKernel;
using WorkDot.Services.Functions;
using WorkDot.Services.Services;
using static WorkDot.Services.Functions.KernelFunctions;

namespace WorkDot.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkDotService(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Azure AD authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddMicrosoftGraphAppOnly(authProvider => new GraphServiceClient(authProvider))
                .AddInMemoryTokenCaches()
                .AddDownstreamApi("GraphApi", configuration.GetSection("GraphApi"));

            services.AddAzureOpenAIChatCompletion(configuration["AzureOpenAi:DeploymentName"]!,
                configuration["AzureOpenAi:Endpoint"]!,
                configuration["AzureOpenAi:ApiKey"]!);

            var kernel = services.AddKernel();
            kernel.Plugins.AddFromType<KernelFunctions>("graph_functions");
            kernel.Plugins.AddFromPromptDirectory(Path.Combine(AppContext.BaseDirectory, "Plugins"));
            kernel.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

            services.AddSingleton<IAutoFunctionInvocationFilter>(new FunctionCallsFilter());
            services.AddTransient<GraphService>();

            return services;
        }
    }
}