namespace OpenCliToMcp.Core;

/// <summary>
/// Default implementation of <see cref="IResponseFormatter"/>.
/// </summary>
public class DefaultResponseFormatter : IResponseFormatter
{
    /// <inheritdoc/>
    public string Format(ProcessResult result, ResponseFormat format)
    {
        ArgumentNullException.ThrowIfNull(result);

        return format switch
        {
            ResponseFormat.Json => FormatAsJson(result),
            ResponseFormat.Raw => FormatAsRaw(result),
            ResponseFormat.PlainText => FormatAsPlainText(result),
            _ => FormatAsJson(result)
        };
    }

    private static string FormatAsJson(ProcessResult result)
    {
        CliResponse response = new()
        {
            Success = result.Success,
            ExitCode = result.ExitCode,
            Output = result.Output,
            Error = result.Error
        };

        return response.ToJson();
    }

    private static string FormatAsRaw(ProcessResult result)
    {
        if (result.Success)
        {
            return result.Output;
        }

        // For failed processes, return error if available, otherwise output
        return !string.IsNullOrEmpty(result.Error) ? result.Error : result.Output;
    }

    private static string FormatAsPlainText(ProcessResult result)
    {
        if (result.Success)
        {
            return result.Output;
        }

        var lines = new List<string>();
        
        if (result.WasCancelled)
        {
            lines.Add("Command was cancelled or timed out");
        }
        else
        {
            lines.Add($"Command failed with exit code {result.ExitCode}");
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            lines.Add("Error:");
            lines.Add(result.Error);
        }

        if (!string.IsNullOrEmpty(result.Output))
        {
            lines.Add("Output:");
            lines.Add(result.Output);
        }

        lines.Add($"Duration: {result.Duration.TotalMilliseconds:F0}ms");

        return string.Join(Environment.NewLine, lines);
    }
}