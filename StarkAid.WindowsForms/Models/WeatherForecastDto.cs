using System.Text.Json.Serialization;

namespace StarkAid.WindowsForms.Models;

public class WeatherForecastDto
{
    [JsonPropertyName("current")]
    public CurrentWeatherDto? Current { get; set; }

    [JsonPropertyName("hourly")]
    public List<HourlyWeatherDto>? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public List<DailyWeatherDto>? Daily { get; set; }
}

public class CurrentWeatherDto
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("weatherCode")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("windSpeed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("windDirection")]
    public double WindDirection { get; set; }

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("weatherDescription")]
    public string WeatherDescription { get; set; } = string.Empty;

    [JsonPropertyName("windDirectionText")]
    public string WindDirectionText { get; set; } = string.Empty;
}

public class HourlyWeatherDto
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [JsonPropertyName("weatherCode")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("windSpeed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("windDirection")]
    public double WindDirection { get; set; }

    [JsonPropertyName("weatherDescription")]
    public string WeatherDescription { get; set; } = string.Empty;
}

public class DailyWeatherDto
{
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }

    [JsonPropertyName("temperatureMax")]
    public double TemperatureMax { get; set; }

    [JsonPropertyName("temperatureMin")]
    public double TemperatureMin { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }

    [JsonPropertyName("weatherCode")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("windSpeedMax")]
    public double WindSpeedMax { get; set; }

    [JsonPropertyName("windDirection")]
    public double WindDirection { get; set; }

    [JsonPropertyName("weatherDescription")]
    public string WeatherDescription { get; set; } = string.Empty;
}
