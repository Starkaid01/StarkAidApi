using System;
using System.Collections.Generic;

namespace StarkAid.Web.DTOs
{
    public class WeatherForecastDto
    {
        public WeatherCurrentDto Current { get; set; } = new();
        public List<WeatherHourlyDto> Hourly { get; set; } = new();
        public List<WeatherDailyDto> Daily { get; set; } = new();
    }

    public class WeatherCurrentDto
    {
        public double Temperature { get; set; }
        public int WeatherCode { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public DateTime Time { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
        public string WindDirectionText { get; set; } = string.Empty;
        // Adicionando campo para localização se a API retornar (ou simularemos)
        public string? Location { get; set; } 
    }

    public class WeatherHourlyDto
    {
        public DateTime Time { get; set; }
        public double Temperature { get; set; }
        public double Precipitation { get; set; }
        public int WeatherCode { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
    }

    public class WeatherDailyDto
    {
        public DateTime Date { get; set; }
        public double TemperatureMax { get; set; }
        public double TemperatureMin { get; set; }
        public double Precipitation { get; set; }
        public int WeatherCode { get; set; }
        public double WindSpeedMax { get; set; }
        public int WindDirection { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
    }
}
