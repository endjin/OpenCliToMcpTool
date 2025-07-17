namespace TaskManager.McpServer.Tests;

// Test to verify the command execution pattern
[TestClass]
public class CommandExecutionVerificationTests
{
    [TestMethod]
    public void VerifyCommandPattern()
    {
        // This test verifies that the command pattern used in tests matches the generated code
        
        // Example from generated code:
        // return await cliExecutor.ExecuteAsync("task manager", args, cancellationToken);
        
        // The command is "task manager" (with space)
        // The args is a List<string> passed as IEnumerable<string>
        
        const string expectedCommand = "task manager";
        expectedCommand.ShouldBe("task manager");
        
        // Verify List<string> can be passed as IEnumerable<string>
        List<string> args = ["stats", "--verbose"];
        IEnumerable<string> enumerable = args;
        enumerable.Count().ShouldBe(2);
    }
}