namespace StarkAid.Api.Options;

/// <summary>
/// Configurações da API Tuya.
/// </summary>
public sealed class TuyaConfig
{
    public string AccessId { get; set; } = string.Empty;
    public string AccessSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}
