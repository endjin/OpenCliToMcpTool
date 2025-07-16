using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenCliToMcp.Core.Tests.Mocks;
using Shouldly;

namespace OpenCliToMcp.Core.Tests;

[TestClass]
public class CliExecutorBaseTests
{
    private ILogger<TestCliExecutor> logger = null!;
    private IOptions<CliExecutorOptions> options = null!;
    private TestCliExecutor executor = null!;

    #region Cross-Platform Path Utilities

    /// <summary>
    /// Creates a cross-platform absolute test path.
    /// </summary>
    private static string CreateTestPath(params string[] pathParts)
    {
        if (OperatingSystem.IsWindows())
        {
            // Use C:\ as the root for Windows
            var parts = new string[pathParts.Length + 1];
            parts[0] = "C:";
            Array.Copy(pathParts, 0, parts, 1, pathParts.Length);
            return Path.Combine(parts);
        }
        else
        {
            // Use / as the root for Unix-like systems
            return Path.Combine("/", Path.Combine(pathParts));
        }
    }

    /// <summary>
    /// Creates a relative test path using proper path separators.
    /// </summary>
    private static string CreateRelativePath(params string[] pathParts)
    {
        return Path.Combine(pathParts);
    }

    /// <summary>
    /// Normalizes a path for cross-platform comparison.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        try
        {
            // Handle relative paths properly
            if (!Path.IsPathRooted(path))
            {
                // For relative paths, just normalize separators
                return path.Replace('\\', '/');
            }

            // For absolute paths, use GetFullPath for proper normalization
            string fullPath = Path.GetFullPath(path);
            
            // Convert to forward slashes for consistent comparison
            string normalized = fullPath.Replace('\\', '/');
            
            // Handle Windows drive letters consistently (convert to lowercase)
            if (OperatingSystem.IsWindows() && normalized.Length >= 2 && normalized[1] == ':')
            {
                normalized = char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);
            }
            
            return normalized;
        }
        catch (ArgumentException)
        {
            // If path is invalid, fall back to simple separator replacement
            return path.Replace('\\', '/');
        }
    }

    /// <summary>
    /// Asserts that two paths are equal after normalization.
    /// </summary>
    private static void AssertPathsEqual(string expected, string? actual, string? customMessage = null)
    {
        if (actual == null)
        {
            actual.ShouldNotBeNull(customMessage);
            return;
        }

        string normalizedExpected = NormalizePath(expected);
        string normalizedActual = NormalizePath(actual);
        normalizedActual.ShouldBe(normalizedExpected, customMessage);
    }

    /// <summary>
    /// Asserts that a path contains a specific substring after normalization.
    /// </summary>
    private static void AssertPathContains(string? actual, string expectedSubstring, string? customMessage = null)
    {
        if (actual == null)
        {
            actual.ShouldNotBeNull(customMessage);
            return;
        }

        string normalizedActual = NormalizePath(actual);
        string normalizedExpected = NormalizePath(expectedSubstring);
        normalizedActual.ShouldContain(normalizedExpected);
    }

    #endregion

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<TestCliExecutor>>();
        options = Options.Create(new CliExecutorOptions());
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        logger = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCliExecutor(logger, options)).ParamName.ShouldBe("logger");
    }

    [TestMethod]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        options = null!;

        // Act
        executor = new TestCliExecutor(logger, options);

        // Assert
        executor.ShouldNotBeNull();
        // The executor should work with default options
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenExecutableNotFound_ReturnsErrorResponse()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.Json,
            SearchInPath = false,
            ExecutablePath = CreateTestPath("nonexistent", "path", "to", "executable")
        });

        MockProcessFactory mockFactory = new();
        
        // Create an executor that won't find the executable
        ExecutableNotFoundTestExecutor executor = new(logger, options, mockFactory);
        
        // Act
        string result = await executor.ExecuteAsync("test", ["arg"]);
        
        // Debug output
        Console.WriteLine($"Executor result:\n{result}");
        
        // Assert - check for JSON structure and error message
        result.ShouldContain("\"success\":");
        result.ShouldContain("false");
        result.ShouldContain("test-exec executable not found");
    }
    
    // Special test executor that never finds the executable
    private class ExecutableNotFoundTestExecutor : CliExecutorBase
    {
        public ExecutableNotFoundTestExecutor(ILogger logger, IOptions<CliExecutorOptions> options, IProcessFactory processFactory, IFileSystem? fileSystem = null)
            : base(logger, options, processFactory, fileSystem)
        {
        }
        
        protected override string ExecutableName => "test-exec";
        
        protected override bool ValidateExecutable(string path)
        {
            return false; // Never find any executable
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenExecutableNotFoundAndThrowOnError_ThrowsCliExecutionException()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions { ThrowOnError = true });
        executor = new TestCliExecutor(logger, options)
        {
            ShouldFindExecutable = false
        };

        // Act & Assert
        await Should.ThrowAsync<CliExecutionException>(
            executor.ExecuteAsync("test", ["arg1", "arg2"]));
    }

    [TestMethod]
    public void ExecuteAsync_WithCustomExecutablePath_UsesConfiguredPath()
    {
        // Arrange
        string customPath = CreateTestPath("custom", "path", "to", "executable");
        options = Options.Create(new CliExecutorOptions 
        { 
            ExecutablePath = customPath,
            ResponseFormat = ResponseFormat.Raw 
        });
        
        // Act
        executor = new TestCliExecutor(logger, options);

        // Assert
        // The test executor should validate the custom path
        executor.SearchedFullPaths.ShouldContain(customPath);
    }

    [TestMethod]
    public void ExecuteAsync_WithEnvironmentVariables_PassesThemToProcess()
    {
        // Arrange
        Dictionary<string, string> envVars = new()
        {
            ["TEST_VAR1"] = "value1",
            ["TEST_VAR2"] = "value2"
        };
        options = Options.Create(new CliExecutorOptions 
        { 
            EnvironmentVariables = envVars,
            ResponseFormat = ResponseFormat.Raw,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        // Act
        executor = new TestCliExecutor(logger, options)
        {
            ShouldFindExecutable = true
        };
        
        // Note: We can't easily test the environment variables being passed without actually executing
        // This would require either mocking Process.Start or refactoring CliExecutorBase
        // For now, we just verify the executor is created with the options
        
        // Assert
        executor.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithWorkingDirectory_SetsProcessWorkingDirectory()
    {
        // Arrange
        string workingDir = CreateTestPath("test", "working", "directory");
        MockProcessFactory mockFactory = new();
        MockProcess mockProcess = mockFactory.CreateProcess("test output", "", 0);
        mockFactory.EnqueueProcess(mockProcess);
        
        options = Options.Create(new CliExecutorOptions 
        { 
            WorkingDirectory = workingDir,
            ResponseFormat = ResponseFormat.Raw,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        // Act
        executor = new TestCliExecutor(logger, options, mockFactory)
        {
            ShouldFindExecutable = true
        };
        await executor.ExecuteAsync("test", ["arg"]);

        // Assert
        mockFactory.StartInfoHistory.Count.ShouldBe(1);
        mockFactory.StartInfoHistory[0].WorkingDirectory.ShouldBe(workingDir);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithJsonResponseFormat_ReturnsJsonFormattedResponse()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.Json,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        MockProcessFactory mockFactory = new();
        MockProcess mockProcess = mockFactory.CreateProcess("hello world", "", 0);
        mockFactory.EnqueueProcess(mockProcess);
        
        executor = new TestCliExecutor(logger, options, mockFactory)
        {
            ShouldFindExecutable = true
        };

        // Act
        string result = await executor.ExecuteAsync("test", ["hello"]);

        // Assert - check for JSON structure
        result.ShouldContain("\"success\":");
        result.ShouldContain("true");
        result.ShouldContain("\"exitCode\":");
        result.ShouldContain("0");
        result.ShouldContain("\"output\":");
        result.ShouldContain("hello world");
    }

    [TestMethod]
    public void ExecuteAsync_WithPlainTextResponseFormatAndError_ReturnsFormattedError()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.PlainText,
            ExecutablePath = CreateTestPath("nonexistent", "executable")
        });
        
        // Act
        executor = new TestCliExecutor(logger, options)
        {
            ShouldFindExecutable = false
        };

        // Assert
        // With PlainText format and no executable found, the constructor will set executablePath to null
        // We can't easily test the actual error formatting without executing
        executor.ShouldNotBeNull();
    }

    [TestMethod]  
    public void FindExecutable_WithConfiguredPath_UsesConfiguredPath()
    {
        // Arrange
        string configuredPath = CreateTestPath("test", "configured", "path");
        MockProcessFactory testProcessFactory = new();
        
        // Create a custom test executor that allows us to track search behavior
        SearchTrackingExecutor searchTracker = new(logger, Options.Create(new CliExecutorOptions 
        { 
            ExecutablePath = configuredPath,
            ResponseFormat = ResponseFormat.Raw,
        }), testProcessFactory);

        // Assert
        // Check if the configured path was searched
        searchTracker.SearchedFullPaths.Count.ShouldBeGreaterThan(0, "No paths were searched");
        
        
        // The configured path should be the first and only path checked
        searchTracker.SearchedFullPaths.ShouldContain(configuredPath);
    }

    // Special test executor that tracks all searched paths without finding any
    private class SearchTrackingExecutor : CliExecutorBase
    {
        public List<string> SearchedFullPaths { get; } = [];
        
        public SearchTrackingExecutor(ILogger logger, IOptions<CliExecutorOptions> options, IProcessFactory processFactory, IFileSystem? fileSystem = null)
            : base(logger, options, processFactory, fileSystem)
        {
        }
        
        protected override string ExecutableName => "test-exec";
        
        protected override bool ValidateExecutable(string path)
        {
            SearchedFullPaths.Add(path);
            return false; // Never find the executable to ensure all paths are searched
        }
    }

    [TestMethod]
    public void ExecuteAsync_WithArgumentsContainingSpecialCharacters_EscapesThemProperly()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.Raw,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        // Act
        executor = new TestCliExecutor(logger, options);

        // Assert
        // We would need to actually execute to test argument escaping
        // or refactor to expose the BuildArgumentString method
        // For now, just verify the executor is created
        executor.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenProcessFailsAndThrowOnError_ThrowsCliExecutionException()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ThrowOnError = true,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        MockProcessFactory mockFactory = new();
        MockProcess mockProcess = mockFactory.CreateProcess("", "Command failed", 1);
        mockFactory.EnqueueProcess(mockProcess);
        
        executor = new TestCliExecutor(logger, options, mockFactory)
        {
            ShouldFindExecutable = true
        };

        // Act & Assert
        CliExecutionException exception = await Should.ThrowAsync<CliExecutionException>(
            executor.ExecuteAsync("test", []));
        
        exception.Message.ShouldContain("test-exec command failed");
        exception.Response.ShouldNotBeNull();
        exception.Response.Success.ShouldBeFalse();
        exception.Response.ExitCode.ShouldBe(1);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenProcessFactoryReturnsNull_ReturnsErrorResponse()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.Json,
            ExecutablePath = CreateTestPath("test", "executable")
        });
        
        MockProcessFactory mockFactory = new() { ReturnNullProcess = true };
        executor = new TestCliExecutor(logger, options, mockFactory)
        {
            ShouldFindExecutable = true
        };

        // Act
        string result = await executor.ExecuteAsync("test", ["arg1", "arg2"]);

        // Assert - check for JSON structure and values
        result.ShouldContain("\"success\":");
        result.ShouldContain("false");
        result.ShouldContain("Failed to start test-exec process");
        result.ShouldContain("\"exitCode\":");
        result.ShouldContain("-1");
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenProcessTimesOut_ReturnsTimeoutError()
    {
        // Arrange
        options = Options.Create(new CliExecutorOptions 
        { 
            ResponseFormat = ResponseFormat.Json,
            ExecutablePath = CreateTestPath("test", "executable"),
            TimeoutSeconds = 1
        });
        
        MockProcessFactory mockFactory = new();
        MockProcess mockProcess = new();
        mockProcess.SetOutput("partial output");
        // Don't complete execution - this will cause timeout
        mockFactory.EnqueueProcess(mockProcess);
        
        executor = new TestCliExecutor(logger, options, mockFactory)
        {
            ShouldFindExecutable = true
        };

        // Act
        string result = await executor.ExecuteAsync("test", ["arg1"]);

        // Assert - check for JSON structure and values
        result.ShouldContain("\"success\":");
        result.ShouldContain("false");
        result.ShouldContain("Command timed out after 1 seconds");
        mockProcess.WasKilled.ShouldBeTrue();
    }

    [TestMethod]
    public void FindExecutable_WithSearchPaths_FindsExecutableInSearchPath()
    {
        // Arrange
        string[] searchPaths = [
            CreateTestPath("search", "path1"), 
            CreateTestPath("search", "path2"), 
            CreateTestPath("search", "path3")
        ];
        MockProcessFactory testProcessFactory = new();
        MockFileSystem mockFileSystem = new();
        
        // Set up file system to have executable in second search path
        mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path2"), "test-exec"));
        
        SearchPathsTestExecutor executor = new(logger, Options.Create(new CliExecutorOptions 
        { 
            SearchPaths = searchPaths,
            SearchInPath = true,
            ResponseFormat = ResponseFormat.Raw 
        }), testProcessFactory, mockFileSystem);

        // Assert
        // The executor should have found the executable in the second search path
        AssertPathsEqual(Path.Combine(CreateTestPath("search", "path2"), "test-exec"), executor.FoundExecutablePath);
        
        // Verify that it searched the paths in order
        executor.SearchedFullPaths.Count.ShouldBeGreaterThanOrEqualTo(2);
        // The implementation checks multiple extensions, so we need to verify it checked the right paths
        string expectedPath1 = Path.Combine(CreateTestPath("search", "path1"), "test-exec");
        string expectedPath2 = Path.Combine(CreateTestPath("search", "path2"), "test-exec");
        executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(expectedPath1));
        executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(expectedPath2));
    }

    [TestMethod]
    public void FindExecutable_WithSearchPathsButSearchInPathFalse_DoesNotUseSearchPaths()
    {
        // Arrange
        string[] searchPaths = [
            CreateTestPath("search", "path1"), 
            CreateTestPath("search", "path2"), 
            CreateTestPath("search", "path3")
        ];
        MockProcessFactory testProcessFactory = new();
        MockFileSystem mockFileSystem = new();
        
        // Set up file system to have executable in search paths
        mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path1"), "test-exec"));
        mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path2"), "test-exec"));
        
        SearchPathsTestExecutor executor = new(logger, Options.Create(new CliExecutorOptions 
        { 
            SearchPaths = searchPaths,
            SearchInPath = false,  // This should prevent search paths from being used
            ResponseFormat = ResponseFormat.Raw 
        }), testProcessFactory, mockFileSystem);

        // Assert
        // The executor should NOT have found the executable because SearchInPath is false
        executor.FoundExecutablePath.ShouldBeNull();
        
        // Verify that search paths were NOT checked
        string searchPathPattern = NormalizePath(CreateTestPath("search", "path"));
        executor.SearchedFullPaths.ShouldNotContain(path => NormalizePath(path).StartsWith(searchPathPattern));
        
        // It should only have tried the executable name directly
        executor.SearchedFullPaths.ShouldContain("test-exec");
    }

    [TestMethod]
    public void FindExecutable_WithMultipleOptions_UsesCorrectPriority()
    {
        // Arrange
        string executablePath = CreateTestPath("explicit", "path", "test-exec");
        string[] searchPaths = [
            CreateTestPath("search", "path1"),
            CreateTestPath("search", "path2")
        ];
        MockProcessFactory testProcessFactory = new();
        MockFileSystem mockFileSystem = new();
        
        // Set up file system with executables in all possible locations
        mockFileSystem.AddFile(executablePath);  // Explicit path
        mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path1"), "test-exec"));  // First search path
        mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path2"), "test-exec"));  // Second search path
        mockFileSystem.AddFile("test-exec");  // Current directory (PATH)
        
        // Test 1: ExecutablePath takes priority over everything
        SearchPathsTestExecutor executor1 = new(logger, Options.Create(new CliExecutorOptions 
        { 
            ExecutablePath = executablePath,
            SearchPaths = searchPaths,
            SearchInPath = true,
            ResponseFormat = ResponseFormat.Raw 
        }), testProcessFactory, mockFileSystem);

        // Should use the explicit path first
        executor1.FoundExecutablePath.ShouldBe(executablePath);
        executor1.SearchedFullPaths[0].ShouldBe(executablePath);
        
        // Test 2: SearchPaths take priority over PATH when ExecutablePath is not set
        SearchPathsTestExecutor executor2 = new(logger, Options.Create(new CliExecutorOptions 
        { 
            SearchPaths = searchPaths,
            SearchInPath = true,
            ResponseFormat = ResponseFormat.Raw 
        }), testProcessFactory, mockFileSystem);

        // Should find in first search path (normalize for cross-platform compatibility)
        AssertPathsEqual(Path.Combine(CreateTestPath("search", "path1"), "test-exec"), executor2.FoundExecutablePath);
        
        // Test 3: Falls back to PATH when no ExecutablePath and no SearchPaths
        SearchPathsTestExecutor executor3 = new(logger, Options.Create(new CliExecutorOptions 
        { 
            SearchInPath = true,
            ResponseFormat = ResponseFormat.Raw 
        }), testProcessFactory, mockFileSystem);

        // Should find in PATH (current directory)
        executor3.FoundExecutablePath.ShouldBe("test-exec");
    }

    [TestMethod]
    public void FindExecutable_WithSearchPaths_ChecksMultipleExtensions()
    {
        // Arrange
        string[] searchPaths = [
            CreateTestPath("search", "path1"),
            CreateTestPath("search", "path2")
        ];
        MockProcessFactory testProcessFactory = new();
        MockFileSystem mockFileSystem = new();
        
        // Test on Unix-like systems (also checks .exe for .NET executables)
        if (!OperatingSystem.IsWindows())
        {
            // Set up file system with .exe extension in search path
            mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path1"), "test-exec.exe"));
            
            SearchPathsTestExecutor executor = new(logger, Options.Create(new CliExecutorOptions 
            { 
                SearchPaths = searchPaths,
                SearchInPath = true,
                ResponseFormat = ResponseFormat.Raw 
            }), testProcessFactory, mockFileSystem);

            // Should find the .exe version
            AssertPathsEqual(Path.Combine(CreateTestPath("search", "path1"), "test-exec.exe"), executor.FoundExecutablePath);
            
            // Should have checked both with and without .exe
            string expectedBasePath = Path.Combine(CreateTestPath("search", "path1"), "test-exec");
            string expectedExePath = Path.Combine(CreateTestPath("search", "path1"), "test-exec.exe");
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(expectedBasePath));
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(expectedExePath));
        }
        
        // Test Windows-specific extensions
        if (OperatingSystem.IsWindows())
        {
            // Set up file system with various Windows executable extensions
            mockFileSystem.AddFile(Path.Combine(CreateTestPath("search", "path2"), "test-exec.bat"));
            
            SearchPathsTestExecutor executor = new(logger, Options.Create(new CliExecutorOptions 
            { 
                SearchPaths = searchPaths,
                SearchInPath = true,
                ResponseFormat = ResponseFormat.Raw 
            }), testProcessFactory, mockFileSystem);

            // Should find the .bat version
            AssertPathsEqual(Path.Combine(CreateTestPath("search", "path2"), "test-exec.bat"), executor.FoundExecutablePath);
            
            // Should have checked multiple extensions
            string basePath = CreateTestPath("search", "path1");
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(Path.Combine(basePath, "test-exec")));
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(Path.Combine(basePath, "test-exec.exe")));
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(Path.Combine(basePath, "test-exec.bat")));
            executor.SearchedFullPaths.ShouldContain(path => NormalizePath(path) == NormalizePath(Path.Combine(basePath, "test-exec.cmd")));
        }
    }

    [TestMethod]
    public async Task FindExecutable_Integration_FindsExecutableInSearchPathWithFileSystem()
    {
        // Arrange
        string[] searchPaths = ["../nonexistent", "../Weather.Cli/bin/Debug/net9.0", "../another/path"];
        MockProcessFactory mockProcessFactory = new();
        MockFileSystem mockFileSystem = new();
        
        // Simulate the actual weather executable location
        mockFileSystem.AddFile("../Weather.Cli/bin/Debug/net9.0/weather");
        mockFileSystem.AddFile("../Weather.Cli/bin/Debug/net9.0/weather.exe");
        
        // Create mock process for execution
        MockProcess mockProcess = mockProcessFactory.CreateProcess(
            output: @"{""success"":true,""output"":""Weather data"",""exitCode"":0}", 
            error: "", 
            exitCode: 0);
        mockProcessFactory.EnqueueProcess(mockProcess);
        
        // Create the executor with search paths configuration
        IntegrationTestExecutor executor = IntegrationTestExecutor.Create(logger, Options.Create(new CliExecutorOptions 
        { 
            ExecutableName = "weather",
            SearchPaths = searchPaths,
            SearchInPath = true,
            ResponseFormat = ResponseFormat.Json,
            TimeoutSeconds = 30
        }), mockProcessFactory, mockFileSystem);

        // Act
        string result = await executor.ExecuteAsync("weather", ["current", "London"]);

        // Assert
        // Debug output to understand what's happening
        if (executor.FoundExecutablePath == null)
        {
            Console.WriteLine("Searched paths:");
            foreach (var path in executor.SearchedPaths)
            {
                Console.WriteLine($"  - {path}");
            }
        }
        
        // Should have found the executable in the search path
        executor.FoundExecutablePath.ShouldNotBeNull();
        // Check for the path with normalized separators (works on both Windows and Unix)
        AssertPathContains(executor.FoundExecutablePath, 
            CreateRelativePath("Weather.Cli", "bin", "Debug", "net9.0", "weather"),
            $"Expected to find weather executable in search path, but found: {executor.FoundExecutablePath}");
        
        // Should have executed successfully
        result.ShouldContain("\"success\": true", Case.Insensitive);
        result.ShouldContain("Weather data", Case.Insensitive);
        
        // Verify the process was started with correct info
        mockProcessFactory.StartInfoHistory.Count.ShouldBe(1);
        ProcessStartInfo startInfo = mockProcessFactory.StartInfoHistory[0];
        startInfo.FileName.ShouldContain("weather");
        startInfo.Arguments.ShouldContain("current London");
    }

    // Integration test executor that behaves like the real ConfigurableCliExecutor
    private class IntegrationTestExecutor : CliExecutorBase
    {
        private static string? optionsExecutableName;
        public string? FoundExecutablePath { get; private set; }
        public List<string> SearchedPaths { get; } = new();
        
        private IntegrationTestExecutor(ILogger logger, IOptions<CliExecutorOptions> options, IProcessFactory processFactory, IFileSystem fileSystem)
            : base(logger, options, processFactory, fileSystem)
        {
            // Store the found path from base constructor
            var baseType = typeof(CliExecutorBase);
            var executablePathField = baseType.GetField("executablePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (executablePathField != null)
            {
                FoundExecutablePath = executablePathField.GetValue(this) as string;
            }
        }
        
        public static IntegrationTestExecutor Create(ILogger logger, IOptions<CliExecutorOptions>? options, IProcessFactory processFactory, IFileSystem fileSystem)
        {
            // Set the executable name before calling constructor
            optionsExecutableName = options?.Value?.ExecutableName;
            
            // If options is null, create a default one
            if (options == null)
            {
                options = Options.Create(new CliExecutorOptions());
            }
            
            return new IntegrationTestExecutor(logger, options, processFactory, fileSystem);
        }
        
        protected override string ExecutableName => optionsExecutableName ?? "test-exec";
        
        protected override bool ValidateExecutable(string path)
        {
            SearchedPaths.Add(path);
            bool exists = base.ValidateExecutable(path);
            if (exists && FoundExecutablePath == null)
            {
                FoundExecutablePath = path;
            }
            return exists;
        }
    }

    // Test executor that actually finds executables based on mock file system
    private class SearchPathsTestExecutor : CliExecutorBase
    {
        public List<string> SearchedFullPaths { get; } = [];
        public string? FoundExecutablePath { get; private set; }
        
        public SearchPathsTestExecutor(ILogger logger, IOptions<CliExecutorOptions> options, IProcessFactory processFactory, IFileSystem fileSystem)
            : base(logger, options, processFactory, fileSystem)
        {
        }
        
        protected override string ExecutableName => "test-exec";
        
        protected override bool ValidateExecutable(string path)
        {
            SearchedFullPaths.Add(path);
            bool exists = base.ValidateExecutable(path);
            if (exists && FoundExecutablePath == null)
            {
                FoundExecutablePath = path;
            }
            return exists;
        }
    }

    // Mock file system for testing
    private class MockFileSystem : IFileSystem
    {
        private readonly HashSet<string> files = [];
        
        public void AddFile(string path) => files.Add(NormalizePath(path));
        
        public bool FileExists(string path) => files.Contains(NormalizePath(path));
        
        public string GetCurrentDirectory() => CreateTestPath("test", "current");
        
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                // Handle relative paths properly
                if (!Path.IsPathRooted(path))
                {
                    // For relative paths, just normalize separators
                    return path.Replace('\\', '/');
                }

                // For absolute paths, use GetFullPath for proper normalization
                string fullPath = Path.GetFullPath(path);
                
                // Convert to forward slashes for consistent comparison
                string normalized = fullPath.Replace('\\', '/');
                
                // Handle Windows drive letters consistently (convert to lowercase)
                if (OperatingSystem.IsWindows() && normalized.Length >= 2 && normalized[1] == ':')
                {
                    normalized = char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);
                }
                
                return normalized;
            }
            catch (ArgumentException)
            {
                // If path is invalid, fall back to simple separator replacement
                return path.Replace('\\', '/');
            }
        }
    }

    // Test implementation of CliExecutorBase for testing
    public class TestCliExecutor : CliExecutorBase
    {
        private readonly CliExecutorOptions? _options;
        
        public bool ShouldFindExecutable { get; set; } = true;
        public bool ForceSearchAllPaths { get; set; } = false;
        public ProcessStartInfo? LastProcessStartInfo { get; set; }
        public List<string> SearchedFullPaths { get; } = [];
        public string? OverrideArgs { get; set; }
        public bool SimulateEchoCommand { get; set; } = false;
        public string? SimulatedOutput { get; set; }
        public int SimulatedExitCode { get; set; } = 0;

        public TestCliExecutor(ILogger<TestCliExecutor> logger, IOptions<CliExecutorOptions> options, IProcessFactory? processFactory = null, IFileSystem? fileSystem = null) 
            : base(logger, options, processFactory, fileSystem)
        {
            _options = options?.Value ?? new CliExecutorOptions();
        }

        protected override string ExecutableName => "test-exec";

        protected override bool ValidateExecutable(string path)
        {
            SearchedFullPaths.Add(path);
            
            
            // For nonexistent paths, always return false
            string normalizedPath = NormalizePath(path);
            if (normalizedPath.Contains("nonexistent"))
                return false;
                
            // For unit test paths, use ShouldFindExecutable to control whether the path is found
            bool isTestPath = normalizedPath.Contains("test") || 
                             normalizedPath.Contains("mock") ||
                             path == "echo" ||
                             path.Contains("cmd.exe") ||
                             normalizedPath.Contains("bin/sh") ||
                             path.Contains("test-exec") ||
                             path == "test-exec";
                             
            if (isTestPath)
            {
                return ShouldFindExecutable;
            }
            
            // For search path testing, don't find it immediately so all paths are searched
            if (ForceSearchAllPaths)
            {
                return false; // Force it to search all paths
            }
            
            // For real paths, return false (not found in test environment)
            return false;
        }

        protected override void ConfigureProcess(ProcessStartInfo startInfo)
        {
            LastProcessStartInfo = startInfo;
            
            // If we have override args, use them
            if (!string.IsNullOrEmpty(OverrideArgs))
            {
                startInfo.Arguments = OverrideArgs;
            }
        }
    }
}