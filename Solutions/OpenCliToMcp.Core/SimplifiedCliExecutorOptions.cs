namespace OpenCliToMcp.Core;

/// <summary>
/// Options for configuring the simplified CLI executor.
/// </summary>
public record SimplifiedCliExecutorOptions
{
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
    /// Whether to throw exceptions on command failures or return error responses.
    /// </summary>
    public bool ThrowOnError { get; init; } = false;

    /// <summary>
    /// The response format to use. Defaults to JSON.
    /// </summary>
    public ResponseFormat ResponseFormat { get; init; } = ResponseFormat.Json;
}