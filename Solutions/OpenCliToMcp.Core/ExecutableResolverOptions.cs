namespace OpenCliToMcp.Core;

/// <summary>
/// Options for configuring executable paths.
/// </summary>
public record ExecutableResolverOptions
{
    /// <summary>
    /// Dictionary of executable names to their full paths.
    /// </summary>
    public Dictionary<string, string> ExecutablePaths { get; init; } = new();

    /// <summary>
    /// Whether to throw an exception if an executable is not found.
    /// </summary>
    public bool ThrowOnNotFound { get; init; } = true;
}