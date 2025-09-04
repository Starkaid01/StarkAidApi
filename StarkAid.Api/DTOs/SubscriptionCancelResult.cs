namespace StarkAid.Api.DTOs;

public record SubscriptionCancelResult(
    Guid SubscriptionId,
    string LocalStatus,
    string StripeStatus
);
