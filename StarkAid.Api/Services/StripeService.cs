using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;
using Stripe;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StarkAid.Api.Services
{
    public class StripeService
    {
        private readonly CustomerService _customerService;
        private readonly Stripe.Checkout.SessionService _sessionService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<StripeService> _logger;

        private readonly StripeClient _client;
        private readonly StripeSettings _stripeSettings;

        public StripeService(IOptions<StripeSettings> options, ILogger<StripeService> logger)
        {

            _stripeSettings = options.Value; // Armazene as configurações
            StripeConfiguration.ApiKey = options.Value.SecretKey;

            _client = new StripeClient(options.Value.SecretKey);

            _customerService = new CustomerService();
            _sessionService = new Stripe.Checkout.SessionService();
            _subscriptionService = new SubscriptionService();
            _logger = logger;
        }

        public async Task<(Stripe.Checkout.Session session, Stripe.Customer customer)> CreateCheckoutSessionAsync(
            User user,
            string priceId,
            string successUrl,
            string cancelUrl,
            string? existingCustomerId = null)
        {
            _logger.LogInformation("Iniciando criação de sessão de checkout para {Email}", user.Email);

            Stripe.Customer customer;

            if (!string.IsNullOrEmpty(existingCustomerId))
            {
                _logger.LogInformation("Usando cliente existente: {CustomerId}", existingCustomerId);
                customer = await _customerService.GetAsync(existingCustomerId);
            }
            else
            {
                _logger.LogInformation("Criando novo cliente Stripe para {Email}", user.Email);
                customer = await _customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = user.Name
                });
            }

            var sessionOptions = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Customer = customer.Id
            };

            _logger.LogDebug("Opções da sessão: {@SessionOptions}", sessionOptions);

            var session = await _sessionService.CreateAsync(sessionOptions);

            _logger.LogInformation("Sessão de checkout criada com sucesso: {SessionId}", session.Id);

            return (session, customer);
        }

        public async Task<Stripe.Checkout.Session?> GetSessionAsync(string sessionId)
        {
            _logger.LogInformation("Obtendo sessão do Stripe: {SessionId}", sessionId);
            return await _sessionService.GetAsync(sessionId);
        }

        public async Task<Stripe.Subscription?> CancelSubscriptionAsync(string subscriptionId)
        {
            _logger.LogWarning("Cancelando assinatura no Stripe: {SubscriptionId}", subscriptionId);
            Console.WriteLine($"Cancelando assinatura no Stripe: {subscriptionId}");
            return await _subscriptionService.CancelAsync(subscriptionId);
        }

        public async Task CancelSubscriptionImmediately(string subscriptionId)
        {
            try
            {                
                Console.WriteLine($"Iniciando cancelamento imediato: {subscriptionId}");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _stripeSettings.SecretKey);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("invoice_now", "true"),
                    new KeyValuePair<string, string>("prorate", "true")
                });

                var response = await httpClient.PostAsync(
                    $"https://api.stripe.com/v1/subscriptions/{subscriptionId}/cancel",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();                   
                    Console.WriteLine($"Falha no cancelamento: {response.StatusCode} - {errorContent}");

                    throw new InvalidOperationException($"Stripe error: {response.StatusCode}");
                }

                _logger.LogInformation($"Assinatura {subscriptionId} cancelada com sucesso");
                Console.WriteLine($"Assinatura {subscriptionId} cancelada com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao cancelar assinatura via API direta: {ex.Message}");
                throw;
            }
        }
    }
}
