namespace OpenCliToMcp.Core;

/// <summary>
/// Options for configuring the globbing executable resolver.
/// </summary>
public record GlobbingExecutableResolverOptions
{
    /// <summary>
    /// The search paths to use for finding executables.
    /// </summary>
    public string[] SearchPaths { get; init; } = [];

    /// <summary>
    /// Whether to search in the system PATH environment variable.
    /// </summary>
    public bool SearchInSystemPath { get; init; } = true;

    /// <summary>
    /// Whether to include common installation paths based on the operating system.
    /// </summary>
    public bool IncludeCommonPaths { get; init; } = true;

    /// <summary>
    /// Custom file patterns to use for matching executables.
    /// If not specified, platform-specific patterns will be used.
    /// </summary>
    public string[]? CustomPatterns { get; init; }

    /// <summary>
    /// Whether to throw an exception if an executable is not found.
    /// </summary>
    public bool ThrowOnNotFound { get; init; } = true;
}