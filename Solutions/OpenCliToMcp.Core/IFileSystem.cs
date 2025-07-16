namespace OpenCliToMcp.Core;

/// <summary>
/// Abstraction for file system operations to enable unit testing.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory path.</returns>
    string GetCurrentDirectory();
}