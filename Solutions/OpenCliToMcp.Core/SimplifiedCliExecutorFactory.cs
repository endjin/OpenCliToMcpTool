using Microsoft.Extensions.Logging;

namespace OpenCliToMcp.Core;

/// <summary>
/// Factory for creating simplified CLI executors.
/// </summary>
public class SimplifiedCliExecutorFactory
{
    private readonly IExecutableResolver executableResolver;
    private readonly IProcessExecutor processExecutor;
    private readonly IResponseFormatter responseFormatter;
    private readonly ILogger<SimplifiedCliExecutor> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedCliExecutorFactory"/> class.
    /// </summary>
    /// <param name="executableResolver">The executable resolver to use for finding executables.</param>
    /// <param name="processExecutor">The process executor to use for executing commands.</param>
    /// <param name="responseFormatter">The response formatter to use for formatting results.</param>
    /// <param name="logger">The logger to use for logging.</param>
    public SimplifiedCliExecutorFactory(
        IExecutableResolver executableResolver,
        IProcessExecutor processExecutor,
        IResponseFormatter responseFormatter,
        ILogger<SimplifiedCliExecutor> logger)
    {
        this.executableResolver = executableResolver ?? throw new ArgumentNullException(nameof(executableResolver));
        this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        this.responseFormatter = responseFormatter ?? throw new ArgumentNullException(nameof(responseFormatter));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a simplified CLI executor for the specified executable.
    /// </summary>
    /// <param name="executableName">The name of the executable to create an executor for.</param>
    /// <param name="options">The options for configuring the executor.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created executor.</returns>
    /// <exception cref="ExecutableNotFoundException">Thrown when the executable cannot be found.</exception>
    public async Task<SimplifiedCliExecutor> CreateExecutorAsync(string executableName, SimplifiedCliExecutorOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        var executablePath = await executableResolver.ResolveExecutableAsync(executableName);
        
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new ExecutableNotFoundException(executableName, $"Executable '{executableName}' could not be resolved to a valid path");
        }

        return new SimplifiedCliExecutor(executablePath, processExecutor, responseFormatter, logger, options);
    }

    /// <summary>
    /// Creates a simplified CLI executor with an explicit executable path.
    /// </summary>
    /// <param name="executablePath">The full path to the executable.</param>
    /// <param name="options">The options for configuring the executor.</param>
    /// <returns>The created executor.</returns>
    public SimplifiedCliExecutor CreateExecutor(string executablePath, SimplifiedCliExecutorOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(executablePath);

        return new SimplifiedCliExecutor(executablePath, processExecutor, responseFormatter, logger, options);
    }
}