namespace OpenCliToMcp.Core;

/// <summary>
/// Abstraction for a system process to enable unit testing.
/// </summary>
public interface IProcess : IDisposable
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    int ExitCode { get; }
    
    /// <summary>
    /// Gets a stream reader for the standard output of the process.
    /// </summary>
    StreamReader StandardOutput { get; }
    
    /// <summary>
    /// Gets a stream reader for the standard error of the process.
    /// </summary>
    StreamReader StandardError { get; }
    
    /// <summary>
    /// Waits asynchronously for the process to exit.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>A task that completes when the process exits.</returns>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Immediately stops the process.
    /// </summary>
    void Kill();
}