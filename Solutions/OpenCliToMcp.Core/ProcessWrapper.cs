using System.Diagnostics;

namespace OpenCliToMcp.Core;

/// <summary>
/// Wrapper around System.Diagnostics.Process that implements IProcess.
/// </summary>
internal class ProcessWrapper : IProcess
{
    private readonly Process process;
    private bool disposed;

    public ProcessWrapper(Process process)
    {
        this.process = process ?? throw new ArgumentNullException(nameof(process));
    }

    public int ExitCode => process.ExitCode;
    
    public StreamReader StandardOutput => process.StandardOutput;
    
    public StreamReader StandardError => process.StandardError;

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return process.WaitForExitAsync(cancellationToken);
    }

    public void Kill()
    {
        process.Kill();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            process.Dispose();
            disposed = true;
        }
    }
}