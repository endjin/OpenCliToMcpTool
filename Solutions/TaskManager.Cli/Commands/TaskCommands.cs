using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
using TaskManager.Cli.Models;
using TaskManager.Cli.Services;

namespace TaskManager.Cli.Commands;

public class TaskAddCommand : Command<TaskAddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Task title")]
        [CommandArgument(0, "<title>")]
        public string Title { get; set; } = string.Empty;

        [Description("Task description")]
        [CommandOption("-d|--description")]
        public string? Description { get; set; }

        [Description("Task priority (low, medium, high, critical)")]
        [CommandOption("-p|--priority")]
        public Priority Priority { get; set; } = Priority.Medium;

        [Description("Due date (YYYY-MM-DD or relative like 'tomorrow', 'next week')")]
        [CommandOption("--due")]
        public string? DueDate { get; set; }

        [Description("Assignee name")]
        [CommandOption("-a|--assignee")]
        public string? Assignee { get; set; }

        [Description("Project name")]
        [CommandOption("--project")]
        public string? Project { get; set; }

        [Description("Comma-separated tags")]
        [CommandOption("-t|--tags")]
        public string? Tags { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        
        TaskItem task = new()
        {
            Title = settings.Title,
            Description = settings.Description,
            Priority = settings.Priority,
            Assignee = settings.Assignee,
            Project = settings.Project,
            Tags = settings.Tags?.Split(',').Select(t => t.Trim()).ToList() ?? []
        };

        // Parse due date
        if (!string.IsNullOrEmpty(settings.DueDate))
        {
            if (DateTime.TryParse(settings.DueDate, out DateTime dueDate))
            {
                task.DueDate = dueDate;
            }
            else
            {
                // Handle relative dates
                task.DueDate = settings.DueDate.ToLower() switch
                {
                    "today" => DateTime.Today,
                    "tomorrow" => DateTime.Today.AddDays(1),
                    "next week" => DateTime.Today.AddDays(7),
                    "next month" => DateTime.Today.AddMonths(1),
                    _ => null
                };

                if (task.DueDate == null)
                {
                    AnsiConsole.MarkupLine("[red]Invalid due date format. Use YYYY-MM-DD or relative dates like 'tomorrow'.[/]");
                    return 1;
                }
            }
        }

        TaskItem created = service.AddTask(task);
        AnsiConsole.MarkupLine($"[green]✓[/] Task created with ID: [cyan]{created.Id}[/]");
        
        return 0;
    }
}

public class TaskUpdateCommand : Command<TaskUpdateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Task ID")]
        [CommandArgument(0, "<id>")]
        public int Id { get; set; }

        [Description("New title")]
        [CommandOption("--title")]
        public string? Title { get; set; }

        [Description("New description")]
        [CommandOption("-d|--description")]
        public string? Description { get; set; }

        [Description("New status (pending, in-progress, completed, cancelled)")]
        [CommandOption("-s|--status")]
        public TaskItemStatus? Status { get; set; }

        [Description("New priority (low, medium, high, critical)")]
        [CommandOption("-p|--priority")]
        public Priority? Priority { get; set; }

        [Description("New assignee")]
        [CommandOption("-a|--assignee")]
        public string? Assignee { get; set; }

        [Description("Clear assignee")]
        [CommandOption("--clear-assignee")]
        public bool ClearAssignee { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        TaskItem? task = service.GetTask(settings.Id);

        if (task == null)
        {
            AnsiConsole.MarkupLine($"[red]Task with ID {settings.Id} not found.[/]");
            return 1;
        }

        // Update fields
        if (!string.IsNullOrEmpty(settings.Title))
            task.Title = settings.Title;

        if (settings.Description != null)
            task.Description = settings.Description;

        if (settings.Status.HasValue)
            task.Status = settings.Status.Value;

        if (settings.Priority.HasValue)
            task.Priority = settings.Priority.Value;

        if (settings.ClearAssignee)
            task.Assignee = null;
        else if (!string.IsNullOrEmpty(settings.Assignee))
            task.Assignee = settings.Assignee;

        if (service.UpdateTask(task))
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Task {settings.Id} updated successfully.");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to update task.[/]");
        return 1;
    }
}

public class TaskDeleteCommand : Command<TaskDeleteCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Task ID")]
        [CommandArgument(0, "<id>")]
        public int Id { get; set; }

        [Description("Skip confirmation")]
        [CommandOption("-f|--force")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        TaskItem? task = service.GetTask(settings.Id);

        if (task == null)
        {
            AnsiConsole.MarkupLine($"[red]Task with ID {settings.Id} not found.[/]");
            return 1;
        }

        if (!settings.Force)
        {
            if (!AnsiConsole.Confirm($"Delete task '{task.Title}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
                return 0;
            }
        }

        if (service.DeleteTask(settings.Id))
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Task {settings.Id} deleted successfully.");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to delete task.[/]");
        return 1;
    }
}

public class TaskShowCommand : Command<TaskShowCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Task ID")]
        [CommandArgument(0, "<id>")]
        public int Id { get; set; }

        [Description("Output format (table, json)")]
        [CommandOption("-f|--format")]
        public string Format { get; set; } = "table";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        TaskItem? task = service.GetTask(settings.Id);

        if (task == null)
        {
            AnsiConsole.MarkupLine($"[red]Task with ID {settings.Id} not found.[/]");
            return 1;
        }

        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            string json = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(json);
        }
        else
        {
            Panel panel = new(new Rows(
                new Markup($"[bold]{Markup.Escape(task.Title)}[/]"),
                new Markup($"\n[dim]ID:[/] {task.Id}"),
                new Markup($"[dim]Status:[/] {GetStatusMarkup(task.Status)}"),
                new Markup($"[dim]Priority:[/] {GetPriorityMarkup(task.Priority)}"),
                new Markup($"[dim]Created:[/] {task.CreatedAt:yyyy-MM-dd HH:mm}"),
                new Markup($"[dim]Due:[/] {task.DueDate?.ToString("yyyy-MM-dd") ?? "Not set"}"),
                new Markup($"[dim]Assignee:[/] {task.Assignee ?? "Unassigned"}"),
                new Markup($"[dim]Project:[/] {task.Project ?? "None"}"),
                new Markup($"\n[dim]Description:[/]\n{Markup.Escape(task.Description ?? "No description")}"),
                new Markup($"\n[dim]Tags:[/] {(task.Tags.Any() ? string.Join(", ", task.Tags) : "None")}")
            ))
            {
                Header = new PanelHeader("Task Details"),
                Padding = new Padding(2),
                Border = BoxBorder.Rounded
            };

            AnsiConsole.Write(panel);
        }

        return 0;
    }

    private static string GetStatusMarkup(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Pending => "[yellow]Pending[/]",
        TaskItemStatus.InProgress => "[blue]In Progress[/]",
        TaskItemStatus.Completed => "[green]Completed[/]",
        TaskItemStatus.Cancelled => "[red]Cancelled[/]",
        _ => status.ToString()
    };

    private static string GetPriorityMarkup(Priority priority) => priority switch
    {
        Priority.Critical => "[red]Critical[/]",
        Priority.High => "[orange1]High[/]",
        Priority.Medium => "[yellow]Medium[/]",
        Priority.Low => "[grey]Low[/]",
        _ => priority.ToString()
    };
}