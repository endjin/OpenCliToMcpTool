using OpenCliToMcp.Generated;
using System.Text.Json;

namespace TaskManager.McpServer.Tests;

[TestClass]
public class IntegrationTests
{
    private ICliExecutor mockCliExecutor = null!;

    [TestInitialize]
    public void Setup()
    {
        this.mockCliExecutor = Substitute.For<ICliExecutor>();
    }

    #region End-to-End Scenarios

    [TestMethod]
    public async Task CompleteTaskWorkflow_AddListUpdateDelete_Success()
    {
        // Scenario: Add a task, list it, update it, then delete it
        
        // Step 1: Add a new task
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "task" && args[1] == "add")
                {
                    return Task.FromResult("Task created successfully. ID: 123");
                }
                return Task.FromResult("Unknown command");
            });

        string addResult = await TaskManagerToolMcp.TaskAddAsync(
            this.mockCliExecutor,
            "Write integration tests",
            priority: "high",
            project: "Testing");

        addResult.ShouldContain("Task created successfully");
        addResult.ShouldContain("ID: 123");

        // Step 2: List tasks to verify it was added
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "list")
                {
                    return Task.FromResult(@"ID  | Title                    | Status  | Priority
123 | Write integration tests  | pending | high");
                }
                return Task.FromResult("Unknown command");
            });

        string listResult = await TaskManagerToolMcp.ListAsync(
            this.mockCliExecutor,
            project: "Testing");

        listResult.ShouldContain("Write integration tests");
        listResult.ShouldContain("123");
        listResult.ShouldContain("high");

        // Step 3: Update the task status
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "task" && args[1] == "update" && args.Contains("123"))
                {
                    return Task.FromResult("Task 123 updated successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string updateResult = await TaskManagerToolMcp.TaskUpdateAsync(
            this.mockCliExecutor,
            "123",
            status: "completed");

        updateResult.ShouldContain("Task 123 updated successfully");

        // Step 4: Delete the task
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "task" && args[1] == "delete" && args.Contains("123"))
                {
                    return Task.FromResult("Task 123 deleted successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string deleteResult = await TaskManagerToolMcp.TaskDeleteAsync(
            this.mockCliExecutor,
            "123",
            force: true);

        deleteResult.ShouldContain("Task 123 deleted successfully");
    }

    [TestMethod]
    public async Task ProjectManagement_CreateListArchive_Success()
    {
        // Scenario: Create a project, list projects, then archive it

        // Step 1: Create a new project
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "project" && args[1] == "create")
                {
                    return Task.FromResult("Project 'Test Project' created successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string createResult = await TaskManagerToolMcp.ProjectCreateAsync(
            this.mockCliExecutor,
            "Test Project",
            description: "A test project for integration testing",
            team: "QA");

        createResult.ShouldContain("Project 'Test Project' created successfully");

        // Step 2: List projects
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "project" && args[1] == "list")
                {
                    return Task.FromResult(@"Projects:
- Test Project (Active) - Team: QA
- Legacy Project (Archived)");
                }
                return Task.FromResult("Unknown command");
            });

        string listResult = await TaskManagerToolMcp.ProjectListAsync(
            this.mockCliExecutor,
            active: true);

        listResult.ShouldContain("Test Project");
        listResult.ShouldContain("QA");

        // Step 3: Archive the project
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args[0] == "project" && args[1] == "archive" && args.Contains("Test Project"))
                {
                    return Task.FromResult("Project 'Test Project' archived successfully");
                }
                return Task.FromResult("Unknown command");
            });

        string archiveResult = await TaskManagerToolMcp.ProjectArchiveAsync(
            this.mockCliExecutor,
            "Test Project");

        archiveResult.ShouldContain("Project 'Test Project' archived successfully");
    }

    #endregion

    #region Error Scenarios

    [TestMethod]
    public async Task TaskOperations_WithInvalidId_ReturnsError()
    {
        // Arrange
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args.Contains("999"))
                {
                    return Task.FromResult("Error: Task with ID 999 not found");
                }
                return Task.FromResult("Unknown command");
            });

        // Act
        string showResult = await TaskManagerToolMcp.TaskShowAsync(
            this.mockCliExecutor,
            "999");

        string updateResult = await TaskManagerToolMcp.TaskUpdateAsync(
            this.mockCliExecutor,
            "999",
            status: "completed");

        string deleteResult = await TaskManagerToolMcp.TaskDeleteAsync(
            this.mockCliExecutor,
            "999");

        // Assert
        showResult.ShouldContain("Error: Task with ID 999 not found");
        updateResult.ShouldContain("Error: Task with ID 999 not found");
        deleteResult.ShouldContain("Error: Task with ID 999 not found");
    }

    [TestMethod]
    public async Task Export_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args.Contains("--format") && args.Contains("invalid"))
                {
                    return Task.FromResult("Error: Unsupported export format 'invalid'. Supported formats: json, csv, markdown");
                }
                return Task.FromResult("Export successful");
            });

        // Act
        string result = await TaskManagerToolMcp.ExportAsync(
            this.mockCliExecutor,
            "output.txt",
            format: "invalid");

        // Assert
        result.ShouldContain("Error: Unsupported export format");
        result.ShouldContain("Supported formats: json, csv, markdown");
    }

    #endregion

    #region JSON Output Scenarios

    [TestMethod]
    public async Task TaskShow_WithJsonFormat_ParsesCorrectly()
    {
        // Arrange
        string taskJson = @"{
  ""id"": 123,
  ""title"": ""Complete documentation"",
  ""status"": ""in-progress"",
  ""priority"": ""high"",
  ""assignee"": ""john"",
  ""project"": ""Documentation"",
  ""created"": ""2024-01-15T10:00:00Z"",
  ""updated"": ""2024-01-16T14:30:00Z""
}";

        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                if (args.Contains("--format") && args.Contains("json"))
                {
                    return Task.FromResult(taskJson);
                }
                return Task.FromResult("Task details in default format");
            });

        // Act
        string result = await TaskManagerToolMcp.TaskShowAsync(
            this.mockCliExecutor,
            "123",
            format: "json");

        // Assert
        result.ShouldBe(taskJson);
        
        // Verify it's valid JSON
        JsonElement parsed = JsonSerializer.Deserialize<JsonElement>(result);
        parsed.GetProperty("id").GetInt32().ShouldBe(123);
        parsed.GetProperty("title").GetString().ShouldBe("Complete documentation");
        parsed.GetProperty("priority").GetString().ShouldBe("high");
    }

    #endregion

    #region Statistics Scenarios

    [TestMethod]
    public async Task Stats_WithDifferentPeriods_ReturnsAppropriateData()
    {
        // Arrange
        Dictionary<string, string> statsResponses = new Dictionary<string, string>
        {
            ["day"] = @"Statistics for past day:
- Tasks created: 5
- Tasks completed: 3
- Average completion time: 2.5 hours",
            ["week"] = @"Statistics for past week:
- Tasks created: 25
- Tasks completed: 18
- Average completion time: 1.2 days",
            ["month"] = @"Statistics for past month:
- Tasks created: 120
- Tasks completed: 95
- Average completion time: 3.4 days"
        };

        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                List<string> args = info.Arg<IEnumerable<string>>().ToList();
                int periodIndex = args.IndexOf("--period");
                if (periodIndex >= 0 && periodIndex + 1 < args.Count)
                {
                    string period = args[periodIndex + 1];
                    if (statsResponses.TryGetValue(period, out string? response))
                    {
                        return Task.FromResult(response);
                    }
                }
                return Task.FromResult("Statistics for all time");
            });

        // Act & Assert
        foreach (string period in new[] { "day", "week", "month" })
        {
            string result = await TaskManagerToolMcp.StatsAsync(
                this.mockCliExecutor,
                period: period);

            result.ShouldContain($"Statistics for past {period}");
            result.ShouldContain("Tasks created:");
            result.ShouldContain("Tasks completed:");
            result.ShouldContain("Average completion time:");
        }
    }

    #endregion

    #region Cancellation Scenarios

    [TestMethod]
    public async Task LongRunningOperation_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

        this.mockCliExecutor.ExecuteAsync("task manager", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(async info =>
            {
                CancellationToken token = info.Arg<CancellationToken>();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                    return "Should not reach here";
                }
                catch (OperationCanceledException)
                {
                    tcs.SetResult("Cancelled");
                    throw;
                }
            });

        // Act
        Task<string> exportTask = TaskManagerToolMcp.ExportAsync(
            this.mockCliExecutor,
            "large-export.json",
            includeCompleted: true,
            cancellationToken: cts.Token);

        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(exportTask);
        string cancellationResult = await tcs.Task;
        cancellationResult.ShouldBe("Cancelled");
    }

    #endregion
}