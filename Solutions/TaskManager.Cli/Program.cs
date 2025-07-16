using Spectre.Console.Cli;
using TaskManager.Cli.Commands;

CommandApp app = new();

app.Configure(config =>
{
    config.SetApplicationName("taskmanager");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<ListCommand>("list")
        .WithAlias("ls")
        .WithDescription("List all tasks")
        .WithExample("list")
        .WithExample("list --status pending")
        .WithExample("list --priority high --status in-progress");
    
    config.AddBranch("task", task =>
    {
        task.SetDescription("Manage individual tasks");
        
        task.AddCommand<TaskAddCommand>("add")
            .WithDescription("Add a new task")
            .WithExample("task add \"Complete project documentation\"")
            .WithExample("task add \"Review pull request\" --priority high --due tomorrow");
        
        task.AddCommand<TaskUpdateCommand>("update")
            .WithDescription("Update an existing task")
            .WithExample("task update 123 --status completed")
            .WithExample("task update 123 --priority low --assignee john");
        
        task.AddCommand<TaskDeleteCommand>("delete")
            .WithAlias("rm")
            .WithDescription("Delete a task")
            .WithExample("task delete 123")
            .WithExample("task delete 123 --force");
        
        task.AddCommand<TaskShowCommand>("show")
            .WithDescription("Show details of a specific task")
            .WithExample("task show 123")
            .WithExample("task show 123 --format json");
    });
    
    config.AddBranch("project", project =>
    {
        project.SetDescription("Manage projects");
        
        project.AddCommand<ProjectListCommand>("list")
            .WithDescription("List all projects")
            .WithExample("project list")
            .WithExample("project list --active");
        
        project.AddCommand<ProjectCreateCommand>("create")
            .WithDescription("Create a new project")
            .WithExample("project create \"Website Redesign\"")
            .WithExample("project create \"API Development\" --team backend");
        
        project.AddCommand<ProjectArchiveCommand>("archive")
            .WithDescription("Archive a project")
            .WithExample("project archive \"Old Project\"");
    });
    
    config.AddCommand<StatsCommand>("stats")
        .WithDescription("Show task statistics")
        .WithExample("stats")
        .WithExample("stats --period week")
        .WithExample("stats --period month --project \"Website Redesign\"");
    
    config.AddCommand<ExportCommand>("export")
        .WithDescription("Export tasks to various formats")
        .WithExample("export tasks.json")
        .WithExample("export tasks.csv --format csv")
        .WithExample("export report.md --format markdown --include-completed");
});

return await app.RunAsync(args);