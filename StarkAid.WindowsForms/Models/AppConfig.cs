namespace StarkAid.WindowsForms.Models;

public class AppConfig
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public SpotifyConfig? Spotify { get; set; }
    public EwelinkConfig? Ewelink { get; set; }
}

public class SpotifyConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
}

public class EwelinkConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

