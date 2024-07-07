var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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
app.MapPost("/i2s_samples", async (HttpContext context, IHostEnvironment _environment, ILogger<Program> logger) =>
{
    var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot\\rawfiles", "audio_i2s.raw");
    await using (var fileStream = new FileStream(filePath, FileMode.Append))
    {
        await context.Request.Body.CopyToAsync(fileStream);
    }
    logger.LogInformation($"Received I2S bytes and appended to {filePath}");
    await context.Response.WriteAsync("OK");
});
app.MapControllers();

app.Run();
