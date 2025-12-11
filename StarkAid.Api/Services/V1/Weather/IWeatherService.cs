using StarkAid.Api.DTOs.Weather;

namespace StarkAid.Api.Services.V1.Weather;

public interface IWeatherService
{
    Task<WeatherForecastDto?> GetWeatherForecastAsync(string? cidade, string? bairro);
    Task<WeatherForecastDto?> GetWeatherForecastByCoordinatesAsync(double latitude, double longitude);
}
