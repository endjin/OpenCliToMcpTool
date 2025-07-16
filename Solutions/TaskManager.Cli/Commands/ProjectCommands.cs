using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TaskManager.Cli.Models;
using TaskManager.Cli.Services;

namespace TaskManager.Cli.Commands;

public class ProjectListCommand : Command<ProjectListCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Show only active projects")]
        [CommandOption("--active")]
        public bool ActiveOnly { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        ProjectService service = new();
        IEnumerable<Project> projects = service.GetAllProjects();

        if (settings.ActiveOnly)
            projects = projects.Where(p => p.IsActive);

        List<Project> projectList = projects.ToList();

        if (!projectList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No projects found.[/]");
            return 0;
        }

        Table table = new();
        table.AddColumn("Name");
        table.AddColumn("Team");
        table.AddColumn("Status");
        table.AddColumn("Created");

        foreach (Project project in projectList)
        {
            table.AddRow(
                Markup.Escape(project.Name),
                project.Team ?? "-",
                project.IsActive ? "[green]Active[/]" : "[grey]Archived[/]",
                project.CreatedAt.ToString("yyyy-MM-dd")
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}

public class ProjectCreateCommand : Command<ProjectCreateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Project name")]
        [CommandArgument(0, "<name>")]
        public string Name { get; set; } = string.Empty;

        [Description("Project description")]
        [CommandOption("-d|--description")]
        public string? Description { get; set; }

        [Description("Team name")]
        [CommandOption("-t|--team")]
        public string? Team { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        ProjectService service = new();

        if (service.GetProject(settings.Name) != null)
        {
            AnsiConsole.MarkupLine($"[red]Project '{settings.Name}' already exists.[/]");
            return 1;
        }

        Project project = new()
        {
            Name = settings.Name,
            Description = settings.Description,
            Team = settings.Team
        };

        service.AddProject(project);
        AnsiConsole.MarkupLine($"[green]✓[/] Project '[cyan]{settings.Name}[/]' created successfully.");
        
        return 0;
    }
}

public class ProjectArchiveCommand : Command<ProjectArchiveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Project name")]
        [CommandArgument(0, "<name>")]
        public string Name { get; set; } = string.Empty;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        ProjectService service = new();

        if (service.ArchiveProject(settings.Name))
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Project '[cyan]{settings.Name}[/]' archived successfully.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Project '{settings.Name}' not found.[/]");
        return 1;
    }
}