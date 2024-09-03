using AiChatApi.KernelPlugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.SemanticKernel;
using WorkDot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Azure AD authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraphAppOnly(authProvider => new GraphServiceClient(authProvider))
    .AddInMemoryTokenCaches()
    .AddDownstreamApi("GraphApi", builder.Configuration.GetSection("GraphApi"));

builder.Services.AddAzureOpenAIChatCompletion(builder.Configuration["AzureOpenAi:DeploymentName"]!,
    builder.Configuration["AzureOpenAi:Endpoint"]!,
    builder.Configuration["AzureOpenAi:ApiKey"]!);

builder.Services.AddKernel().Plugins.AddFromType<KernelFunctions>("graph_functions");

builder.Services.AddTransient<GraphService>();

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
