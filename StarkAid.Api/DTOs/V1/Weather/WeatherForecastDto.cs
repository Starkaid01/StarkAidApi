namespace StarkAid.Api.DTOs.V1.Weather;

public class WeatherForecastDto
{
    public CurrentWeatherDto? Current { get; set; }
    public List<HourlyWeatherDto>? Hourly { get; set; }
    public List<DailyWeatherDto>? Daily { get; set; }
}

public class CurrentWeatherDto
{
    public double Temperature { get; set; }
    public int WeatherCode { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public DateTimeOffset Time { get; set; }
    public string WeatherDescription => GetWeatherDescription(WeatherCode);
    public string WindDirectionText => GetWindDirectionText(WindDirection);

    private string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Céu limpo",
            1 => "Principalmente limpo",
            2 => "Parcialmente nublado",
            3 => "Nublado",
            45 => "Nevoeiro",
            48 => "Nevoeiro com geada",
            51 => "Garoa leve",
            53 => "Garoa moderada",
            55 => "Garoa densa",
            56 => "Garoa congelante leve",
            57 => "Garoa congelante densa",
            61 => "Chuva leve",
            63 => "Chuva moderada",
            65 => "Chuva forte",
            66 => "Chuva congelante leve",
            67 => "Chuva congelante forte",
            71 => "Queda de neve leve",
            73 => "Queda de neve moderada",
            75 => "Queda de neve forte",
            77 => "Grãos de neve",
            80 => "Chuva leve",
            81 => "Chuva moderada",
            82 => "Chuva forte",
            85 => "Neve leve",
            86 => "Neve forte",
            95 => "Trovoada",
            96 => "Trovoada com granizo leve",
            99 => "Trovoada com granizo forte",
            _ => "Desconhecido"
        };
    }

    private string GetWindDirectionText(double degrees)
    {
        return degrees switch
        {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "L",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SO",
            >= 247.5 and < 292.5 => "O",
            >= 292.5 and < 337.5 => "NO",
            _ => "N"
        };
    }
}

public class HourlyWeatherDto
{
    public DateTimeOffset Time { get; set; }
    public double Temperature { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public string WeatherDescription => GetWeatherDescription(WeatherCode);

    private string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Céu limpo",
            1 => "Principalmente limpo",
            2 => "Parcialmente nublado",
            3 => "Nublado",
            45 => "Nevoeiro",
            48 => "Nevoeiro com geada",
            51 => "Garoa leve",
            53 => "Garoa moderada",
            55 => "Garoa densa",
            56 => "Garoa congelante leve",
            57 => "Garoa congelante densa",
            61 => "Chuva leve",
            63 => "Chuva moderada",
            65 => "Chuva forte",
            66 => "Chuva congelante leve",
            67 => "Chuva congelante forte",
            71 => "Queda de neve leve",
            73 => "Queda de neve moderada",
            75 => "Queda de neve forte",
            77 => "Grãos de neve",
            80 => "Chuva leve",
            81 => "Chuva moderada",
            82 => "Chuva forte",
            85 => "Neve leve",
            86 => "Neve forte",
            95 => "Trovoada",
            96 => "Trovoada com granizo leve",
            99 => "Trovoada com granizo forte",
            _ => "Desconhecido"
        };
    }
}

public class DailyWeatherDto
{
    public DateTimeOffset Date { get; set; }
    public double TemperatureMax { get; set; }
    public double TemperatureMin { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public double WindSpeedMax { get; set; }
    public double WindDirection { get; set; }
    public string WeatherDescription => GetWeatherDescription(WeatherCode);

    private string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Céu limpo",
            1 => "Principalmente limpo",
            2 => "Parcialmente nublado",
            3 => "Nublado",
            45 => "Nevoeiro",
            48 => "Nevoeiro com geada",
            51 => "Garoa leve",
            53 => "Garoa moderada",
            55 => "Garoa densa",
            56 => "Garoa congelante leve",
            57 => "Garoa congelante densa",
            61 => "Chuva leve",
            63 => "Chuva moderada",
            65 => "Chuva forte",
            66 => "Chuva congelante leve",
            67 => "Chuva congelante forte",
            71 => "Queda de neve leve",
            73 => "Queda de neve moderada",
            75 => "Queda de neve forte",
            77 => "Grãos de neve",
            80 => "Chuva leve",
            81 => "Chuva moderada",
            82 => "Chuva forte",
            85 => "Neve leve",
            86 => "Neve forte",
            95 => "Trovoada",
            96 => "Trovoada com granizo leve",
            99 => "Trovoada com granizo forte",
            _ => "Desconhecido"
        };
    }
}
