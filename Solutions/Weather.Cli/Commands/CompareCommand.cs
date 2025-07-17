using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Weather.Cli.Models;
using Weather.Cli.Services;

namespace Weather.Cli.Commands;

public class CompareCommand : AsyncCommand<CompareCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Cities to compare (2-5 cities)")]
        [CommandArgument(0, "<cities>")]
        public string[] Cities { get; set; } = [];
        
        [Description("Temperature unit (celsius, fahrenheit, kelvin)")]
        [CommandOption("-u|--unit")]
        public TemperatureUnit Unit { get; set; } = TemperatureUnit.Celsius;
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Cities.Length < 2)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Please provide at least 2 cities to compare");
            return 1;
        }
        
        if (settings.Cities.Length > 5)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Maximum 5 cities can be compared at once");
            return 1;
        }
        
        WeatherService service = new();
        List<WeatherData> weatherData = [];
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                ProgressTask task = ctx.AddTask("Fetching weather data...", maxValue: settings.Cities.Length);
                
                foreach (string city in settings.Cities)
                {
                    WeatherData weather = await service.GetCurrentWeatherAsync(city);
                    weatherData.Add(weather);
                    task.Increment(1);
                }
            });
        
        DisplayComparison(weatherData, settings.Unit, service);
        
        return 0;
    }
    
    private static void DisplayComparison(List<WeatherData> weatherData, TemperatureUnit unit, WeatherService service)
    {
        string symbol = service.GetTemperatureSymbol(unit);
        
        // Create comparison table
        Table table = new();
        table.Title = new TableTitle("[bold]Weather Comparison[/]");
        table.AddColumn("Metric");
        
        foreach (WeatherData weather in weatherData)
        {
            table.AddColumn(new TableColumn(weather.City).Centered());
        }
        
        // Temperature row
        List<string> tempRow = ["Temperature"];
        foreach (WeatherData weather in weatherData)
        {
            double temp = service.ConvertTemperature(weather.Temperature, unit);
            Style style = GetTemperatureStyle(weather.Temperature);
            tempRow.Add($"[{style.Foreground}]{temp}{symbol}[/]");
        }
        table.AddRow(tempRow.ToArray());
        
        // Condition row
        List<string> conditionRow = ["Condition"];
        foreach (WeatherData weather in weatherData)
        {
            conditionRow.Add(weather.Description);
        }
        table.AddRow(conditionRow.ToArray());
        
        // Humidity row
        List<string> humidityRow = ["Humidity"];
        foreach (WeatherData weather in weatherData)
        {
            humidityRow.Add($"{weather.Humidity}%");
        }
        table.AddRow(humidityRow.ToArray());
        
        // Wind row
        List<string> windRow = ["Wind"];
        foreach (WeatherData weather in weatherData)
        {
            windRow.Add($"{weather.WindSpeed} km/h {weather.WindDirection}");
        }
        table.AddRow(windRow.ToArray());
        
        // Feels Like row
        List<string> feelsLikeRow = ["Feels Like"];
        foreach (WeatherData weather in weatherData)
        {
            double feelsLike = service.ConvertTemperature(weather.FeelsLike, unit);
            feelsLikeRow.Add($"{feelsLike}{symbol}");
        }
        table.AddRow(feelsLikeRow.ToArray());
        
        AnsiConsole.Write(table);
        
        // Show summary
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("Summary"));
        
        WeatherData warmest = weatherData.OrderByDescending(w => w.Temperature).First();
        WeatherData coldest = weatherData.OrderBy(w => w.Temperature).First();
        WeatherData wettest = weatherData.OrderByDescending(w => w.Humidity).First();
        WeatherData windiest = weatherData.OrderByDescending(w => w.WindSpeed).First();
        
        AnsiConsole.MarkupLine($"🌡️  Warmest: [bold]{warmest.City}[/] ({service.ConvertTemperature(warmest.Temperature, unit)}{symbol})");
        AnsiConsole.MarkupLine($"❄️  Coldest: [bold]{coldest.City}[/] ({service.ConvertTemperature(coldest.Temperature, unit)}{symbol})");
        AnsiConsole.MarkupLine($"💧 Most Humid: [bold]{wettest.City}[/] ({wettest.Humidity}%)");
        AnsiConsole.MarkupLine($"💨 Windiest: [bold]{windiest.City}[/] ({windiest.WindSpeed} km/h)");
    }
    
    private static Style GetTemperatureStyle(double celsius)
    {
        return celsius switch
        {
            < 0 => new Style(Color.Blue),
            < 10 => new Style(Color.Aqua),
            < 20 => new Style(Color.Green),
            < 30 => new Style(Color.Yellow),
            _ => new Style(Color.Red)
        };
    }
}