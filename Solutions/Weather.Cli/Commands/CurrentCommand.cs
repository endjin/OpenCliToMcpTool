using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Weather.Cli.Models;
using Weather.Cli.Services;

namespace Weather.Cli.Commands;

public class CurrentCommand : AsyncCommand<CurrentCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("City name")]
        [CommandArgument(0, "<city>")]
        public string City { get; set; } = string.Empty;
        
        [Description("Temperature unit (celsius, fahrenheit, kelvin)")]
        [CommandOption("-u|--unit")]
        public TemperatureUnit Unit { get; set; } = TemperatureUnit.Celsius;
        
        [Description("Show detailed information")]
        [CommandOption("-d|--detailed")]
        public bool Detailed { get; set; }
        
        [Description("Output format (text, json)")]
        [CommandOption("-f|--format")]
        public string Format { get; set; } = "text";
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        WeatherService service = new();
        
        await AnsiConsole.Status()
            .StartAsync($"Fetching weather for {settings.City}...", async ctx =>
            {
                WeatherData weather = await service.GetCurrentWeatherAsync(settings.City);
                
                if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(weather, 
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    AnsiConsole.WriteLine(json);
                }
                else
                {
                    DisplayWeather(weather, settings.Unit, settings.Detailed, service);
                }
            });
        
        return 0;
    }
    
    private static void DisplayWeather(WeatherData weather, TemperatureUnit unit, bool detailed, WeatherService service)
    {
        double temp = service.ConvertTemperature(weather.Temperature, unit);
        double feelsLike = service.ConvertTemperature(weather.FeelsLike, unit);
        string symbol = service.GetTemperatureSymbol(unit);
        
        Grid grid = new();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow(
            new Text("Temperature", new Style(Color.Grey)),
            new Text($"{temp}{symbol}", GetTemperatureStyle(weather.Temperature))
        );
        
        grid.AddRow(
            new Text("Condition", new Style(Color.Grey)),
            new Text(weather.Description, GetConditionStyle(weather.Description))
        );
        
        if (detailed)
        {
            grid.AddRow(
                new Text("Feels Like", new Style(Color.Grey)),
                new Text($"{feelsLike}{symbol}")
            );
            
            grid.AddRow(
                new Text("Humidity", new Style(Color.Grey)),
                new Text($"{weather.Humidity}%")
            );
            
            grid.AddRow(
                new Text("Wind", new Style(Color.Grey)),
                new Text($"{weather.WindSpeed} km/h {weather.WindDirection}")
            );
            
            grid.AddRow(
                new Text("Cloud Cover", new Style(Color.Grey)),
                new Text($"{weather.CloudCover}%")
            );
            
            grid.AddRow(
                new Text("Pressure", new Style(Color.Grey)),
                new Text($"{weather.Pressure} hPa")
            );
            
            grid.AddRow(
                new Text("Visibility", new Style(Color.Grey)),
                new Text($"{weather.Visibility} km")
            );
        }
        
        Panel panel = new(grid)
        {
            Header = new PanelHeader($" {weather.City} - Current Weather ", Justify.Center),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };
        
        AnsiConsole.Write(panel);
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
    
    private static Style GetConditionStyle(string condition)
    {
        return condition.ToLower() switch
        {
            var c when c.Contains("clear") => new Style(Color.Yellow),
            var c when c.Contains("cloud") => new Style(Color.Grey),
            var c when c.Contains("rain") => new Style(Color.Blue),
            var c when c.Contains("storm") => new Style(Color.Purple),
            var c when c.Contains("snow") => new Style(Color.White),
            _ => new Style(Color.Default)
        };
    }
}