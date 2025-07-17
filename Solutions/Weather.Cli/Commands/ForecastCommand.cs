using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Weather.Cli.Models;
using Weather.Cli.Services;

namespace Weather.Cli.Commands;

public class ForecastCommand : AsyncCommand<ForecastCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("City name")]
        [CommandArgument(0, "<city>")]
        public string City { get; set; } = string.Empty;
        
        [Description("Number of days (1-14)")]
        [CommandOption("-d|--days")]
        public int Days { get; set; } = 5;
        
        [Description("Temperature unit (celsius, fahrenheit, kelvin)")]
        [CommandOption("-u|--unit")]
        public TemperatureUnit Unit { get; set; } = TemperatureUnit.Celsius;
        
        [Description("Output format (table, json, chart)")]
        [CommandOption("-f|--format")]
        public string Format { get; set; } = "table";
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Days < 1 || settings.Days > 14)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Days must be between 1 and 14");
            return 1;
        }
        
        WeatherService service = new();
        
        await AnsiConsole.Status()
            .StartAsync($"Fetching {settings.Days}-day forecast for {settings.City}...", async ctx =>
            {
                List<WeatherData> forecast = await service.GetForecastAsync(settings.City, settings.Days);
                
                switch (settings.Format.ToLower())
                {
                    case "json":
                        DisplayJson(forecast);
                        break;
                    case "chart":
                        DisplayChart(forecast, settings.Unit, service);
                        break;
                    default:
                        DisplayTable(forecast, settings.Unit, service);
                        break;
                }
            });
        
        return 0;
    }
    
    private static void DisplayTable(List<WeatherData> forecast, TemperatureUnit unit, WeatherService service)
    {
        string symbol = service.GetTemperatureSymbol(unit);
        
        Table table = new();
        table.Title = new TableTitle($"[bold]{forecast[0].City} - {forecast.Count} Day Forecast[/]");
        table.AddColumn("Date");
        table.AddColumn("Condition");
        table.AddColumn("Temp");
        table.AddColumn("Humidity");
        table.AddColumn("Wind");
        
        foreach (WeatherData day in forecast)
        {
            double temp = service.ConvertTemperature(day.Temperature, unit);
            
            table.AddRow(
                day.Date.ToString("ddd, MMM d"),
                day.Description,
                $"{temp}{symbol}",
                $"{day.Humidity}%",
                $"{day.WindSpeed} km/h"
            );
        }
        
        AnsiConsole.Write(table);
    }
    
    private static void DisplayChart(List<WeatherData> forecast, TemperatureUnit unit, WeatherService service)
    {
        string symbol = service.GetTemperatureSymbol(unit);
        
        AnsiConsole.Write(new Rule($"[bold]{forecast[0].City} - Temperature Trend[/]"));
        AnsiConsole.WriteLine();
        
        BarChart chart = new BarChart()
            .Width(60)
            .Label($"Temperature ({symbol})");
        
        foreach (WeatherData day in forecast)
        {
            double temp = service.ConvertTemperature(day.Temperature, unit);
            Color color = GetTemperatureColor(day.Temperature);
            chart.AddItem(day.Date.ToString("MMM d"), temp, color);
        }
        
        AnsiConsole.Write(chart);
        AnsiConsole.WriteLine();
        
        // Show condition summary
        var conditionGroups = forecast
            .GroupBy(f => f.Description)
            .Select(g => new { Condition = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);
        
        AnsiConsole.Write(new Rule("Condition Summary"));
        foreach (var group in conditionGroups)
        {
            AnsiConsole.MarkupLine($"  {group.Condition}: [bold]{group.Count}[/] day(s)");
        }
    }
    
    private static void DisplayJson(List<WeatherData> forecast)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(forecast, 
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        AnsiConsole.WriteLine(json);
    }
    
    private static Color GetTemperatureColor(double celsius)
    {
        return celsius switch
        {
            < 0 => Color.Blue,
            < 10 => Color.Aqua,
            < 20 => Color.Green,
            < 30 => Color.Yellow,
            _ => Color.Red
        };
    }
}