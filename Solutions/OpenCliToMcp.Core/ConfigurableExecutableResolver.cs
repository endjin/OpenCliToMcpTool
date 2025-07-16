using Microsoft.Extensions.Logging;

namespace OpenCliToMcp.Core;

/// <summary>
/// Implementation of <see cref="IExecutableResolver"/> that uses explicit configuration.
/// </summary>
public class ConfigurableExecutableResolver : IExecutableResolver
{
    private readonly Dictionary<string, string> executablePaths;
    private readonly bool throwOnNotFound;
    private readonly ILogger<ConfigurableExecutableResolver> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableExecutableResolver"/> class.
    /// </summary>
    /// <param name="options">The resolver options.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurableExecutableResolver(ExecutableResolverOptions options, ILogger<ConfigurableExecutableResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        executablePaths = new Dictionary<string, string>(options.ExecutablePaths, StringComparer.OrdinalIgnoreCase);
        throwOnNotFound = options.ThrowOnNotFound;
        this.logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableExecutableResolver"/> class.
    /// </summary>
    /// <param name="executablePaths">Dictionary of executable names to their full paths.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurableExecutableResolver(Dictionary<string, string> executablePaths, ILogger<ConfigurableExecutableResolver> logger)
        : this(new ExecutableResolverOptions { ExecutablePaths = executablePaths }, logger)
    {
    }

    /// <inheritdoc/>
    public Task<string?> ResolveExecutableAsync(string executableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        if (executablePaths.TryGetValue(executableName, out var path))
        {
            logger.LogDebug("Resolved executable '{ExecutableName}' to path: {Path}", executableName, path);
            return Task.FromResult<string?>(path);
        }

        logger.LogWarning("Executable '{ExecutableName}' not found in configuration", executableName);

        if (throwOnNotFound)
        {
            throw new ExecutableNotFoundException(executableName, 
                $"Executable '{executableName}' was not found in the configured paths. " +
                $"Available executables: {string.Join(", ", executablePaths.Keys)}");
        }

        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Adds or updates an executable path.
    /// </summary>
    /// <param name="executableName">The name of the executable.</param>
    /// <param name="path">The full path to the executable.</param>
    public void AddExecutable(string executableName, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);
        ArgumentException.ThrowIfNullOrEmpty(path);

        executablePaths[executableName] = path;
        logger.LogDebug("Added executable '{ExecutableName}' with path: {Path}", executableName, path);
    }

    /// <summary>
    /// Removes an executable from the configuration.
    /// </summary>
    /// <param name="executableName">The name of the executable to remove.</param>
    /// <returns>true if the executable was removed; false if it was not found.</returns>
    public bool RemoveExecutable(string executableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        var removed = executablePaths.Remove(executableName);
        if (removed)
        {
            logger.LogDebug("Removed executable '{ExecutableName}' from configuration", executableName);
        }
        return removed;
    }

    /// <summary>
    /// Gets all configured executable names.
    /// </summary>
    /// <returns>A collection of configured executable names.</returns>
    public IEnumerable<string> GetConfiguredExecutables()
    {
        return executablePaths.Keys;
    }
}