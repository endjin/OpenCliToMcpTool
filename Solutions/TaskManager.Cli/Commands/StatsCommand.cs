using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TaskManager.Cli.Models;
using TaskManager.Cli.Services;

namespace TaskManager.Cli.Commands;

public class StatsCommand : Command<StatsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Time period (today, week, month, all)")]
        [CommandOption("-p|--period")]
        public string Period { get; set; } = "all";

        [Description("Filter by project")]
        [CommandOption("--project")]
        public string? Project { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        TaskService service = new();
        IEnumerable<TaskItem> tasks = service.GetAllTasks();

        // Apply time filter
        DateTime startDate = settings.Period.ToLower() switch
        {
            "today" => DateTime.Today,
            "week" => DateTime.Today.AddDays(-7),
            "month" => DateTime.Today.AddMonths(-1),
            _ => DateTime.MinValue
        };

        if (startDate != DateTime.MinValue)
            tasks = tasks.Where(t => t.CreatedAt >= startDate);

        // Apply project filter
        if (!string.IsNullOrEmpty(settings.Project))
            tasks = tasks.Where(t => t.Project?.Equals(settings.Project, StringComparison.OrdinalIgnoreCase) == true);

        List<TaskItem> taskList = tasks.ToList();

        if (!taskList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tasks found for the specified criteria.[/]");
            return 0;
        }

        // Calculate statistics
        int totalTasks = taskList.Count;
        Dictionary<TaskItemStatus, int> byStatus = taskList.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
        Dictionary<Priority, int> byPriority = taskList.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count());
        int overdueTasks = taskList.Count(t => t.DueDate < DateTime.Now && t.Status != TaskItemStatus.Completed);
        int assignedTasks = taskList.Count(t => !string.IsNullOrEmpty(t.Assignee));

        // Display statistics
        Panel panel = new(new Rows(
            new Markup($"[bold]Task Statistics[/] - {settings.Period}"),
            new Markup($"\n[dim]Total Tasks:[/] {totalTasks}"),
            new Rule(),
            new Markup("[bold]By Status:[/]"),
            new Markup($"  [yellow]Pending:[/] {byStatus.GetValueOrDefault(TaskItemStatus.Pending, 0)}"),
            new Markup($"  [blue]In Progress:[/] {byStatus.GetValueOrDefault(TaskItemStatus.InProgress, 0)}"),
            new Markup($"  [green]Completed:[/] {byStatus.GetValueOrDefault(TaskItemStatus.Completed, 0)}"),
            new Markup($"  [red]Cancelled:[/] {byStatus.GetValueOrDefault(TaskItemStatus.Cancelled, 0)}"),
            new Rule(),
            new Markup("[bold]By Priority:[/]"),
            new Markup($"  [red]Critical:[/] {byPriority.GetValueOrDefault(Priority.Critical, 0)}"),
            new Markup($"  [orange1]High:[/] {byPriority.GetValueOrDefault(Priority.High, 0)}"),
            new Markup($"  [yellow]Medium:[/] {byPriority.GetValueOrDefault(Priority.Medium, 0)}"),
            new Markup($"  [grey]Low:[/] {byPriority.GetValueOrDefault(Priority.Low, 0)}"),
            new Rule(),
            new Markup($"[dim]Overdue Tasks:[/] {(overdueTasks > 0 ? $"[red]{overdueTasks}[/]" : "0")}"),
            new Markup($"[dim]Assigned Tasks:[/] {assignedTasks}")
        ))
        {
            Header = new PanelHeader("Statistics"),
            Padding = new Padding(2),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        // Show completion rate chart
        if (totalTasks > 0)
        {
            double completionRate = (double)byStatus.GetValueOrDefault(TaskItemStatus.Completed, 0) / totalTasks * 100;
            
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new BarChart()
                .Width(60)
                .Label("[bold]Completion Rate[/]")
                .AddItem("Completed", completionRate, Color.Green)
                .AddItem("Incomplete", 100 - completionRate, Color.Grey));
        }

        return 0;
    }
}