namespace TaskManager.McpServer.Tests.Mocks;

/// <summary>
/// Mock implementation of task manager CLI for testing various scenarios
/// </summary>
public class MockTaskManagerCli : ICliExecutor
{
    private readonly Dictionary<string, Func<List<string>, string>> commandHandlers;
    private readonly List<(string command, List<string> args, DateTime timestamp)> commandHistory;
    private int nextTaskId = 100;

    public MockTaskManagerCli()
    {
        this.commandHandlers = new Dictionary<string, Func<List<string>, string>>();
        this.commandHistory = [];
        InitializeCommandHandlers();
    }

    public IReadOnlyList<(string command, List<string> args, DateTime timestamp)> CommandHistory => this.commandHistory;

    public async Task<string> ExecuteAsync(string command, IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        List<string> args = arguments.ToList();
        
        // Record command for history
        this.commandHistory.Add((command, [..args], DateTime.UtcNow));

        // Simulate async operation
        await Task.Delay(10, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        // Handle commands
        if (args.Count == 0)
        {
            return "Error: No command specified";
        }

        string mainCommand = args[0];
        if (this.commandHandlers.TryGetValue(mainCommand, out Func<List<string>, string>? handler))
        {
            return handler(args);
        }

        return $"Error: Unknown command '{mainCommand}'";
    }

    private void InitializeCommandHandlers()
    {
        // Stats command
        this.commandHandlers["stats"] = args =>
        {
            string period = GetOptionValue(args, "--period") ?? "all-time";
            string? project = GetOptionValue(args, "--project");

            string stats = period switch
            {
                "day" => "Today: 5 tasks created, 3 completed",
                "week" => "This week: 25 tasks created, 18 completed",
                "month" => "This month: 120 tasks created, 95 completed",
                _ => "All time: 1523 tasks created, 1289 completed"
            };

            if (!string.IsNullOrEmpty(project))
            {
                stats += $" (Project: {project})";
            }

            return stats;
        };

        // List command
        this.commandHandlers["list"] = args =>
        {
            string? status = GetOptionValue(args, "--status");
            string? priority = GetOptionValue(args, "--priority");
            
            List<string> tasks =
            [
                "ID   | Title                        | Status    | Priority",
                "---- | ---------------------------- | --------- | --------"
            ];

            // Add sample tasks based on filters
            if (status == null || status == "pending")
            {
                tasks.Add("101  | Complete documentation       | pending   | high");
                tasks.Add("102  | Review pull requests         | pending   | medium");
            }

            if (status == null || status == "in-progress")
            {
                tasks.Add("103  | Implement new feature        | in-progress | high");
            }

            if (args.Contains("--show-completed") && (status == null || status == "completed"))
            {
                tasks.Add("104  | Fix bug in login            | completed | high");
            }

            return string.Join("\n", tasks);
        };

        // Task command
        this.commandHandlers["task"] = args =>
        {
            if (args.Count < 2)
            {
                return "Error: Task subcommand required (add, update, delete, show)";
            }

            return args[1] switch
            {
                "add" => HandleTaskAdd(args),
                "update" => HandleTaskUpdate(args),
                "delete" => HandleTaskDelete(args),
                "show" => HandleTaskShow(args),
                _ => $"Error: Unknown task subcommand '{args[1]}'"
            };
        };

        // Project command
        this.commandHandlers["project"] = args =>
        {
            if (args.Count < 2)
            {
                return "Error: Project subcommand required (create, list, archive)";
            }

            return args[1] switch
            {
                "create" => HandleProjectCreate(args),
                "list" => HandleProjectList(args),
                "archive" => HandleProjectArchive(args),
                _ => $"Error: Unknown project subcommand '{args[1]}'"
            };
        };

        // Export command
        this.commandHandlers["export"] = args =>
        {
            string format = GetOptionValue(args, "--format") ?? "json";
            string outputFile = args.LastOrDefault() ?? "export.json";

            if (!new[] { "json", "csv", "markdown" }.Contains(format))
            {
                return $"Error: Unsupported export format '{format}'. Supported formats: json, csv, markdown";
            }

            return $"Export completed successfully to {outputFile} in {format} format";
        };
    }

    private string HandleTaskAdd(List<string> args)
    {
        string? title = args.LastOrDefault();
        if (string.IsNullOrEmpty(title) || title.StartsWith("--"))
        {
            return "Error: Task title is required";
        }

        int id = ++this.nextTaskId;
        string priority = GetOptionValue(args, "--priority") ?? "medium";
        string? project = GetOptionValue(args, "--project");

        string response = $"Task created successfully. ID: {id}";
        if (!string.IsNullOrEmpty(project))
        {
            response += $" (Project: {project})";
        }

        return response;
    }

    private string HandleTaskUpdate(List<string> args)
    {
        string? id = args.LastOrDefault();
        if (string.IsNullOrEmpty(id) || id.StartsWith("--"))
        {
            return "Error: Task ID is required";
        }

        if (!int.TryParse(id, out _))
        {
            return $"Error: Invalid task ID '{id}'";
        }

        List<string> updates = [];
        if (GetOptionValue(args, "--status") is string status)
            updates.Add($"status={status}");
        if (GetOptionValue(args, "--priority") is string priority)
            updates.Add($"priority={priority}");
        if (args.Contains("--clear-assignee"))
            updates.Add("assignee=<cleared>");

        if (updates.Count == 0)
        {
            return "Warning: No updates specified";
        }

        return $"Task {id} updated successfully. Changes: {string.Join(", ", updates)}";
    }

    private string HandleTaskDelete(List<string> args)
    {
        string? id = args.LastOrDefault();
        if (string.IsNullOrEmpty(id) || id.StartsWith("--"))
        {
            return "Error: Task ID is required";
        }

        if (!int.TryParse(id, out _))
        {
            return $"Error: Invalid task ID '{id}'";
        }

        if (id == "999")
        {
            return "Error: Task with ID 999 not found";
        }

        bool force = args.Contains("--force");
        return force
            ? $"Task {id} deleted successfully"
            : $"Task {id} deletion requires confirmation (use --force to skip)";
    }

    private string HandleTaskShow(List<string> args)
    {
        string? id = args.LastOrDefault();
        if (string.IsNullOrEmpty(id) || id.StartsWith("--"))
        {
            return "Error: Task ID is required";
        }

        if (id == "999")
        {
            return "Error: Task with ID 999 not found";
        }

        string? format = GetOptionValue(args, "--format");
        if (format == "json")
        {
            return @"{
  ""id"": " + id + @",
  ""title"": ""Sample task"",
  ""status"": ""pending"",
  ""priority"": ""medium"",
  ""created"": ""2024-01-15T10:00:00Z""
}";
        }

        return $@"Task Details:
ID: {id}
Title: Sample task
Status: pending
Priority: medium
Created: 2024-01-15 10:00:00";
    }

    private string HandleProjectCreate(List<string> args)
    {
        string? name = args.LastOrDefault();
        if (string.IsNullOrEmpty(name) || name.StartsWith("--"))
        {
            return "Error: Project name is required";
        }

        string? team = GetOptionValue(args, "--team");
        string response = $"Project '{name}' created successfully";
        if (!string.IsNullOrEmpty(team))
        {
            response += $" (Team: {team})";
        }

        return response;
    }

    private string HandleProjectList(List<string> args)
    {
        bool active = args.Contains("--active");
        
        List<string> projects =
        [
            "Projects:",
            "- Active Project 1 (Active)",
            "- Development Sprint (Active) - Team: Dev"

        ];

        if (!active)
        {
            projects.Add("- Old Project (Archived)");
            projects.Add("- Legacy System (Archived)");
        }

        return string.Join("\n", projects);
    }

    private string HandleProjectArchive(List<string> args)
    {
        string? name = args.LastOrDefault();
        if (string.IsNullOrEmpty(name) || name.StartsWith("--"))
        {
            return "Error: Project name is required";
        }

        return $"Project '{name}' archived successfully";
    }

    private static string? GetOptionValue(List<string> args, string option)
    {
        int index = args.IndexOf(option);
        if (index >= 0 && index + 1 < args.Count)
        {
            string value = args[index + 1];
            if (!value.StartsWith("--"))
            {
                return value;
            }
        }
        return null;
    }
}