namespace StarkAid.Api.EntityConfigurations;

public class StripeSettings
{
    public string SecretKey { get; set; } = null!;  // já deve existir
    public string WebhookSecret { get; set; } = null!;  // já deve existir

    // Adicione essas:
    public string PriceId { get; set; } = null!;
    public string CheckoutFrontendUrl { get; set; } = null!;

    public string AppDeepLink { get; set; } = null!;
}

