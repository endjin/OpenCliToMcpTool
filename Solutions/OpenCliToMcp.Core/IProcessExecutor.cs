namespace OpenCliToMcp.Core;

/// <summary>
/// Defines the contract for executing processes.
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Executes a process with the specified request parameters.
    /// </summary>
    /// <param name="request">The process execution request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the process execution result.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessRequest request);
}