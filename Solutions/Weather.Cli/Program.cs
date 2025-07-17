using Spectre.Console.Cli;
using Weather.Cli.Commands;

CommandApp app = new();

app.Configure(config =>
{
    config.SetApplicationName("weather");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<CurrentCommand>("current")
        .WithDescription("Get current weather")
        .WithExample("current", "London")
        .WithExample("current", "Tokyo", "--unit", "fahrenheit")
        .WithExample("current", "Paris", "--detailed");
    
    config.AddCommand<ForecastCommand>("forecast")
        .WithDescription("Get weather forecast")
        .WithExample("forecast", "Paris")
        .WithExample("forecast", "Berlin", "--days", "7")
        .WithExample("forecast", "Madrid", "--days", "3", "--format", "json");
    
    config.AddCommand<CompareCommand>("compare")
        .WithDescription("Compare weather between cities")
        .WithExample("compare", "London", "Paris")
        .WithExample("compare", "Tokyo", "New York", "Berlin");
    
    config.AddBranch("location", location =>
    {
        location.SetDescription("Manage saved locations");
        
        location.AddCommand<LocationAddCommand>("add")
            .WithDescription("Add a favorite location")
            .WithExample("location", "add", "London", "--nickname", "home");
        
        location.AddCommand<LocationListCommand>("list")
            .WithDescription("List saved locations")
            .WithExample("location", "list");
        
        location.AddCommand<LocationRemoveCommand>("remove")
            .WithDescription("Remove a saved location")
            .WithExample("location", "remove", "London");
    });
});

return await app.RunAsync(args);