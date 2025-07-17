namespace OpenCliToMcp.Core;

/// <summary>
/// Default implementation of IFileSystem that uses the real file system.
/// </summary>
public class DefaultFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc/>
    public string GetCurrentDirectory() => Environment.CurrentDirectory;
}