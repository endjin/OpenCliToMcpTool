using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenCliToMcp.Core;
using OpenCliToMcp.Core.Executors;

// Set working directory to the executable's directory
// This ensures relative paths in configuration work correctly
string? exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (!string.IsNullOrEmpty(exeDirectory))
{
    Directory.SetCurrentDirectory(exeDirectory);
}

// Create builder with explicit configuration
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = exeDirectory ?? Directory.GetCurrentDirectory(),
    ApplicationName = "TaskManager.McpServer"
});

// Explicitly add appsettings.json from the executable directory
if (!string.IsNullOrEmpty(exeDirectory))
{
    var appsettingsPath = Path.Combine(exeDirectory, "appsettings.json");
    if (File.Exists(appsettingsPath))
    {
        builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);
        Console.Error.WriteLine($"Loaded configuration from: {appsettingsPath}");
    }
    else
    {
        Console.Error.WriteLine($"WARNING: appsettings.json not found at: {appsettingsPath}");
    }
}

// Configure logging to use stderr for STDIO transport
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Configure CLI executor options from appsettings.json
builder.Services.Configure<CliExecutorOptions>(builder.Configuration.GetSection("CliExecutor"));

// Add logging to debug configuration
builder.Services.AddSingleton<ICliExecutor>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<ConfigurableCliExecutor>>();
    var options = serviceProvider.GetRequiredService<IOptions<CliExecutorOptions>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    
    // Debug: log all configuration values
    var cliExecutorSection = configuration.GetSection("CliExecutor");
    logger.LogInformation($"CliExecutor section exists: {cliExecutorSection.Exists()}");
    logger.LogInformation($"ExecutableName from config: '{options.Value.ExecutableName}'");
    logger.LogInformation($"ExecutablePath from config: '{options.Value.ExecutablePath}'");
    logger.LogInformation($"SearchInPath from config: {options.Value.SearchInPath}");
    
    // Log all keys in the configuration
    foreach (var kvp in configuration.AsEnumerable())
    {
        if (kvp.Key.StartsWith("CliExecutor"))
        {
            logger.LogDebug($"Config key: {kvp.Key} = {kvp.Value}");
        }
    }
    
    return new ConfigurableCliExecutor(logger, options);
});

IHost host = builder.Build();

Console.Error.WriteLine($"Task Manager MCP Server started. Ready to receive commands.");
Console.Error.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
Console.Error.WriteLine("This server exposes the Task Manager CLI as MCP tools.");

await host.RunAsync();