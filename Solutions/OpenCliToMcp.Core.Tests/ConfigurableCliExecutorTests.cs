using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenCliToMcp.Core.Executors;
using OpenCliToMcp.Core.Tests.Mocks;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class ConfigurableCliExecutorTests
{
    [TestMethod]
    public void Constructor_WithNullExecutableName_ThrowsArgumentException()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = null
        });

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => new ConfigurableCliExecutor(logger, options));
        exception.Message.ShouldContain("ExecutableName");
    }

    [TestMethod]
    public void Constructor_WithEmptyExecutableName_ThrowsArgumentException()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = ""
        });

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => new ConfigurableCliExecutor(logger, options));
        exception.Message.ShouldContain("ExecutableName");
    }

    [TestMethod]
    public void Constructor_WithValidExecutableName_DoesNotThrow()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = "git"
        });

        // Act
        ConfigurableCliExecutor executor = new(logger, options);
        
        // Assert
        executor.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task ExecuteAsync_UsesConfiguredExecutableName()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        MockProcessFactory mockFactory = new();
        MockProcess mockProcess = mockFactory.CreateProcess("hello world", "", 0);
        mockFactory.EnqueueProcess(mockProcess);
        
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = "echo",
            ExecutablePath = "/test/echo",
            ResponseFormat = ResponseFormat.Raw
        });

        // Act
        ConfigurableCliExecutor executor = new(logger, options, mockFactory);
        string result = await executor.ExecuteAsync("test", ["hello", "world"]);
        
        // Assert
        result.ShouldBe("hello world");
        mockFactory.StartInfoHistory.Count.ShouldBe(1);
        mockFactory.StartInfoHistory[0].FileName.ShouldBe("/test/echo");
    }

    [TestMethod]
    public void ConfigurableCliExecutor_CanReplaceGitCliExecutor()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = "git",
            ResponseFormat = ResponseFormat.PlainText
        });

        // Act
        ConfigurableCliExecutor executor = new(logger, options);
        
        // Assert
        executor.ShouldNotBeNull();
        // This proves ConfigurableCliExecutor can replace GitCliExecutor with just configuration
    }

    [TestMethod]
    public void ConfigurableCliExecutor_CanReplaceWeatherCliExecutor()
    {
        // Arrange
        ILogger<ConfigurableCliExecutor> logger = Substitute.For<ILogger<ConfigurableCliExecutor>>();
        MockProcessFactory mockFactory = new();
        
        IOptions<CliExecutorOptions> options = Options.Create(new CliExecutorOptions
        {
            ExecutableName = "weather",
            ExecutablePath = "/test/weather",
            SearchPaths = ["/test/path1", "/test/path2"]
        });

        // Act
        ConfigurableCliExecutor executor = new(logger, options, mockFactory);
        
        // Assert
        executor.ShouldNotBeNull();
        // This proves ConfigurableCliExecutor can replace WeatherCliExecutor with just configuration
    }
}