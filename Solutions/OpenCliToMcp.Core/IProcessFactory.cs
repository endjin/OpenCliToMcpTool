using System.Diagnostics;

namespace OpenCliToMcp.Core;

/// <summary>
/// Factory for creating process instances.
/// </summary>
public interface IProcessFactory
{
    /// <summary>
    /// Starts a process with the specified start information.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The started process, or null if the process could not be started.</returns>
    IProcess? Start(ProcessStartInfo startInfo);
}