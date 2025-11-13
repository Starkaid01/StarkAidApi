using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Headers;

namespace StarkAid.Api.Services.Assinatura
{
    public class StripeService
    {
        private readonly CustomerService _customerService;
        private readonly SessionService _sessionService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<StripeService> _logger;
        private readonly StripeClient _client;
        private readonly StripeSettings _stripeSettings;

        public StripeService(IOptions<StripeSettings> options, ILogger<StripeService> logger)
        {
            _stripeSettings = options.Value;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            _client = new StripeClient(_stripeSettings.SecretKey);
            _customerService = new CustomerService(_client);
            _sessionService = new SessionService(_client);
            _subscriptionService = new SubscriptionService(_client);
            _logger = logger;
        }

        /// <summary>
        /// Cria uma sessão de checkout no Stripe.
        /// </summary>
        public async Task<(Session session, Customer customer)> CreateCheckoutSessionAsync(
            User user,
            string priceId,
            string successUrl,
            string cancelUrl,
            string? existingCustomerId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando criação de sessão de checkout para {Email}", user.Email);

                Customer customer;

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

                var sessionOptions = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = priceId,
                            Quantity = 1
                        }
                    },
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Customer = customer.Id,
                    AllowPromotionCodes = true
                };

                _logger.LogDebug("Configurando sessão Stripe: {@SessionOptions}", sessionOptions);

                var session = await _sessionService.CreateAsync(sessionOptions);

                _logger.LogInformation("Sessão de checkout criada com sucesso: {SessionId}", session.Id);

                return (session, customer);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro Stripe ao criar sessão de checkout: {Message}", ex.Message);
                throw new InvalidOperationException($"Erro Stripe: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro geral ao criar sessão Stripe");
                throw;
            }
        }

        /// <summary>
        /// Recupera informações de uma sessão do Stripe.
        /// </summary>
        public async Task<Session?> GetSessionAsync(string sessionId)
        {
            try
            {
                _logger.LogInformation("Obtendo sessão Stripe: {SessionId}", sessionId);
                return await _sessionService.GetAsync(sessionId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro ao obter sessão Stripe: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cancela uma assinatura Stripe pelo ID.
        /// </summary>
        public async Task<Subscription?> CancelSubscriptionAsync(string subscriptionId)
        {
            try
            {
                _logger.LogWarning("Cancelando assinatura no Stripe: {SubscriptionId}", subscriptionId);
                return await _subscriptionService.CancelAsync(subscriptionId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro Stripe ao cancelar assinatura: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cancela imediatamente uma assinatura Stripe via API HTTP direta (forçando faturamento atual).
        /// </summary>
        public async Task CancelSubscriptionImmediately(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Iniciando cancelamento imediato de assinatura: {SubscriptionId}", subscriptionId);

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
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro no cancelamento imediato: {Status} - {Error}", response.StatusCode, error);
                    throw new InvalidOperationException($"Falha no cancelamento Stripe: {response.StatusCode}");
                }

                _logger.LogInformation("Assinatura {SubscriptionId} cancelada imediatamente com sucesso", subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar assinatura imediatamente");
                throw;
            }
        }

        public async Task<(Session session, Customer customer)> CreateOneTimePaymentSessionAsync(
            User user,
            decimal valor,
            string successUrl,
            string cancelUrl)
        {
            _logger.LogInformation("Criando pagamento avulso de {Valor} para {Email}", valor, user.Email);

            // 1. Cria cliente (ou reutiliza se já existir)
            var customerList = await _customerService.ListAsync(new CustomerListOptions { Email = user.Email });
            var existingCustomer = customerList.Data.FirstOrDefault();
            var customer = existingCustomer ?? await _customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.Name
            });

            // 2. Define valor (Stripe usa centavos)
            long amountInCents = (long)(valor * 100);

            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "brl",
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Crédito StarkCoins (R$ {valor:F2})"
                            }
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Customer = customer.Id
            };

            var session = await _sessionService.CreateAsync(sessionOptions);

            _logger.LogInformation("Sessão de pagamento avulso criada: {SessionId}", session.Id);

            return (session, customer);
        }
    }
}
