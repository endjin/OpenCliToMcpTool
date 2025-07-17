using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenCliToMcp.Core;

/// <summary>
/// Represents the response from a CLI command execution.
/// </summary>
public record CliResponse
{
    /// <summary>
    /// Indicates whether the command executed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// The exit code returned by the command.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public required int ExitCode { get; init; }

    /// <summary>
    /// The standard output from the command.
    /// </summary>
    [JsonPropertyName("output")]
    public required string Output { get; init; }

    /// <summary>
    /// The standard error output from the command.
    /// </summary>
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    /// <summary>
    /// The timestamp when the command was executed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the execution.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static CliResponse CreateSuccess(string output, int exitCode = 0) =>
        new()
        {
            Success = true,
            ExitCode = exitCode,
            Output = output.TrimEnd(),
            Error = string.Empty
        };

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static CliResponse CreateError(string error, int exitCode = -1, string output = "") =>
        new()
        {
            Success = false,
            ExitCode = exitCode,
            Output = output.TrimEnd(),
            Error = error.TrimEnd()
        };

    /// <summary>
    /// Serializes the response to JSON.
    /// </summary>
    public string ToJson(bool writeIndented = true) =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions 
        { 
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
}