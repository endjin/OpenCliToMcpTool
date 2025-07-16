namespace OpenCliToMcp.Core;

/// <summary>
/// Defines the contract for resolving executable paths.
/// </summary>
public interface IExecutableResolver
{
    /// <summary>
    /// Resolves the full path to an executable.
    /// </summary>
    /// <param name="executableName">The name of the executable to resolve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the full path to the executable, or null if not found.</returns>
    Task<string?> ResolveExecutableAsync(string executableName);
}