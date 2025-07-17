using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using TaskManager.Cli.Models;
using TaskManager.Cli.Services;

namespace TaskManager.Cli.Commands;

public class ExportCommand : Command<ExportCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Output file path")]
        [CommandArgument(0, "<output>")]
        public string OutputFile { get; set; } = string.Empty;

        [Description("Export format (json, csv, markdown)")]
        [CommandOption("-f|--format")]
        public string Format { get; set; } = "json";

        [Description("Include completed tasks")]
        [CommandOption("--include-completed")]
        public bool IncludeCompleted { get; set; }

        [Description("Filter by project")]
        [CommandOption("--project")]
        public string? Project { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        IEnumerable<TaskItem> tasks = service.GetAllTasks();

        // Apply filters
        if (!settings.IncludeCompleted)
            tasks = tasks.Where(t => t.Status != TaskItemStatus.Completed);

        if (!string.IsNullOrEmpty(settings.Project))
            tasks = tasks.Where(t => t.Project?.Equals(settings.Project, StringComparison.OrdinalIgnoreCase) == true);

        List<TaskItem> taskList = tasks.OrderBy(t => t.Id).ToList();

        if (!taskList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tasks to export.[/]");
            return 0;
        }

        try
        {
            switch (settings.Format.ToLower())
            {
                case "json":
                    ExportToJson(taskList, settings.OutputFile);
                    break;
                case "csv":
                    ExportToCsv(taskList, settings.OutputFile);
                    break;
                case "markdown":
                case "md":
                    ExportToMarkdown(taskList, settings.OutputFile);
                    break;
                default:
                    AnsiConsole.MarkupLine($"[red]Unknown format: {settings.Format}[/]");
                    return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Exported {taskList.Count} task(s) to [cyan]{settings.OutputFile}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Export failed: {ex.Message}[/]");
            return 1;
        }
    }

    private static void ExportToJson(List<TaskItem> tasks, string outputFile)
    {
        string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFile, json);
    }

    private static void ExportToCsv(List<TaskItem> tasks, string outputFile)
    {
        StringBuilder csv = new();
        csv.AppendLine("ID,Title,Description,Status,Priority,Assignee,Project,DueDate,CreatedAt,Tags");

        foreach (TaskItem task in tasks)
        {
            csv.AppendLine($"{task.Id}," +
                $"\"{EscapeCsv(task.Title)}\"," +
                $"\"{EscapeCsv(task.Description ?? "")}\"," +
                $"{task.Status}," +
                $"{task.Priority}," +
                $"\"{EscapeCsv(task.Assignee ?? "")}\"," +
                $"\"{EscapeCsv(task.Project ?? "")}\"," +
                $"{task.DueDate?.ToString("yyyy-MM-dd") ?? ""}," +
                $"{task.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                $"\"{string.Join(";", task.Tags)}\"");
        }

        File.WriteAllText(outputFile, csv.ToString());
    }

    private static void ExportToMarkdown(List<TaskItem> tasks, string outputFile)
    {
        StringBuilder md = new();
        md.AppendLine("# Task Export");
        md.AppendLine($"\nGenerated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"Total tasks: {tasks.Count}");
        md.AppendLine();

        // Group by status
        IEnumerable<IGrouping<TaskItemStatus, TaskItem>> groupedByStatus = tasks.GroupBy(t => t.Status);

        foreach (IGrouping<TaskItemStatus, TaskItem> group in groupedByStatus)
        {
            md.AppendLine($"## {group.Key} Tasks");
            md.AppendLine();
            md.AppendLine("| ID | Title | Priority | Assignee | Due Date |");
            md.AppendLine("|----|-------|----------|----------|----------|");

            foreach (TaskItem task in group.OrderByDescending(t => t.Priority))
            {
                md.AppendLine($"| {task.Id} | {task.Title} | {task.Priority} | {task.Assignee ?? "-"} | {task.DueDate?.ToString("yyyy-MM-dd") ?? "-"} |");
            }

            md.AppendLine();
        }

        // Add detailed task list
        md.AppendLine("## Task Details");
        md.AppendLine();

        foreach (TaskItem task in tasks)
        {
            md.AppendLine($"### Task #{task.Id}: {task.Title}");
            md.AppendLine();
            md.AppendLine($"- **Status**: {task.Status}");
            md.AppendLine($"- **Priority**: {task.Priority}");
            md.AppendLine($"- **Created**: {task.CreatedAt:yyyy-MM-dd HH:mm}");
            md.AppendLine($"- **Due**: {task.DueDate?.ToString("yyyy-MM-dd") ?? "Not set"}");
            md.AppendLine($"- **Assignee**: {task.Assignee ?? "Unassigned"}");
            md.AppendLine($"- **Project**: {task.Project ?? "None"}");
            md.AppendLine($"- **Tags**: {(task.Tags.Any() ? string.Join(", ", task.Tags) : "None")}");
            
            if (!string.IsNullOrEmpty(task.Description))
            {
                md.AppendLine();
                md.AppendLine("**Description**:");
                md.AppendLine(task.Description);
            }

            md.AppendLine();
            md.AppendLine("---");
            md.AppendLine();
        }

        File.WriteAllText(outputFile, md.ToString());
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}