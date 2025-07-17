using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenCliToMcp.Core;

/// <summary>
/// Base class for CLI executors that provides common functionality.
/// </summary>
public abstract class CliExecutorBase : ICliExecutor
{
    private readonly ILogger logger;
    private readonly CliExecutorOptions options;
    private readonly IProcessFactory processFactory;
    private readonly IFileSystem fileSystem;
    private readonly string? executablePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliExecutorBase"/> class.
    /// </summary>
    protected CliExecutorBase(ILogger logger, IOptions<CliExecutorOptions> options, IProcessFactory? processFactory = null, IFileSystem? fileSystem = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options?.Value ?? new CliExecutorOptions();
        this.processFactory = processFactory ?? new DefaultProcessFactory();
        this.fileSystem = fileSystem ?? new DefaultFileSystem();
        executablePath = FindExecutable();
    }

    /// <summary>
    /// Gets the name of the executable to find (without extension).
    /// </summary>
    protected abstract string ExecutableName { get; }


    /// <summary>
    /// Allows derived classes to configure the process before execution.
    /// </summary>
    protected virtual void ConfigureProcess(ProcessStartInfo startInfo) { }

    /// <summary>
    /// Allows derived classes to validate the executable before execution.
    /// </summary>
    protected virtual bool ValidateExecutable(string path) => fileSystem.FileExists(path);

    /// <inheritdoc/>
    public async Task<string> ExecuteAsync(string command, IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(executablePath))
            {
                string error = $"{ExecutableName} executable not found. Please ensure it's installed and in PATH.";
                logger.LogError(error);
                return ResponseFormatter.FormatResponse(CliResponse.CreateError(error, 127), options.ResponseFormat);
            }

            ProcessStartInfo startInfo = CreateProcessStartInfo(arguments);
            logger.LogDebug("Executing: {Path} {Arguments}", executablePath, startInfo.Arguments);

            using IProcess? process = processFactory.Start(startInfo);
            if (process == null)
            {
                string error = $"Failed to start {ExecutableName} process";
                logger.LogError(error);
                return ResponseFormatter.FormatResponse(CliResponse.CreateError(error, -1), options.ResponseFormat);
            }

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

            StringBuilder outputBuilder = new();
            StringBuilder errorBuilder = new();

            Task outputTask = StreamReaderUtility.ReadStreamAsync(process.StandardOutput, outputBuilder, cts.Token);
            Task errorTask = StreamReaderUtility.ReadStreamAsync(process.StandardError, errorBuilder, cts.Token);

            try
            {
                await process.WaitForExitAsync(cts.Token);
                await Task.WhenAll(outputTask, errorTask);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                string error = cancellationToken.IsCancellationRequested 
                    ? "Command execution was cancelled" 
                    : $"Command timed out after {options.TimeoutSeconds} seconds";
                logger.LogWarning(error);
                return ResponseFormatter.FormatResponse(CliResponse.CreateError(error, -1), options.ResponseFormat);
            }

            string output = outputBuilder.ToString();
            string errorOutput = errorBuilder.ToString();

            logger.LogDebug("Command completed with exit code: {ExitCode}", process.ExitCode);

            CliResponse response = new()
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = output,
                Error = errorOutput
            };

            if (!response.Success && options.ThrowOnError)
            {
                throw new CliExecutionException($"{ExecutableName} command failed", response);
            }

            return ResponseFormatter.FormatResponse(response, options.ResponseFormat);
        }
        catch (Exception ex) when (ex is not CliExecutionException)
        {
            logger.LogError(ex, "Failed to execute {ExecutableName} command", ExecutableName);
            CliResponse response = CliResponse.CreateError($"Execution failed: {ex.Message}", -1);
            
            if (options.ThrowOnError)
            {
                throw new CliExecutionException($"Failed to execute {ExecutableName} command", response, ex);
            }

            return ResponseFormatter.FormatResponse(response, options.ResponseFormat);
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(IEnumerable<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executablePath!,
            Arguments = ArgumentBuilder.BuildArgumentString(arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = options.WorkingDirectory ?? fileSystem.GetCurrentDirectory()
        };

        if (options.EnvironmentVariables != null)
        {
            foreach ((string key, string value) in options.EnvironmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        ConfigureProcess(startInfo);
        return startInfo;
    }

    private string? FindExecutable()
    {
        // Configuration-first approach: require explicit executable path
        if (!string.IsNullOrEmpty(options.ExecutablePath))
        {
            if (ValidateExecutable(options.ExecutablePath))
            {
                logger.LogInformation("Using configured {ExecutableName} path: {Path}", ExecutableName, options.ExecutablePath);
                return options.ExecutablePath;
            }
            else
            {
                logger.LogError("Configured {ExecutableName} path does not exist: {Path}", ExecutableName, options.ExecutablePath);
                return null;
            }
        }

        // Check configured search paths
        if (options.SearchInPath && options.SearchPaths?.Length > 0)
        {
            // Get possible executable names with platform-specific extensions
            var executableNames = GetPossibleExecutableNames(ExecutableName);
            
            foreach (var searchPath in options.SearchPaths)
            {
                foreach (var execName in executableNames)
                {
                    var fullPath = Path.Combine(searchPath, execName);
                    if (ValidateExecutable(fullPath))
                    {
                        logger.LogInformation("Found {ExecutableName} in search path: {Path}", ExecutableName, fullPath);
                        return fullPath;
                    }
                }
            }
        }

        // Fallback: try just the executable name (relies on PATH)
        if (ValidateExecutable(ExecutableName))
        {
            logger.LogInformation("Using {ExecutableName} from PATH", ExecutableName);
            return ExecutableName;
        }

        logger.LogError("{ExecutableName} executable not found. Please configure the ExecutablePath in options.", ExecutableName);
        return null;
    }

    private static string[] GetPossibleExecutableNames(string baseName)
    {
        // Check for common executable extensions based on platform
        if (OperatingSystem.IsWindows())
        {
            return new[] { baseName, $"{baseName}.exe", $"{baseName}.bat", $"{baseName}.cmd" };
        }
        else
        {
            // On Unix-like systems, also check for .exe (for .NET executables)
            return new[] { baseName, $"{baseName}.exe" };
        }
    }
}