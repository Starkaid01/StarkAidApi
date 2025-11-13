namespace StarkAid.Api.DTOs.Assinatura;

public record SubscriptionCancelResult(
    Guid SubscriptionId,
    string LocalStatus,
    string StripeStatus
);
