using OpenCliToMcp.Core;
using OpenCliToMcp.Core.Executors;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure CLI executor options from appsettings.json
builder.Services.Configure<CliExecutorOptions>(
    builder.Configuration.GetSection("WeatherCli"));

builder.Services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

WebApplication app = builder.Build();

app.MapMcp();

app.Run();