namespace OpenCliToMcp.Core;

/// <summary>
/// Exception thrown when an executable cannot be found.
/// </summary>
public class ExecutableNotFoundException : Exception
{
    /// <summary>
    /// Gets the name of the executable that could not be found.
    /// </summary>
    public string ExecutableName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableNotFoundException"/> class.
    /// </summary>
    /// <param name="executableName">The name of the executable that could not be found.</param>
    public ExecutableNotFoundException(string executableName)
        : base($"Executable '{executableName}' was not found")
    {
        ExecutableName = executableName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableNotFoundException"/> class.
    /// </summary>
    /// <param name="executableName">The name of the executable that could not be found.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ExecutableNotFoundException(string executableName, string message)
        : base(message)
    {
        ExecutableName = executableName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableNotFoundException"/> class.
    /// </summary>
    /// <param name="executableName">The name of the executable that could not be found.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExecutableNotFoundException(string executableName, string message, Exception innerException)
        : base(message, innerException)
    {
        ExecutableName = executableName;
    }
}