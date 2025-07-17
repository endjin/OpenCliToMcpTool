using System.Text;

namespace OpenCliToMcp.Core;

/// <summary>
/// Utility class for reading streams asynchronously.
/// </summary>
public static class StreamReaderUtility
{
    /// <summary>
    /// Reads a stream asynchronously and appends the content to a StringBuilder.
    /// </summary>
    /// <param name="reader">The stream reader to read from.</param>
    /// <param name="builder">The StringBuilder to append to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the stream is fully read.</returns>
    public static async Task ReadStreamAsync(StreamReader reader, StringBuilder builder, CancellationToken cancellationToken)
    {
        char[] buffer = new char[4096];
        int bytesRead;

        while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            builder.Append(buffer, 0, bytesRead);
        }
    }
}