namespace StarkAid.Api.Options;

/// <summary>
/// Configurações do JWT lidas de appsettings.
/// </summary>
public sealed class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
