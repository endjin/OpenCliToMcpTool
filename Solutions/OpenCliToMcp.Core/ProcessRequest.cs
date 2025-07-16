namespace OpenCliToMcp.Core;

/// <summary>
/// Represents a request to execute a process.
/// </summary>
public record ProcessRequest
{
    /// <summary>
    /// The full path to the executable file.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The arguments to pass to the executable.
    /// </summary>
    public required string[] Arguments { get; init; }

    /// <summary>
    /// The working directory for the process. If null, uses the current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Environment variables to set for the process.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// The timeout for the process execution.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}