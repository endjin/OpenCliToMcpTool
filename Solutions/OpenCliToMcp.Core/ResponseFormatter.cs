using System.Text;

namespace OpenCliToMcp.Core;

/// <summary>
/// Utility class for formatting CLI responses in different formats.
/// </summary>
public static class ResponseFormatter
{
    /// <summary>
    /// Formats a CLI response according to the specified format.
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>The formatted response string.</returns>
    public static string FormatResponse(CliResponse response, ResponseFormat format)
    {
        return format switch
        {
            ResponseFormat.Json => response.ToJson(),
            ResponseFormat.Raw => response.Success ? response.Output : response.Error,
            ResponseFormat.PlainText => FormatPlainTextResponse(response),
            _ => response.ToJson()
        };
    }

    /// <summary>
    /// Formats a response as plain text with error information.
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <returns>The formatted plain text response.</returns>
    private static string FormatPlainTextResponse(CliResponse response)
    {
        if (response.Success)
        {
            return response.Output;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Command failed with exit code {response.ExitCode}");
        
        if (!string.IsNullOrEmpty(response.Error))
        {
            sb.AppendLine("Error:");
            sb.AppendLine(response.Error);
        }
        
        if (!string.IsNullOrEmpty(response.Output))
        {
            sb.AppendLine("Output:");
            sb.AppendLine(response.Output);
        }

        return sb.ToString().TrimEnd();
    }
}