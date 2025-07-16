using System.Runtime.InteropServices;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;

namespace OpenCliToMcp.Core;

/// <summary>
/// Implementation of <see cref="IExecutableResolver"/> that uses globbing patterns to find executables.
/// </summary>
public class GlobbingExecutableResolver : IExecutableResolver
{
    private readonly GlobbingExecutableResolverOptions options;
    private readonly ILogger<GlobbingExecutableResolver> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobbingExecutableResolver"/> class.
    /// </summary>
    /// <param name="options">The resolver options.</param>
    /// <param name="logger">The logger.</param>
    public GlobbingExecutableResolver(GlobbingExecutableResolverOptions options, ILogger<GlobbingExecutableResolver> logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<string?> ResolveExecutableAsync(string executableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        logger.LogDebug("Resolving executable: {ExecutableName}", executableName);

        var searchPaths = GetSearchPaths();
        var patterns = GetPatternsForExecutable(executableName);

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                logger.LogDebug("Search path does not exist: {SearchPath}", searchPath);
                continue;
            }

            var result = SearchInDirectory(searchPath, patterns);
            if (result != null)
            {
                logger.LogDebug("Found executable '{ExecutableName}' at: {Path}", executableName, result);
                return Task.FromResult<string?>(result);
            }
        }

        logger.LogWarning("Executable '{ExecutableName}' not found in any search path", executableName);

        if (options.ThrowOnNotFound)
        {
            throw new ExecutableNotFoundException(executableName,
                $"Executable '{executableName}' was not found in any of the search paths: {string.Join(", ", searchPaths)}");
        }

        return Task.FromResult<string?>(null);
    }

    private string? SearchInDirectory(string searchPath, string[] patterns)
    {
        try
        {
            Matcher matcher = new();
            
            foreach (var pattern in patterns)
            {
                matcher.AddInclude(pattern);
            }

            DirectoryInfo directoryInfo = new(searchPath);
            PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));

            if (result.HasMatches)
            {
                FilePatternMatch firstMatch = result.Files.First();
                var fullPath = Path.Combine(searchPath, firstMatch.Path);
                
                // Verify the file actually exists and is executable
                if (File.Exists(fullPath) && IsExecutable(fullPath))
                {
                    return fullPath;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error searching in directory: {SearchPath}", searchPath);
        }

        return null;
    }

    private bool IsExecutable(string filePath)
    {
        try
        {
            FileInfo fileInfo = new(filePath);
            
            // Check if file exists and is not a directory
            if (!fileInfo.Exists || fileInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                return false;
            }

            // On Unix systems, check if file has execute permissions
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // This is a simplified check - in a real implementation, you might want to use
                // more sophisticated permission checking
                return true; // Assume executable for now
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking if file is executable: {FilePath}", filePath);
            return false;
        }
    }

    private string[] GetSearchPaths()
    {
        var searchPaths = new List<string>();

        // Add configured search paths
        if (options.SearchPaths.Length > 0)
        {
            searchPaths.AddRange(options.SearchPaths);
        }

        // Add common installation paths if enabled
        if (options.IncludeCommonPaths)
        {
            searchPaths.AddRange(GetCommonInstallationPaths());
        }

        // Add system PATH if enabled
        if (options.SearchInSystemPath)
        {
            searchPaths.AddRange(GetSystemPathDirectories());
        }

        return searchPaths.Distinct().ToArray();
    }

    private static string[] GetCommonInstallationPaths()
    {
        var paths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            paths.AddRange([
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                @"C:\Windows\System32",
                @"C:\Windows",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")
            ]);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            paths.AddRange([
                "/usr/local/bin",
                "/usr/bin",
                "/bin",
                "/opt",
                "/usr/local/sbin",
                "/usr/sbin",
                "/sbin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin")
            ]);
        }

        return paths.Where(Directory.Exists).ToArray();
    }

    private static string[] GetSystemPathDirectories()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return [];
        }

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        return pathVariable.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                          .Where(Directory.Exists)
                          .ToArray();
    }

    private string[] GetPatternsForExecutable(string executableName)
    {
        if (options.CustomPatterns != null && options.CustomPatterns.Length > 0)
        {
            return options.CustomPatterns.Select(pattern => pattern.Replace("{name}", executableName)).ToArray();
        }

        return GetDefaultPatternsForExecutable(executableName);
    }

    private static string[] GetDefaultPatternsForExecutable(string executableName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return
            [
                $"{executableName}.exe",
                $"{executableName}.cmd",
                $"{executableName}.bat",
                $"{executableName}.com",
                executableName
            ];
        }
        else
        {
            return
            [
                executableName,
                $"{executableName}.sh"
            ];
        }
    }
}