using Blazored.LocalStorage;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MudBlazor.Services;
using Plugin.Maui.Audio;
using System.Reflection;
using WorkDot.Services.Common;
using WorkDot.Services.Presentation;
using WorkDot.Services.Utilities;

namespace WorkDot
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .AddAudio();

            builder.Services.AddMauiBlazorWebView();

            #if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
            #endif

            // Build the service provider to access services
            var serviceProvider = builder.Services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            // Load configuration from embedded resource
            ConfigureAppSettings(builder);

            // Register services
            RegisterServices(builder.Services, config);

            return builder.Build();
        }

        private static void ConfigureAppSettings(MauiAppBuilder builder)
        {
            string configFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.appsettings.json";
            var assembly = Assembly.GetExecutingAssembly();
            //refactoring issues. The namespace is WorkDot but the assembly name is WorkDot.Desktop
            var configFileName2 = assembly.GetManifestResourceNames().Where(file => file.Contains("appsettings.json")).FirstOrDefault();
            using var stream = assembly.GetManifestResourceStream(configFileName2!);
            builder.Configuration.AddJsonStream(stream!);
        }

        private static void RegisterServices(IServiceCollection services, IConfiguration config)
        {
            services.AddBlazoredLocalStorage();
            services.AddSingleton(sp =>
            {
                var speechConfig = SpeechConfig.FromSubscription(
                    config["AzureSpeech:SubscriptionKey"]!,
                    config["AzureSpeech:Region"]!);
                speechConfig.SpeechRecognitionLanguage = "en-US";
                return speechConfig;
            });

            services.AddKernel().Plugins.AddFromType<KernelPlugins>("functions");
            services.AddAzureOpenAIChatCompletion(
                     deploymentName: config["OpenAI:Deployment"]!,
                     endpoint: config["OpenAI:Endpoint"]!,
                     apiKey: config["OpenAI:Key"]!);

            services.AddMudServices();
            services.AddBlazoredLocalStorage();
            services.AddScoped<LayoutService>();
            services.AddScoped<ChatCompletionService>();
            services.AddScoped<ClipboardService>();
            services.AddScoped<OutlookService>();;
            services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        }
    }
}
