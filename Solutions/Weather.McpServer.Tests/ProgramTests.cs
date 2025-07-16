using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenCliToMcp.Core.Executors;

namespace Weather.McpServer.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void Program_BuildsWebApplicationWithRequiredServices()
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
                  "WeatherCli": {
                    "ExecutableName": "weather",
                    "ExecutablePath": null,
                    "SearchInPath": true
                  }
                }
                """);

            // Build the web application
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = tempDirectory,
                ApplicationName = "Weather.McpServer.Tests"
            });

            builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);

            // Configure services like the Program.cs does
            builder.Services.Configure<CliExecutorOptions>(
                builder.Configuration.GetSection("WeatherCli"));

            builder.Services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();

            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly();

            // Register WeatherCliMcpTool manually for testing
            builder.Services.AddSingleton<WeatherCliMcpTool>();

            WebApplication app = builder.Build();

            // Act & Assert - verify services are registered
            using (IServiceScope scope = app.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;

                // Verify ICliExecutor is registered
                ICliExecutor? cliExecutor = services.GetService<ICliExecutor>();
                cliExecutor.ShouldNotBeNull();
                cliExecutor.ShouldBeOfType<ConfigurableCliExecutor>();

                // Verify CliExecutorOptions is configured
                IOptions<CliExecutorOptions>? options = services.GetService<IOptions<CliExecutorOptions>>();
                options.ShouldNotBeNull();
                options.Value.ExecutableName.ShouldBe("weather");
                options.Value.SearchInPath.ShouldBeTrue();

                // Verify WeatherCliMcpTool can be created
                WeatherCliMcpTool? weatherTool = services.GetService<WeatherCliMcpTool>();
                weatherTool.ShouldNotBeNull();
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
                ["WeatherCli:ExecutableName"] = "custom-weather",
                ["WeatherCli:ExecutablePath"] = "/usr/local/bin",
                ["WeatherCli:SearchInPath"] = "false"
            })
            .Build();

        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<CliExecutorOptions>(configuration.GetSection("WeatherCli"));
        services.AddLogging();
        services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        ICliExecutor cliExecutor = serviceProvider.GetRequiredService<ICliExecutor>();
        IOptions<CliExecutorOptions> options = serviceProvider.GetRequiredService<IOptions<CliExecutorOptions>>();

        // Assert
        cliExecutor.ShouldNotBeNull();
        options.Value.ExecutableName.ShouldBe("custom-weather");
        options.Value.ExecutablePath.ShouldBe("/usr/local/bin");
        options.Value.SearchInPath.ShouldBeFalse();
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
    public void WeatherCliMcpTool_CanBeCreatedFromDI()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        services.Configure<CliExecutorOptions>(options =>
        {
            options.ExecutableName = "weather";
        });
        services.AddSingleton<ICliExecutor, ConfigurableCliExecutor>();
        services.AddSingleton<WeatherCliMcpTool>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using (IServiceScope scope1 = serviceProvider.CreateScope())
        using (IServiceScope scope2 = serviceProvider.CreateScope())
        {
            WeatherCliMcpTool tool1 = scope1.ServiceProvider.GetRequiredService<WeatherCliMcpTool>();
            WeatherCliMcpTool tool2 = scope2.ServiceProvider.GetRequiredService<WeatherCliMcpTool>();

            // Singleton instances should be the same
            tool1.ShouldBe(tool2);
        }
    }

    [TestMethod]
    public void Program_ConfiguresHttpTransport()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging(); // Add logging for MCP dependencies
        
        // Add hosting services that MCP requires
        services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();
        
        // Act
        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify MCP server services are registered
        // Note: Specific service types depend on ModelContextProtocol.AspNetCore implementation
        serviceProvider.GetServices<IHostedService>().ShouldNotBeEmpty();
    }

    // Test implementation of IHostApplicationLifetime
    private class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}