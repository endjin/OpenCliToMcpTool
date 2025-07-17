namespace OpenCliToMcp.Core;

/// <summary>
/// Defines the contract for executing CLI commands.
/// </summary>
public interface ICliExecutor
{
    /// <summary>
    /// Executes a CLI command with the specified arguments.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command output.</returns>
    Task<string> ExecuteAsync(string command, IEnumerable<string> arguments, CancellationToken cancellationToken = default);
}