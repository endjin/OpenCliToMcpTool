using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OpenCliToMcp.Core;

/// <summary>
/// Default implementation of <see cref="IProcessExecutor"/> that uses the system process factory.
/// </summary>
public class DefaultProcessExecutor : IProcessExecutor
{
    private readonly IProcessFactory processFactory;
    private readonly ILogger<DefaultProcessExecutor> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProcessExecutor"/> class.
    /// </summary>
    /// <param name="processFactory">The process factory to use for creating processes.</param>
    /// <param name="logger">The logger to use for logging process execution.</param>
    public DefaultProcessExecutor(IProcessFactory processFactory, ILogger<DefaultProcessExecutor> logger)
    {
        this.processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProcessResult> ExecuteAsync(ProcessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (string.IsNullOrEmpty(request.FileName))
        {
            throw new ArgumentException("FileName cannot be null or empty", nameof(request));
        }

        DateTime startTime = DateTime.UtcNow;
        
        try
        {
            ProcessStartInfo startInfo = CreateProcessStartInfo(request);
            
            logger.LogDebug("Executing process: {FileName} {Arguments}", 
                request.FileName, string.Join(" ", request.Arguments));

            using IProcess? process = processFactory.Start(startInfo);
            if (process == null)
            {
                var error = $"Failed to start process: {request.FileName}";
                logger.LogError(error);
                
                return new ProcessResult
                {
                    Success = false,
                    ExitCode = -1,
                    Output = string.Empty,
                    Error = error,
                    Duration = DateTime.UtcNow - startTime
                };
            }

            return await ExecuteProcessAsync(process, request, startTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute process: {FileName}", request.FileName);
            
            return new ProcessResult
            {
                Success = false,
                ExitCode = -1,
                Output = string.Empty,
                Error = $"Process execution failed: {ex.Message}",
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(ProcessRequest request)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = request.FileName,
            Arguments = string.Join(" ", request.Arguments.Select(EscapeArgument)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory
        };

        if (request.EnvironmentVariables != null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        return startInfo;
    }

    private async Task<ProcessResult> ExecuteProcessAsync(IProcess process, ProcessRequest request, DateTime startTime)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        cts.CancelAfter(request.Timeout);

        StringBuilder outputBuilder = new();
        StringBuilder errorBuilder = new();

        Task outputTask = ReadStreamAsync(process.StandardOutput, outputBuilder, cts.Token);
        Task errorTask = ReadStreamAsync(process.StandardError, errorBuilder, cts.Token);

        bool wasCancelled = false;
        
        try
        {
            await process.WaitForExitAsync(cts.Token);
            await Task.WhenAll(outputTask, errorTask);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            
            try 
            { 
                process.Kill(); 
            } 
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to kill process after cancellation");
            }
            
            var cancellationReason = request.CancellationToken.IsCancellationRequested
                ? "Process execution was cancelled"
                : $"Process timed out after {request.Timeout.TotalSeconds} seconds";
                
            logger.LogWarning(cancellationReason);
        }

        TimeSpan duration = DateTime.UtcNow - startTime;
        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (wasCancelled)
        {
            return new ProcessResult
            {
                Success = false,
                ExitCode = -1,
                Output = output,
                Error = error,
                WasCancelled = true,
                Duration = duration
            };
        }

        logger.LogDebug("Process completed with exit code: {ExitCode} in {Duration}ms", 
            process.ExitCode, duration.TotalMilliseconds);

        return new ProcessResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            Output = output,
            Error = error,
            WasCancelled = false,
            Duration = duration
        };
    }

    private static async Task ReadStreamAsync(StreamReader reader, StringBuilder builder, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        int bytesRead;

        while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            builder.Append(buffer, 0, bytesRead);
        }
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (!arg.Contains(' ') && !arg.Contains('"') && !arg.Contains('\\'))
            return arg;

        var escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}