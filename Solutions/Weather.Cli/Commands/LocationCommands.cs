using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Weather.Cli.Models;
using Weather.Cli.Services;

namespace Weather.Cli.Commands;

public class LocationAddCommand : Command<LocationAddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("City name")]
        [CommandArgument(0, "<city>")]
        public string City { get; set; } = string.Empty;
        
        [Description("Nickname for the location")]
        [CommandOption("-n|--nickname")]
        public string? Nickname { get; set; }
    }
    
    public override int Execute(CommandContext context, Settings settings)
    {
        LocationService service = new();
        
        if (service.Get(settings.City) != null)
        {
            AnsiConsole.MarkupLine($"[yellow]Location '{settings.City}' already exists[/]");
            return 0;
        }
        
        Location location = new()
        {
            City = settings.City,
            Nickname = settings.Nickname,
            // In a real app, we'd geocode these
            Latitude = Random.Shared.NextDouble() * 180 - 90,
            Longitude = Random.Shared.NextDouble() * 360 - 180
        };
        
        service.Add(location);
        AnsiConsole.MarkupLine($"[green]✓[/] Added location: {settings.City}" + 
            (settings.Nickname != null ? $" ({settings.Nickname})" : ""));
        
        return 0;
    }
}

public class LocationListCommand : Command
{
    public override int Execute(CommandContext context)
    {
        LocationService service = new();
        List<Location> locations = service.GetAll().ToList();
        
        if (!locations.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No saved locations[/]");
            return 0;
        }
        
        Table table = new();
        table.Title = new TableTitle("[bold]Saved Locations[/]");
        table.AddColumn("City");
        table.AddColumn("Nickname");
        table.AddColumn("Coordinates");
        table.AddColumn("Added");
        
        foreach (Location location in locations.OrderBy(l => l.City))
        {
            table.AddRow(
                location.City,
                location.Nickname ?? "-",
                $"{location.Latitude:F2}, {location.Longitude:F2}",
                location.AddedAt.ToString("yyyy-MM-dd")
            );
        }
        
        AnsiConsole.Write(table);
        return 0;
    }
}

public class LocationRemoveCommand : Command<LocationRemoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("City name to remove")]
        [CommandArgument(0, "<city>")]
        public string City { get; set; } = string.Empty;
        
        [Description("Skip confirmation")]
        [CommandOption("-f|--force")]
        public bool Force { get; set; }
    }
    
    public override int Execute(CommandContext context, Settings settings)
    {
        LocationService service = new();
        Location? location = service.Get(settings.City);
        
        if (location == null)
        {
            AnsiConsole.MarkupLine($"[red]Location '{settings.City}' not found[/]");
            return 1;
        }
        
        if (!settings.Force)
        {
            if (!AnsiConsole.Confirm($"Remove location '{settings.City}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                return 0;
            }
        }
        
        if (service.Remove(settings.City))
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Removed location: {settings.City}");
            return 0;
        }
        
        AnsiConsole.MarkupLine("[red]Failed to remove location[/]");
        return 1;
    }
}