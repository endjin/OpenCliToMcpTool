namespace OpenCliToMcp.Core;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public record ProcessResult
{
    /// <summary>
    /// Whether the process executed successfully (exit code 0).
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The exit code of the process.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// The standard output of the process.
    /// </summary>
    public required string Output { get; init; }

    /// <summary>
    /// The standard error of the process.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Whether the process was cancelled due to timeout or cancellation token.
    /// </summary>
    public bool WasCancelled { get; init; }

    /// <summary>
    /// The actual duration of the process execution.
    /// </summary>
    public TimeSpan Duration { get; init; }
}