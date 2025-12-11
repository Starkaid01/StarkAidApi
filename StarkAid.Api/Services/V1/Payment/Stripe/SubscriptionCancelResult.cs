namespace StarkAid.Api.Services.V1.Payment.Stripe;

/// <summary>
/// Resultado da operação de cancelamento de assinatura.
/// </summary>
public record SubscriptionCancelResult(Guid SubscriptionId, string LocalStatus, string StripeStatus);
