namespace StarkAid.Api.Services.Payment.Stripe;

/// <summary>
/// Resultado da operação de cancelamento de assinatura.
/// </summary>
public record SubscriptionCancelResult(Guid SubscriptionId, string LocalStatus, string StripeStatus);
