namespace Weather.Cli.Models;

public class WeatherData
{
    public string City { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string WindDirection { get; set; } = "N";
    public double FeelsLike { get; set; }
    public int CloudCover { get; set; }
    public double Pressure { get; set; }
    public double Visibility { get; set; }
}

public class Location
{
    public string City { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit,
    Kelvin
}