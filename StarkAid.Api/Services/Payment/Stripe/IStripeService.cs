using StarkAid.Api.Entities;
using Stripe;
using Stripe.Checkout;

namespace StarkAid.Api.Services.Payment.Stripe;

public interface IStripeService
{
    Task<(Session Session, Customer Customer)> CreateCheckoutAsync(
        User user,
        string priceId,
        string successUrl,
        string cancelUrl,
        string? existingCustomerId = null);

    Task<(Session Session, Customer Customer)> CreateOneTimePaymentAsync(
        User user,
        decimal amount,
        string successUrl,
        string cancelUrl);

    Task<Session?> GetSessionAsync(string sessionId);
    Task<Subscription?> CancelSubscriptionAsync(string subscriptionId);
    Task CancelSubscriptionImmediatelyAsync(string subscriptionId);
}
