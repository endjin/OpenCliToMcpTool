namespace OpenCliToMcp.Core;

/// <summary>
/// Defines the contract for formatting process execution results.
/// </summary>
public interface IResponseFormatter
{
    /// <summary>
    /// Formats a process result according to the specified format.
    /// </summary>
    /// <param name="result">The process result to format.</param>
    /// <param name="format">The desired response format.</param>
    /// <returns>The formatted response string.</returns>
    string Format(ProcessResult result, ResponseFormat format);
}