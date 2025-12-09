namespace StarkAid.Api.Options;

/// <summary>
/// Opções de integração ao NLP.
/// </summary>
public sealed class NlpConnectOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenDeAutenticacao { get; set; } = string.Empty;
    public string NovoDominio { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
