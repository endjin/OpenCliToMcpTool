namespace OpenCliToMcp.Core;

/// <summary>
/// Exception thrown when CLI command execution fails.
/// </summary>
public class CliExecutionException : Exception
{
    /// <summary>
    /// Gets the CLI response that caused the exception.
    /// </summary>
    public CliResponse Response { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliExecutionException"/> class.
    /// </summary>
    public CliExecutionException(string message, CliResponse response) 
        : base(message)
    {
        Response = response;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliExecutionException"/> class.
    /// </summary>
    public CliExecutionException(string message, CliResponse response, Exception innerException) 
        : base(message, innerException)
    {
        Response = response;
    }
}