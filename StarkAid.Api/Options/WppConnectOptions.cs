namespace StarkAid.Api.Options;

/// <summary>
/// Opções de integração ao WhatsApp Cloud (WPPConnect).
/// </summary>
public sealed class WppConnectOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenDeAutenticacao { get; set; } = string.Empty;
    public string NovoDominio { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
