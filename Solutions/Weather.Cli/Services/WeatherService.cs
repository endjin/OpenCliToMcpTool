using System.Text.Json;
using Weather.Cli.Models;

namespace Weather.Cli.Services;

public class WeatherService
{
    private readonly Random _random = new();
    private readonly string[] _conditions =
    [
        "Clear", "Partly Cloudy", "Cloudy", "Overcast", 
        "Light Rain", "Rain", "Heavy Rain", "Thunderstorm",
        "Snow", "Fog", "Windy"
    ];
    
    private readonly string[] _windDirections = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];

    public async Task<WeatherData> GetCurrentWeatherAsync(string city)
    {
        // Simulate API call
        await Task.Delay(300);
        
        int baseTemp = _random.Next(-10, 35);
        WeatherData weather = new()
        {
            City = city,
            Date = DateTime.Today,
            Temperature = baseTemp,
            Description = _conditions[_random.Next(_conditions.Length)],
            Humidity = _random.Next(30, 95),
            WindSpeed = Math.Round(_random.NextDouble() * 30, 1),
            WindDirection = _windDirections[_random.Next(_windDirections.Length)],
            FeelsLike = baseTemp + _random.Next(-5, 5),
            CloudCover = _random.Next(0, 101),
            Pressure = Math.Round(980 + _random.NextDouble() * 40, 1),
            Visibility = Math.Round(1 + _random.NextDouble() * 19, 1)
        };
        
        return weather;
    }

    public async Task<List<WeatherData>> GetForecastAsync(string city, int days)
    {
        List<WeatherData> forecast = [];
        int baseTemp = _random.Next(-10, 35);
        
        for (int i = 0; i < days; i++)
        {
            await Task.Delay(50); // Simulate API call
            
            WeatherData weather = new()
            {
                City = city,
                Date = DateTime.Today.AddDays(i),
                Temperature = baseTemp + _random.Next(-10, 10),
                Description = _conditions[_random.Next(_conditions.Length)],
                Humidity = _random.Next(30, 95),
                WindSpeed = Math.Round(_random.NextDouble() * 30, 1),
                WindDirection = _windDirections[_random.Next(_windDirections.Length)],
                FeelsLike = baseTemp + _random.Next(-7, 7),
                CloudCover = _random.Next(0, 101),
                Pressure = Math.Round(980 + _random.NextDouble() * 40, 1),
                Visibility = Math.Round(1 + _random.NextDouble() * 19, 1)
            };
            
            forecast.Add(weather);
        }
        
        return forecast;
    }

    public double ConvertTemperature(double celsius, TemperatureUnit toUnit)
    {
        return toUnit switch
        {
            TemperatureUnit.Fahrenheit => Math.Round(celsius * 9 / 5 + 32, 1),
            TemperatureUnit.Kelvin => Math.Round(celsius + 273.15, 1),
            _ => celsius
        };
    }

    public string GetTemperatureSymbol(TemperatureUnit unit)
    {
        return unit switch
        {
            TemperatureUnit.Fahrenheit => "°F",
            TemperatureUnit.Kelvin => "K",
            _ => "°C"
        };
    }
}

public class LocationService
{
    private readonly string _dataFile;
    private List<Location> _locations = [];

    public LocationService()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "weather-cli"
        );
        Directory.CreateDirectory(appData);
        _dataFile = Path.Combine(appData, "locations.json");
        LoadLocations();
    }

    private void LoadLocations()
    {
        if (File.Exists(_dataFile))
        {
            string json = File.ReadAllText(_dataFile);
            _locations = JsonSerializer.Deserialize<List<Location>>(json) ?? [];
        }
        else
        {
            _locations = [];
        }
    }

    private void SaveLocations()
    {
        string json = JsonSerializer.Serialize(_locations, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_dataFile, json);
    }

    public IEnumerable<Location> GetAll() => _locations;

    public Location? Get(string city) => 
        _locations.FirstOrDefault(l => l.City.Equals(city, StringComparison.OrdinalIgnoreCase));

    public void Add(Location location)
    {
        _locations.Add(location);
        SaveLocations();
    }

    public bool Remove(string city)
    {
        bool removed = _locations.RemoveAll(l => l.City.Equals(city, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) SaveLocations();
        return removed;
    }
}