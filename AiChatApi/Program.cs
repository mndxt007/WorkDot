using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using AiChatApi.KernelPlugins;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAzureOpenAIChatCompletion(builder.Configuration["AzureOpenAi:DeploymentName"]!,
    builder.Configuration["AzureOpenAi:Endpoint"]!,
    builder.Configuration["AzureOpenAi:ApiKey"]!);

var kernel = builder.Services.AddKernel();
kernel.Plugins.AddFromPromptDirectory(Path.Combine(Environment.CurrentDirectory, "Kernel\\Plugins"));
kernel.Plugins.AddFromType<SKFunctions>("retrieve_emails");
kernel.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

var app = builder.Build();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseWebSockets(webSocketOptions);

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
