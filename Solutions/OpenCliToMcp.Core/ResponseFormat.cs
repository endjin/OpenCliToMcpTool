namespace OpenCliToMcp.Core;

/// <summary>
/// Defines the format for CLI responses.
/// </summary>
public enum ResponseFormat
{
    /// <summary>
    /// Return responses as JSON-serialized CliResponse objects.
    /// </summary>
    Json,

    /// <summary>
    /// Return raw command output (stdout for success, stderr for errors).
    /// </summary>
    Raw,

    /// <summary>
    /// Return plain text with error information included.
    /// </summary>
    PlainText
}