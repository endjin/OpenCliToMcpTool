using OpenCliToMcp.Generated;
using TaskManager.McpServer.Tests.TestHelpers;

namespace TaskManager.McpServer.Tests;

[TestClass]
public class TaskManagerToolMcpTests
{
    private ICliExecutor cliExecutor = null!;
    private CancellationToken cancellationToken;

    [TestInitialize]
    public void Setup()
    {
        cliExecutor = Substitute.For<ICliExecutor>();
        cancellationToken = CancellationToken.None;
    }

    #region Stats Command Tests

    [TestMethod]
    public async Task StatsAsync_WithMinimalParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Statistics output");

        // Act
        string result = await TaskManagerToolMcp.StatsAsync(cliExecutor, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Statistics output");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "stats")),
            cancellationToken);
    }

    [TestMethod]
    public async Task StatsAsync_WithAllParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Statistics with filters");

        // Act
        string result = await TaskManagerToolMcp.StatsAsync(
            cliExecutor,
            verbose: true,
            config: "/path/to/config.json",
            period: "week",
            project: "Website Redesign",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Statistics with filters");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyStatsArgs(args, true, "/path/to/config.json", "week", "Website Redesign")),
            cancellationToken);
    }

    #endregion

    #region Task Command Tests - New Tests for Missing Coverage

    [TestMethod]
    public async Task TaskAsync_WithMinimalParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task command help");

        // Act
        string result = await TaskManagerToolMcp.TaskAsync(cliExecutor, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task command help");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "task")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskAsync_WithVerboseAndConfig_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task command with options");

        // Act
        string result = await TaskManagerToolMcp.TaskAsync(
            cliExecutor,
            verbose: true,
            config: "/custom/config.json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task command with options");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyTaskArgs(args, true, "/custom/config.json")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskShowAsync_WithTaskId_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task details");

        // Act
        string result = await TaskManagerToolMcp.TaskShowAsync(
            cliExecutor,
            "456",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task details");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "show", "456")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskShowAsync_WithJsonFormat_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("{\"id\": 789, \"title\": \"Test task\"}");

        // Act
        string result = await TaskManagerToolMcp.TaskShowAsync(
            cliExecutor,
            "789",
            format: "json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("{\"id\": 789, \"title\": \"Test task\"}");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "show", "789", "--format", "json")),
            cancellationToken);
    }

    #endregion

    #region List Command Tests

    [TestMethod]
    public async Task ListAsync_WithMinimalParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task list output");

        // Act
        string result = await TaskManagerToolMcp.ListAsync(cliExecutor, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task list output");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "list")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ListAsync_WithFilterParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Filtered task list");

        // Act
        string result = await TaskManagerToolMcp.ListAsync(
            cliExecutor,
            status: "pending",
            priority: "high",
            assignee: "john",
            showCompleted: true,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Filtered task list");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyListArgs(args, false, null, "pending", "high", "john", null, null, true)),
            cancellationToken);
    }

    #endregion

    #region Task Add Command Tests

    [TestMethod]
    public async Task TaskAddAsync_WithRequiredTitle_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task created successfully");

        // Act
        string result = await TaskManagerToolMcp.TaskAddAsync(
            cliExecutor,
            "Complete project documentation",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task created successfully");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyTaskAddArgs(args, "Complete project documentation", false, null, null, null, null, null, null, null)),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskAddAsync_WithAllParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task created with all details");

        // Act
        string result = await TaskManagerToolMcp.TaskAddAsync(
            cliExecutor,
            "Review pull request",
            verbose: true,
            config: "/path/to/config.json",
            description: "Review and approve PR #123",
            priority: "high",
            due: "tomorrow",
            assignee: "jane",
            project: "API Development",
            tags: "review,urgent",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task created with all details");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyTaskAddArgs(args, "Review pull request", true, "/path/to/config.json", "Review and approve PR #123", "high", "tomorrow", "jane", "API Development", "review,urgent")),
            cancellationToken);
    }

    #endregion

    #region Task Update Command Tests

    [TestMethod]
    public async Task TaskUpdateAsync_WithTaskId_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task updated");

        // Act
        string result = await TaskManagerToolMcp.TaskUpdateAsync(
            cliExecutor,
            "123",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task updated");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "update", "123")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskUpdateAsync_WithStatusChange_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task status updated");

        // Act
        string result = await TaskManagerToolMcp.TaskUpdateAsync(
            cliExecutor,
            "123",
            status: "completed",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task status updated");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "update", "123", "--status", "completed")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskUpdateAsync_WithClearAssignee_IncludesFlag()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Assignee cleared");

        // Act
        string result = await TaskManagerToolMcp.TaskUpdateAsync(
            cliExecutor,
            "456",
            clearAssignee: true,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Assignee cleared");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "update", "456", "--clear-assignee")),
            cancellationToken);
    }

    #endregion

    #region Task Delete Command Tests

    [TestMethod]
    public async Task TaskDeleteAsync_WithTaskId_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task deleted");

        // Act
        string result = await TaskManagerToolMcp.TaskDeleteAsync(
            cliExecutor,
            "789",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task deleted");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "delete", "789")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskDeleteAsync_WithForceFlag_SkipsConfirmation()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task force deleted");

        // Act
        string result = await TaskManagerToolMcp.TaskDeleteAsync(
            cliExecutor,
            "789",
            force: true,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task force deleted");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "delete", "789", "--force")),
            cancellationToken);
    }

    #endregion

    #region Project Commands Tests - New Tests for Missing Coverage

    [TestMethod]
    public async Task ProjectAsync_WithMinimalParameters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project command help");

        // Act
        string result = await TaskManagerToolMcp.ProjectAsync(cliExecutor, cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project command help");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "project")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectAsync_WithVerboseAndConfig_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project command with options");

        // Act
        string result = await TaskManagerToolMcp.ProjectAsync(
            cliExecutor,
            verbose: true,
            config: "/project/config.json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project command with options");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "project", "--verbose", "--config", "/project/config.json")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectCreateAsync_WithName_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project created");

        // Act
        string result = await TaskManagerToolMcp.ProjectCreateAsync(
            cliExecutor,
            "Website Redesign",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project created");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "project", "create", "Website Redesign")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectArchiveAsync_WithProjectName_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project archived");

        // Act
        string result = await TaskManagerToolMcp.ProjectArchiveAsync(
            cliExecutor,
            "Old Project",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project archived");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "project", "archive", "Old Project")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectArchiveAsync_WithVerboseAndConfig_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project archived with options");

        // Act
        string result = await TaskManagerToolMcp.ProjectArchiveAsync(
            cliExecutor,
            "Legacy System",
            verbose: true,
            config: "/archive/config.json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project archived with options");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "project", "archive", "Legacy System", "--verbose", "--config", "/archive/config.json")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectListAsync_WithActiveFilter_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Active projects list");

        // Act
        string result = await TaskManagerToolMcp.ProjectListAsync(
            cliExecutor,
            active: true,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Active projects list");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyProjectListArgs(args, false, null, true)),
            cancellationToken);
    }

    #endregion

    #region Export Command Tests

    [TestMethod]
    public async Task ExportAsync_WithOutputPath_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Export completed");

        // Act
        string result = await TaskManagerToolMcp.ExportAsync(
            cliExecutor,
            "tasks.json",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Export completed");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "export", "tasks.json")),
            cancellationToken);
    }

    [TestMethod]
    public async Task ExportAsync_WithFormatAndFilters_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Export with format");

        // Act
        string result = await TaskManagerToolMcp.ExportAsync(
            cliExecutor,
            "report.md",
            format: "markdown",
            includeCompleted: true,
            project: "API Development",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Export with format");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "export", "report.md", "--format", "markdown", "--include-completed", "--project", "API Development")),
            cancellationToken);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task StatsAsync_WhenCliExecutorThrows_PropagatesException()
    {
        // Arrange
        InvalidOperationException expectedException = new("CLI not found");
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(expectedException));

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            TaskManagerToolMcp.StatsAsync(cliExecutor, cancellationToken: cancellationToken));
        
        exception.Message.ShouldBe("CLI not found");
    }

    [TestMethod]
    public async Task TaskAddAsync_WithCancellation_PassesCancellationToken()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Cancelled");

        // Act
        await TaskManagerToolMcp.TaskAddAsync(
            cliExecutor,
            "Test task",
            cancellationToken: cts.Token);

        // Assert
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Any<IEnumerable<string>>(),
            cts.Token);
    }

    #endregion

    #region Null and Empty Parameter Tests

    [TestMethod]
    public async Task ListAsync_WithNullParameters_OmitsFromCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("List output");

        // Act
        string result = await TaskManagerToolMcp.ListAsync(
            cliExecutor,
            status: null,
            priority: null,
            assignee: null,
            project: null,
            sort: null,
            cancellationToken: cancellationToken);

        // Assert
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyExactArgs(args, "list")),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskAddAsync_WithEmptyStrings_OmitsFromCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task added");

        // Act
        string result = await TaskManagerToolMcp.TaskAddAsync(
            cliExecutor,
            "Test task",
            description: "",
            priority: "",
            assignee: "",
            cancellationToken: cancellationToken);

        // Assert
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => 
                ArgumentVerifiers.VerifyExactArgs(args, "task", "add", "Test task") &&
                !ArgumentVerifiers.VerifyContainsArgs(args, "--description", "--priority", "--assignee")),
            cancellationToken);
    }

    #endregion

    #region Edge Case Tests - New Tests

    [TestMethod]
    public async Task TaskAddAsync_WithSpecialCharactersInTitle_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task created");

        string titleWithSpecialChars = "Task with 'quotes' and \"double quotes\" & special chars!";

        // Act
        string result = await TaskManagerToolMcp.TaskAddAsync(
            cliExecutor,
            titleWithSpecialChars,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task created");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "task", "add", titleWithSpecialChars)),
            cancellationToken);
    }

    [TestMethod]
    public async Task ProjectCreateAsync_WithLongProjectName_HandlesCorrectly()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Project created");

        string longProjectName = new string('A', 255); // Very long project name

        // Act
        string result = await TaskManagerToolMcp.ProjectCreateAsync(
            cliExecutor,
            longProjectName,
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Project created");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => ArgumentVerifiers.VerifyContainsArgs(args, "project", "create", longProjectName)),
            cancellationToken);
    }

    [TestMethod]
    public async Task TaskUpdateAsync_WithAllFieldsUpdated_BuildsCorrectCommand()
    {
        // Arrange
        cliExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns("Task fully updated");

        // Act
        string result = await TaskManagerToolMcp.TaskUpdateAsync(
            cliExecutor,
            "999",
            title: "Updated Title",
            description: "Updated Description",
            status: "in-progress",
            priority: "medium",
            assignee: "alice",
            cancellationToken: cancellationToken);

        // Assert
        result.ShouldBe("Task fully updated");
        await cliExecutor.Received(1).ExecuteAsync(
            "task manager",
            Arg.Is<IEnumerable<string>>(args => 
                ArgumentVerifiers.VerifyContainsArgs(args, "task", "update", "999") &&
                ArgumentVerifiers.VerifyContainsArgs(args, "--title", "Updated Title") &&
                ArgumentVerifiers.VerifyContainsArgs(args, "--description", "Updated Description") &&
                ArgumentVerifiers.VerifyContainsArgs(args, "--status", "in-progress") &&
                ArgumentVerifiers.VerifyContainsArgs(args, "--priority", "medium") &&
                ArgumentVerifiers.VerifyContainsArgs(args, "--assignee", "alice")),
            cancellationToken);
    }

    #endregion
}