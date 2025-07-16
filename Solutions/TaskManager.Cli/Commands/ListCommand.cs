using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TaskManager.Cli.Models;
using TaskManager.Cli.Services;

namespace TaskManager.Cli.Commands;

public class ListCommand : Command<ListCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Filter by task status")]
        [CommandOption("-s|--status")]
        public TaskItemStatus? Status { get; set; }

        [Description("Filter by priority")]
        [CommandOption("-p|--priority")]
        public Priority? Priority { get; set; }

        [Description("Filter by assignee")]
        [CommandOption("-a|--assignee")]
        public string? Assignee { get; set; }

        [Description("Filter by project")]
        [CommandOption("--project")]
        public string? Project { get; set; }

        [Description("Sort tasks by field (id, title, priority, status, due)")]
        [CommandOption("--sort")]
        public string? SortBy { get; set; }

        [Description("Show completed tasks")]
        [CommandOption("--show-completed")]
        public bool ShowCompleted { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        IEnumerable<TaskItem> tasks = service.GetAllTasks();

        // Apply filters
        if (settings.Status.HasValue)
            tasks = tasks.Where(t => t.Status == settings.Status.Value);

        if (settings.Priority.HasValue)
            tasks = tasks.Where(t => t.Priority == settings.Priority.Value);

        if (!string.IsNullOrEmpty(settings.Assignee))
            tasks = tasks.Where(t => t.Assignee?.Contains(settings.Assignee, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrEmpty(settings.Project))
            tasks = tasks.Where(t => t.Project?.Contains(settings.Project, StringComparison.OrdinalIgnoreCase) == true);

        if (!settings.ShowCompleted)
            tasks = tasks.Where(t => t.Status != TaskItemStatus.Completed);

        // Apply sorting
        tasks = settings.SortBy?.ToLower() switch
        {
            "id" => tasks.OrderBy(t => t.Id),
            "title" => tasks.OrderBy(t => t.Title),
            "priority" => tasks.OrderByDescending(t => t.Priority),
            "status" => tasks.OrderBy(t => t.Status),
            "due" => tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
            _ => tasks.OrderBy(t => t.Id)
        };

        List<TaskItem> taskList = tasks.ToList();

        if (!taskList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tasks found matching the criteria.[/]");
            return 0;
        }

        // Create table
        Table table = new();
        table.AddColumn("ID");
        table.AddColumn("Title");
        table.AddColumn("Status");
        table.AddColumn("Priority");
        table.AddColumn("Assignee");
        table.AddColumn("Due Date");

        foreach (TaskItem task in taskList)
        {
            string statusColor = task.Status switch
            {
                TaskItemStatus.Pending => "yellow",
                TaskItemStatus.InProgress => "blue",
                TaskItemStatus.Completed => "green",
                TaskItemStatus.Cancelled => "red",
                _ => "white"
            };

            string priorityColor = task.Priority switch
            {
                Priority.Critical => "red",
                Priority.High => "orange1",
                Priority.Medium => "yellow",
                Priority.Low => "grey",
                _ => "white"
            };

            table.AddRow(
                task.Id.ToString(),
                Markup.Escape(task.Title),
                $"[{statusColor}]{task.Status}[/]",
                $"[{priorityColor}]{task.Priority}[/]",
                task.Assignee ?? "-",
                task.DueDate?.ToString("yyyy-MM-dd") ?? "-"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {taskList.Count} task(s)[/]");

        return 0;
    }
}