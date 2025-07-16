using System.Diagnostics;
using System.Text;

namespace OpenCliToMcp.Core.Tests.Mocks;

public class MockProcess : IProcess
{
    private readonly MemoryStream outputStream = new();
    private readonly MemoryStream errorStream = new();
    private readonly StreamReader outputReader;
    private readonly StreamReader errorReader;
    private readonly StreamWriter outputWriter;
    private readonly StreamWriter errorWriter;
    private bool disposed;
    private readonly TaskCompletionSource<bool> waitForExitTcs = new();
    
    public MockProcess()
    {
        outputWriter = new StreamWriter(outputStream);
        errorWriter = new StreamWriter(errorStream);
        
        outputStream.Position = 0;
        errorStream.Position = 0;
        
        outputReader = new StreamReader(outputStream);
        errorReader = new StreamReader(errorStream);
    }
    
    public int ExitCode { get; set; }
    
    public StreamReader StandardOutput => outputReader;
    
    public StreamReader StandardError => errorReader;
    
    public bool WasKilled { get; private set; }
    
    public void SetOutput(string output)
    {
        outputWriter.Write(output);
        outputWriter.Flush();
        outputStream.Position = 0;
    }
    
    public void SetError(string error)
    {
        errorWriter.Write(error);
        errorWriter.Flush();
        errorStream.Position = 0;
    }
    
    public void CompleteExecution()
    {
        waitForExitTcs.TrySetResult(true);
    }
    
    public void SimulateTimeout()
    {
        waitForExitTcs.TrySetCanceled();
    }
    
    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => waitForExitTcs.TrySetCanceled());
        return waitForExitTcs.Task;
    }
    
    public void Kill()
    {
        WasKilled = true;
        waitForExitTcs.TrySetCanceled();
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            outputWriter?.Dispose();
            errorWriter?.Dispose();
            outputReader?.Dispose();
            errorReader?.Dispose();
            outputStream?.Dispose();
            errorStream?.Dispose();
            disposed = true;
        }
    }
}

public class MockProcessFactory : IProcessFactory
{
    private readonly Queue<MockProcess> processQueue = new();
    private readonly List<ProcessStartInfo> startInfoHistory = [];
    
    public bool ReturnNullProcess { get; set; }
    
    public IReadOnlyList<ProcessStartInfo> StartInfoHistory => startInfoHistory;
    
    public void EnqueueProcess(MockProcess process)
    {
        processQueue.Enqueue(process);
    }
    
    public MockProcess CreateProcess(string output = "", string error = "", int exitCode = 0, bool autoComplete = true)
    {
        MockProcess process = new()
        {
            ExitCode = exitCode
        };
        
        if (!string.IsNullOrEmpty(output))
            process.SetOutput(output);
            
        if (!string.IsNullOrEmpty(error))
            process.SetError(error);
            
        if (autoComplete)
            process.CompleteExecution();
            
        return process;
    }
    
    public IProcess? Start(ProcessStartInfo startInfo)
    {
        startInfoHistory.Add(startInfo);
        
        if (ReturnNullProcess)
            return null;
            
        if (processQueue.Count > 0)
            return processQueue.Dequeue();
            
        // Default process returns empty output with exit code 0
        return CreateProcess(autoComplete: true);
    }
}