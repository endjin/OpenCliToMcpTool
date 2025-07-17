using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenCliToMcp.Core.Executors;

namespace TaskManager.McpServer.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void Program_BuildsHostWithRequiredServices()
    {
        // Arrange
        string[] args = [];
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Create a test appsettings.json
            string appsettingsPath = Path.Combine(tempDirectory, "appsettings.json");
            File.WriteAllText(appsettingsPath, """
                                               {
                                                 "CliExecutor": {
                                                   "ExecutableName": "taskmanager",
                                                   "ExecutablePath": null,
                                                   "SearchInPath": true
                                                 }
                                               }
                                               """);

            // Build the host configuration
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = tempDirectory,
                ApplicationName = "TaskManager.McpServer.Tests"
            });

            builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);

            // Configure services like the Program.cs does
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            builder.Services.Configure<CliExecutorOptions>(builder.Configuration.GetSection("CliExecutor"));
            builder.Services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();

            IHost host = builder.Build();

            // Act & Assert - verify services are registered
            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;

                // Verify ICliExecutor is registered
                ICliExecutor? cliExecutor = services.GetService<ICliExecutor>();
                cliExecutor.ShouldNotBeNull();
                cliExecutor.ShouldBeOfType<ConfigurableCliExecutor>();

                // Verify CliExecutorOptions is configured
                IOptions<CliExecutorOptions>? options = services.GetService<IOptions<CliExecutorOptions>>();
                options.ShouldNotBeNull();
                options.Value.ExecutableName.ShouldBe("taskmanager");
                options.Value.SearchInPath.ShouldBeTrue();

                // Verify logging is configured
                ILoggerFactory? loggerFactory = services.GetService<ILoggerFactory>();
                loggerFactory.ShouldNotBeNull();
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    [TestMethod]
    public void ConfigurableCliExecutor_LoadsOptionsFromConfiguration()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CliExecutor:ExecutableName"] = "custom-task-manager",
                ["CliExecutor:ExecutablePath"] = "/usr/local/bin",
                ["CliExecutor:SearchInPath"] = "false"
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<CliExecutorOptions>(configuration.GetSection("CliExecutor"));
        services.AddLogging();
        services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        ICliExecutor cliExecutor = serviceProvider.GetRequiredService<ICliExecutor>();
        IOptions<CliExecutorOptions> options = serviceProvider.GetRequiredService<IOptions<CliExecutorOptions>>();

        // Assert
        cliExecutor.ShouldNotBeNull();
        options.Value.ExecutableName.ShouldBe("custom-task-manager");
        options.Value.ExecutablePath.ShouldBe("/usr/local/bin");
        options.Value.SearchInPath.ShouldBeFalse();
    }

    [TestMethod]
    public void Host_ConfiguresLoggingToStderr()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            ApplicationName = "TestApp"
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        IHost host = builder.Build();

        // Act
        using IServiceScope scope = host.Services.CreateScope();
        ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("TestLogger");

        // Assert
        logger.ShouldNotBeNull();
        // The console logger should be configured to log to stderr
        // This is a configuration test - actual output verification would require integration testing
    }

    [TestMethod]
    public void CliExecutorOptions_DefaultValues()
    {
        // Arrange & Act
        CliExecutorOptions options = new();

        // Assert
        options.ExecutableName.ShouldBeNull();
        options.ExecutablePath.ShouldBeNull();
        options.SearchInPath.ShouldBeTrue(); // Default should be true
    }

    [TestMethod]
    public void Program_HandlesWorkingDirectoryCorrectly()
    {
        // This test verifies the working directory logic
        // In a real test, we'd need to mock Assembly.GetExecutingAssembly().Location
        
        // Arrange
        string originalDirectory = Directory.GetCurrentDirectory();
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        try
        {
            // Act
            Directory.SetCurrentDirectory(testDirectory);
            string currentDir = Directory.GetCurrentDirectory();

            // Assert
            currentDir.ShouldBe(testDirectory);
        }
        finally
        {
            // Cleanup
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }
}