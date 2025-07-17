using System.Diagnostics;

namespace OpenCliToMcp.Core;

/// <summary>
/// Default implementation of IProcessFactory that creates real system processes.
/// </summary>
public class DefaultProcessFactory : IProcessFactory
{
    /// <inheritdoc/>
    public IProcess? Start(ProcessStartInfo startInfo)
    {
        Process? process = Process.Start(startInfo);
        return process != null ? new ProcessWrapper(process) : null;
    }
}