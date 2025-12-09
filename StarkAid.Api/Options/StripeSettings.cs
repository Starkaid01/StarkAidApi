namespace StarkAid.Api.Options;

/// <summary>
/// Configurações do Stripe (chave, preços, URLs etc.).
/// </summary>
public sealed class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PriceIdNivel2 { get; set; } = string.Empty;
    public string PriceIdNivel3 { get; set; } = string.Empty;
    public string PriceIdNivel4 { get; set; } = string.Empty;
    public string PriceIdNivel5 { get; set; } = string.Empty;
    public string PriceIdNivel6 { get; set; } = string.Empty;
    public string PriceIdNivel7 { get; set; } = string.Empty;
    public string CheckoutFrontendUrl { get; set; } = string.Empty;
    public string AppDeepLink { get; set; } = string.Empty;
    public string SoftwareDeepLink { get; set; } = string.Empty;
}
