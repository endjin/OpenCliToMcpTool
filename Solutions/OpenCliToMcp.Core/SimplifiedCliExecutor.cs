using Microsoft.Extensions.Logging;

namespace OpenCliToMcp.Core;

/// <summary>
/// A simplified CLI executor that requires explicit executable paths.
/// </summary>
public class SimplifiedCliExecutor : ICliExecutor
{
    private readonly string executablePath;
    private readonly IProcessExecutor processExecutor;
    private readonly IResponseFormatter responseFormatter;
    private readonly ILogger<SimplifiedCliExecutor> logger;
    private readonly SimplifiedCliExecutorOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedCliExecutor"/> class.
    /// </summary>
    /// <param name="executablePath">The full path to the executable.</param>
    /// <param name="processExecutor">The process executor to use for executing commands.</param>
    /// <param name="responseFormatter">The response formatter to use for formatting results.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="options">The options for configuring the executor.</param>
    public SimplifiedCliExecutor(
        string executablePath,
        IProcessExecutor processExecutor,
        IResponseFormatter responseFormatter,
        ILogger<SimplifiedCliExecutor> logger,
        SimplifiedCliExecutorOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(executablePath);
        ArgumentNullException.ThrowIfNull(processExecutor);
        ArgumentNullException.ThrowIfNull(responseFormatter);
        ArgumentNullException.ThrowIfNull(logger);

        this.executablePath = executablePath;
        this.processExecutor = processExecutor;
        this.responseFormatter = responseFormatter;
        this.logger = logger;
        this.options = options ?? new SimplifiedCliExecutorOptions();
    }

    /// <summary>
    /// Gets the executable path used by this executor.
    /// </summary>
    public string ExecutablePath => executablePath;

    /// <inheritdoc/>
    public async Task<string> ExecuteAsync(string command, IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(command);
        ArgumentNullException.ThrowIfNull(arguments);

        ProcessRequest request = new()
        {
            FileName = executablePath,
            Arguments = arguments.ToArray(),
            WorkingDirectory = options.WorkingDirectory,
            EnvironmentVariables = options.EnvironmentVariables,
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            CancellationToken = cancellationToken
        };

        logger.LogDebug("Executing command '{Command}' with executable: {ExecutablePath}", command, executablePath);

        try
        {
            ProcessResult result = await processExecutor.ExecuteAsync(request);

            if (!result.Success && options.ThrowOnError)
            {
                var errorMessage = !string.IsNullOrEmpty(result.Error) ? result.Error : "Command execution failed";
                CliResponse cliResponse = new()
                {
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    Output = result.Output,
                    Error = result.Error
                };

                throw new CliExecutionException(errorMessage, cliResponse);
            }

            return responseFormatter.Format(result, options.ResponseFormat);
        }
        catch (CliExecutionException)
        {
            throw; // Re-throw CLI execution exceptions as-is
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing command '{Command}'", command);

            if (options.ThrowOnError)
            {
                CliResponse cliResponse = CliResponse.CreateError($"Unexpected error: {ex.Message}", -1);
                throw new CliExecutionException("Command execution failed with unexpected error", cliResponse, ex);
            }

            ProcessResult errorResult = new()
            {
                Success = false,
                ExitCode = -1,
                Output = string.Empty,
                Error = $"Unexpected error: {ex.Message}",
                Duration = TimeSpan.Zero
            };

            return responseFormatter.Format(errorResult, options.ResponseFormat);
        }
    }
}