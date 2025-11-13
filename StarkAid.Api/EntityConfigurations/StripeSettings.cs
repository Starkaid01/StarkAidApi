namespace StarkAid.Api.EntityConfigurations;

public class StripeSettings
{
    public string SecretKey { get; set; } = null!;  // já deve existir
    public string WebhookSecret { get; set; } = null!;  // já deve existir

    // Adicione essas:
    public string PriceId { get; set; } = null!;
    public string PriceIdNivel2 { get; set; } = string.Empty;
    public string PriceIdNivel3 { get; set; } = string.Empty;
    public string PriceIdNivel4 { get; set; } = string.Empty;
    public string PriceIdNivel5 { get; set; } = string.Empty;
    public string PriceIdNivel6 { get; set; } = string.Empty;
    public string PriceIdNivel7 { get; set; } = string.Empty;


    public string CheckoutFrontendUrl { get; set; } = null!;

    public string AppDeepLink { get; set; } = null!;
}

