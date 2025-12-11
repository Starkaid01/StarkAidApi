using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StarkAid.Api.DTOs.Weather;

namespace StarkAid.Api.Services.V1.Weather;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherForecastDto?> GetWeatherForecastAsync(string? cidade, string? bairro)
    {
        if (string.IsNullOrWhiteSpace(cidade))
        {
            _logger.LogWarning("Cidade não fornecida para busca de previsão do tempo");
            return null;
        }

        try
        {
            _logger.LogInformation("Buscando coordenadas para {Cidade}, {Bairro}", cidade, bairro);
            // Buscar coordenadas usando Nominatim (OpenStreetMap) - gratuito
            var location = await GetCoordinatesAsync(cidade, bairro);
            if (location == null)
            {
                _logger.LogWarning("Não foi possível obter coordenadas para {Cidade}, {Bairro}", cidade, bairro);
                return null;
            }

            _logger.LogInformation("Coordenadas obtidas: Latitude={Latitude}, Longitude={Longitude}", location.Latitude, location.Longitude);
            var forecast = await GetWeatherForecastByCoordinatesAsync(location.Latitude, location.Longitude);
            if (forecast == null)
            {
                _logger.LogWarning("Não foi possível obter previsão do tempo para coordenadas {Latitude}, {Longitude}", location.Latitude, location.Longitude);
            }
            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter previsão do tempo para {Cidade}, {Bairro}", cidade, bairro);
            return null;
        }
    }

    public async Task<WeatherForecastDto?> GetWeatherForecastByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            // Construir URL da API Open-Meteo
            // Usar InvariantCulture para garantir ponto decimal (não vírgula)
            var url = $"https://api.open-meteo.com/v1/forecast?" +
                     $"latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&" +
                     $"longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&" +
                     $"hourly=temperature_2m,precipitation,weathercode,windspeed_10m,winddirection_10m&" +
                     $"daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode,windspeed_10m_max,winddirection_10m_dominant&" +
                     $"current_weather=true&" +
                     $"temperature_unit=celsius&" +
                     $"windspeed_unit=kmh&" +
                     $"precipitation_unit=mm&" +
                     $"timezone=America/Sao_Paulo";

            _logger.LogInformation("Chamando API Open-Meteo: {Url}", url);
            
            var httpResponse = await _httpClient.GetAsync(url);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("API Open-Meteo retornou status {StatusCode}: {ReasonPhrase}", 
                    httpResponse.StatusCode, httpResponse.ReasonPhrase);
                return null;
            }

            var content = await httpResponse.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Resposta da API Open-Meteo está vazia");
                return null;
            }

            var response = System.Text.Json.JsonSerializer.Deserialize<OpenMeteoResponse>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response == null)
            {
                _logger.LogWarning("Não foi possível deserializar resposta da API Open-Meteo. Conteúdo: {Content}", content.Substring(0, Math.Min(500, content.Length)));
                return null;
            }

            _logger.LogInformation("Resposta recebida da API Open-Meteo. Mapeando dados...");
            var forecast = MapToWeatherForecastDto(response);
            _logger.LogInformation("Previsão do tempo mapeada com sucesso. Current: {HasCurrent}, Hourly: {HourlyCount}, Daily: {DailyCount}", 
                forecast?.Current != null, forecast?.Hourly?.Count ?? 0, forecast?.Daily?.Count ?? 0);
            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter previsão do tempo para coordenadas {Latitude}, {Longitude}", latitude, longitude);
            return null;
        }
    }

    private async Task<LocationCoordinates?> GetCoordinatesAsync(string cidade, string? bairro)
    {
        try
        {
            // Tentar primeiro com bairro (se fornecido)
            if (!string.IsNullOrWhiteSpace(bairro))
            {
                var queryWithBairro = $"{bairro}, {cidade}, Brasil";
                var location = await TryGetCoordinatesAsync(queryWithBairro);
                if (location != null)
                {
                    _logger.LogInformation("Coordenadas obtidas usando bairro: {Query}", queryWithBairro);
                    return location;
                }
            }

            // Se falhar ou não tiver bairro, tentar apenas com cidade
            var queryCityOnly = $"{cidade}, Brasil";
            var locationCity = await TryGetCoordinatesAsync(queryCityOnly);
            if (locationCity != null)
            {
                _logger.LogInformation("Coordenadas obtidas usando apenas cidade: {Query}", queryCityOnly);
                return locationCity;
            }

            _logger.LogWarning("Não foi possível obter coordenadas para {Cidade}, {Bairro}", cidade, bairro);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter coordenadas para {Cidade}, {Bairro}", cidade, bairro);
            return null;
        }
    }

    private async Task<LocationCoordinates?> TryGetCoordinatesAsync(string query)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "StarkAid/1.0");

            var httpResponse = await _httpClient.GetAsync(url);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Nominatim retornou status {StatusCode}", httpResponse.StatusCode);
                return null;
            }

            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Resposta completa do Nominatim: {Json}", jsonContent);
            
            var response = System.Text.Json.JsonSerializer.Deserialize<JsonElement[]>(jsonContent);
            if (response == null || response.Length == 0)
            {
                _logger.LogWarning("Resposta do Nominatim está vazia ou nula");
                return null;
            }

            var firstResult = response[0];
            
            // Tentar diferentes formas de obter as coordenadas
            double? lat = null;
            double? lon = null;
            
            // Tentar obter como string primeiro (formato mais comum)
            if (firstResult.TryGetProperty("lat", out var latElement))
            {
                if (latElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var latString = latElement.GetString();
                    if (!string.IsNullOrWhiteSpace(latString))
                    {
                        if (double.TryParse(latString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedLat))
                        {
                            lat = parsedLat;
                        }
                    }
                }
                else if (latElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    lat = latElement.GetDouble();
                }
            }
            
            if (firstResult.TryGetProperty("lon", out var lonElement))
            {
                if (lonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var lonString = lonElement.GetString();
                    if (!string.IsNullOrWhiteSpace(lonString))
                    {
                        if (double.TryParse(lonString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedLon))
                        {
                            lon = parsedLon;
                        }
                    }
                }
                else if (lonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    lon = lonElement.GetDouble();
                }
            }
            
            // Fallback: tentar "latitude" e "longitude" (algumas APIs usam nomes diferentes)
            if (!lat.HasValue && firstResult.TryGetProperty("latitude", out var latElement2))
            {
                if (latElement2.ValueKind == System.Text.Json.JsonValueKind.Number)
                    lat = latElement2.GetDouble();
                else if (latElement2.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (double.TryParse(latElement2.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedLat))
                        lat = parsedLat;
                }
            }
            
            if (!lon.HasValue && firstResult.TryGetProperty("longitude", out var lonElement2))
            {
                if (lonElement2.ValueKind == System.Text.Json.JsonValueKind.Number)
                    lon = lonElement2.GetDouble();
                else if (lonElement2.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (double.TryParse(lonElement2.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedLon))
                        lon = parsedLon;
                }
            }
            
            if (lat.HasValue && lon.HasValue)
            {
                _logger.LogInformation("Coordenadas obtidas - lat: {Lat}, lon: {Lon}", lat.Value, lon.Value);
                
                // Validar que as coordenadas estão em range válido
                if (lat.Value >= -90 && lat.Value <= 90 && lon.Value >= -180 && lon.Value <= 180)
                {
                    return new LocationCoordinates { Latitude = lat.Value, Longitude = lon.Value };
                }
                else
                {
                    _logger.LogWarning("Coordenadas fora do range válido - lat: {Lat}, lon: {Lon}", lat.Value, lon.Value);
                }
            }
            else
            {
                _logger.LogWarning("Não foi possível obter coordenadas válidas do Nominatim. Resultado: {Result}", firstResult.ToString());
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao tentar obter coordenadas para query: {Query}", query);
            return null;
        }
    }

    private WeatherForecastDto MapToWeatherForecastDto(OpenMeteoResponse response)
    {
        var dto = new WeatherForecastDto
        {
            Current = new CurrentWeatherDto
            {
                Temperature = response.CurrentWeather?.Temperature ?? 0,
                WeatherCode = response.CurrentWeather?.WeatherCode ?? 0,
                WindSpeed = response.CurrentWeather?.WindSpeed ?? 0,
                WindDirection = response.CurrentWeather?.WindDirection ?? 0,
                Time = response.CurrentWeather?.Time ?? DateTimeOffset.UtcNow
            }
        };

        // Mapear dados horários
        if (response.Hourly != null && response.Hourly.Time != null && response.Hourly.Time.Length > 0)
        {
            var hourlyData = new List<HourlyWeatherDto>();
            var count = Math.Min(response.Hourly.Time.Length, 24); // Próximas 24 horas

            for (int i = 0; i < count; i++)
            {
                hourlyData.Add(new HourlyWeatherDto
                {
                    Time = DateTimeOffset.Parse(response.Hourly.Time[i]),
                    Temperature = response.Hourly.Temperature2m?[i] ?? 0,
                    Precipitation = response.Hourly.Precipitation?[i] ?? 0,
                    WeatherCode = response.Hourly.WeatherCode?[i] ?? 0,
                    WindSpeed = response.Hourly.WindSpeed10m?[i] ?? 0,
                    WindDirection = response.Hourly.WindDirection10m?[i] ?? 0
                });
            }
            dto.Hourly = hourlyData;
        }

        // Mapear dados diários
        if (response.Daily != null && response.Daily.Time != null && response.Daily.Time.Length > 0)
        {
            var dailyData = new List<DailyWeatherDto>();
            var count = Math.Min(response.Daily.Time.Length, 7); // Próximos 7 dias

            for (int i = 0; i < count; i++)
            {
                dailyData.Add(new DailyWeatherDto
                {
                    Date = DateTimeOffset.Parse(response.Daily.Time[i]),
                    TemperatureMax = response.Daily.Temperature2mMax?[i] ?? 0,
                    TemperatureMin = response.Daily.Temperature2mMin?[i] ?? 0,
                    Precipitation = response.Daily.PrecipitationSum?[i] ?? 0,
                    WeatherCode = response.Daily.WeatherCode?[i] ?? 0,
                    WindSpeedMax = response.Daily.WindSpeed10mMax?[i] ?? 0,
                    WindDirection = response.Daily.WindDirection10mDominant?[i] ?? 0
                });
            }
            dto.Daily = dailyData;
        }

        return dto;
    }

    private class LocationCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private class OpenMeteoResponse
    {
        [JsonPropertyName("current_weather")]
        public CurrentWeatherData? CurrentWeather { get; set; }
        [JsonPropertyName("hourly")]
        public HourlyData? Hourly { get; set; }
        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }

    private class CurrentWeatherData
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
        [JsonPropertyName("weathercode")]
        public int? WeatherCode { get; set; }
        [JsonPropertyName("windspeed")]
        public double? WindSpeed { get; set; }
        [JsonPropertyName("winddirection")]
        public double? WindDirection { get; set; }
        [JsonPropertyName("time")]
        public DateTimeOffset? Time { get; set; }
    }

    private class HourlyData
    {
        [JsonPropertyName("time")]
        public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m")]
        public double[]? Temperature2m { get; set; }
        [JsonPropertyName("precipitation")]
        public double[]? Precipitation { get; set; }
        [JsonPropertyName("weathercode")]
        public int[]? WeatherCode { get; set; }
        [JsonPropertyName("windspeed_10m")]
        public double[]? WindSpeed10m { get; set; }
        [JsonPropertyName("winddirection_10m")]
        public double[]? WindDirection10m { get; set; }
    }

    private class DailyData
    {
        [JsonPropertyName("time")]
        public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m_max")]
        public double[]? Temperature2mMax { get; set; }
        [JsonPropertyName("temperature_2m_min")]
        public double[]? Temperature2mMin { get; set; }
        [JsonPropertyName("precipitation_sum")]
        public double[]? PrecipitationSum { get; set; }
        [JsonPropertyName("weathercode")]
        public int[]? WeatherCode { get; set; }
        [JsonPropertyName("windspeed_10m_max")]
        public double[]? WindSpeed10mMax { get; set; }
        [JsonPropertyName("winddirection_10m_dominant")]
        public double[]? WindDirection10mDominant { get; set; }
    }
}
