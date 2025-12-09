namespace StarkAid.Api.DTOs.Config;

public class AppConfigDto
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public SpotifyConfigDto Spotify { get; set; } = new();
    public EwelinkConfigDto Ewelink { get; set; } = new();
}

public class SpotifyConfigDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = "https://accounts.spotify.com/api/token";
}

public class EwelinkConfigDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

