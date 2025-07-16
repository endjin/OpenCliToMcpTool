namespace OpenCliToMcp.Core;

/// <summary>
/// Configuration options for CLI executors.
/// </summary>
public record CliExecutorOptions
{
    /// <summary>
    /// The name of the executable to run (e.g., "git", "npm", "docker").
    /// Required for ConfigurableCliExecutor.
    /// </summary>
    public string? ExecutableName { get; set; }

    /// <summary>
    /// The path to the CLI executable. If not specified, the executor will attempt to find it.
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// The working directory for command execution. Defaults to the current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// The timeout in seconds for command execution. Defaults to 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Environment variables to set for the command execution.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Additional search paths for finding the executable.
    /// </summary>
    public string[]? SearchPaths { get; set; }

    /// <summary>
    /// Whether to search for the executable in the system PATH.
    /// </summary>
    public bool SearchInPath { get; init; } = true;

    /// <summary>
    /// Whether to throw exceptions on command failures or return error responses.
    /// </summary>
    public bool ThrowOnError { get; init; } = false;

    /// <summary>
    /// The response format to use. Defaults to JSON.
    /// </summary>
    public ResponseFormat ResponseFormat { get; init; } = ResponseFormat.Json;
}