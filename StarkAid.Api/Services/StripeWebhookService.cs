using global::StarkAid.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Entities;
using Stripe;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StarkAid.Api.Services
{
    public class StripeWebhookService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StripeWebhookService> _logger;
        private readonly string _stripeWebhookSecret;

        public StripeWebhookService(AppDbContext db, ILogger<StripeWebhookService> logger, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            // Garantir que o valor não seja nulo usando o operador de coalescência nula (??) para fornecer um valor padrão.
            _stripeWebhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;

            // Alternativamente, você pode usar o operador de supressão de nulo (!) se tiver certeza de que o valor não será nulo.
            // _stripeWebhookSecret = config["Stripe:WebhookSecret"]!;
        }

        public async Task<string> ProcessWebhookAsync(HttpRequest request)
        {
            _logger.LogInformation("📥 Iniciando processamento do webhook Stripe...");
            Console.WriteLine("Iniciando processamento do webhook Stripe...");
            // 1. Buffering para garantir múltiplas leituras
            request.EnableBuffering();

            string json;
            try
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                json = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Falha ao ler o corpo da requisição do webhook.");
                Console.WriteLine("Erro ao ler o corpo da requisição do webhook: " + ex.Message);
                return "Erro ao ler corpo";
            }

            _logger.LogInformation("✅ Corpo da requisição lido com sucesso. Tamanho: {Length} caracteres", json.Length);
            Console.WriteLine($"Corpo da requisição lido com sucesso. Tamanho: {json.Length} caracteres");

            var headerValues = request.Headers["Stripe-Signature"];
            _logger.LogInformation("Stripe-Signature header count: {Count}", headerValues.Count);
            Console.WriteLine($"Stripe-Signature header count: {headerValues.Count}");
            foreach (var val in headerValues)
            {
                _logger.LogInformation("Stripe-Signature header value: {Value}", val);
                Console.WriteLine($"Stripe-Signature header value: {val}");
            }
            var signatureHeader = headerValues.FirstOrDefault();

            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogError("Header Stripe-Signature está vazio ou nulo.");
                Console.WriteLine("Header Stripe-Signature está vazio ou nulo.");
                return "Assinatura inválida";
            }

            _logger.LogInformation("📄 Assinatura recebida: {Signature}", signatureHeader);
            Console.WriteLine($"Assinatura recebida: {signatureHeader}");

            // Confirma segredo
            _logger.LogInformation("Stripe webhook secret length: {Length}", _stripeWebhookSecret?.Length ?? 0);
            Console.WriteLine($"Stripe webhook secret length: {_stripeWebhookSecret?.Length ?? 0}");
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    _stripeWebhookSecret,
                    tolerance: 300,
                    throwOnApiVersionMismatch: false // ADICIONE ESTE PARÂMETRO
                );
                _logger.LogInformation("🔍 Evento recebido do Stripe: {Type}", stripeEvent.Type);
                Console.WriteLine($"Evento recebido do Stripe: {stripeEvent.Type}");

                // Seu switch para tratar eventos permanece igual
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompleted(stripeEvent);
                        break;

                    case "invoice.payment_succeeded":
                        await HandlePaymentSucceeded(stripeEvent);
                        break;

                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdated(stripeEvent);
                        break;

                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeleted(stripeEvent);
                        break;

                    case "invoice.payment_failed":
                        await HandlePaymentFailed(stripeEvent);
                        break;

                    default:
                        _logger.LogWarning("⚠ Evento não tratado: {EventType}", stripeEvent.Type);
                        Console.WriteLine($"Evento não tratado: {stripeEvent.Type}");
                        break;
                }

                _logger.LogInformation("✅ Webhook Stripe processado com sucesso.");
                Console.WriteLine("Webhook Stripe processado com sucesso.");
                return "Webhook processado";
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "❌ Falha na validação da assinatura do webhook Stripe.");
                Console.WriteLine("Falha na validação da assinatura do webhook Stripe: " + ex.Message);
                return "Assinatura inválida";
            }
        }

        private async Task HandleSubscriptionUpdated(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            var assinatura = await _db.Assinaturas
                .FirstOrDefaultAsync(a => a.StripeSubscriptionId == subscription.Id);

            if (assinatura == null) return;

            // Atualizar status com base no evento do Stripe
            assinatura.Status = subscription.Status switch
            {
                "active" => "Ativa",
                "canceled" => "Cancelada",
                "incomplete" => "Incompleta",
                "incomplete_expired" => "Expirada",
                "past_due" => "Atrasada",
                "unpaid" => "Não Paga",
                _ => assinatura.Status
            };

            // Atualizar datas
            assinatura.ExpiraEm = subscription.CurrentPeriodEnd;

            // Rebaixar usuário se cancelada
            if (subscription.Status == "canceled" || subscription.Status == "unpaid")
            {
                var user = await _db.Users.FindAsync(assinatura.UserId);
                if (user != null) user.Role = "UserNivel1";
            }

            await _db.SaveChangesAsync();
        }

        private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null)
            {
                _logger.LogWarning("⚠ Objeto da sessão de checkout inválido.");
                Console.WriteLine("Objeto da sessão de checkout inválido.");
                return;
            }

            _logger.LogInformation("💳 Checkout concluído. CustomerId: {CustomerId}, SubscriptionId: {SubscriptionId}", session.CustomerId, session.SubscriptionId);
            Console.WriteLine($"Checkout concluído. CustomerId: {session.CustomerId}, SubscriptionId: {session.SubscriptionId}");
            var assinatura = await _db.Assinaturas.FirstOrDefaultAsync(a => a.StripeCustomerId == session.CustomerId);

            if (assinatura == null)
            {
                _logger.LogWarning("⚠ Nenhuma assinatura encontrada para o cliente {CustomerId}", session.CustomerId);
                Console.WriteLine($"Nenhuma assinatura encontrada para o cliente {session.CustomerId}");
                return;
            }

            assinatura.Status = "Ativa";
            assinatura.PagamentoConfirmadoEm = DateTime.UtcNow;
            assinatura.StripeSubscriptionId = session.SubscriptionId;

            var userId = assinatura.UserId;

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("⚠ Usuário não encontrado para o ID {UserId}", userId);
                Console.WriteLine($"Usuário não encontrado para o ID {userId}");
                return; // Adicionado para evitar desreferência nula
            }

            user.Role = "UserNivel2";

            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Assinatura {Id} ativada com sucesso para o cliente {CustomerId}", assinatura.Id, session.CustomerId);
            Console.WriteLine($"Assinatura {assinatura.Id} ativada com sucesso para o cliente {session.CustomerId}");
        }

        private async Task HandlePaymentSucceeded(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("⚠ Objeto da fatura inválido.");
                Console.WriteLine("Objeto da fatura inválido.");
                return;
            }

            _logger.LogInformation("💰 Pagamento confirmado. CustomerId: {CustomerId}, SubscriptionId: {SubscriptionId}", invoice.CustomerId, invoice.SubscriptionId);
            Console.WriteLine($"Pagamento confirmado. CustomerId: {invoice.CustomerId}, SubscriptionId: {invoice.SubscriptionId}");
            var assinatura = await _db.Assinaturas.FirstOrDefaultAsync(a => a.StripeCustomerId == invoice.CustomerId);

            if (assinatura == null)
            {
                _logger.LogWarning("⚠ Nenhuma assinatura encontrada para o cliente {CustomerId}", invoice.CustomerId);
                Console.WriteLine($"Nenhuma assinatura encontrada para o cliente {invoice.CustomerId}");
                return;
            }

            assinatura.Status = "Ativa";
            assinatura.PagamentoConfirmadoEm = DateTime.UtcNow;
            assinatura.ExpiraEm = DateTime.UtcNow.AddMonths(1);

            var userId = assinatura.UserId;
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("⚠ Usuário não encontrado para o ID {UserId}", userId);
                Console.WriteLine($"Usuário não encontrado para o ID {userId}");
                return; // Adicionado para evitar desreferência nula
            }

            user.Role = "UserNivel2";

            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Assinatura {Id} atualizada como ativa até {ExpiraEm}", assinatura.Id, assinatura.ExpiraEm);
            Console.WriteLine($"Assinatura {assinatura.Id} atualizada como ativa até {assinatura.ExpiraEm}");
        }

        private async Task HandlePaymentFailed(Event stripeEvent)
        {
            Console.WriteLine("evento recebido PaymentFailed");
            var invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (invoice == null) return;

            var assinatura = await _db.Assinaturas
                .FirstOrDefaultAsync(a => a.StripeCustomerId == invoice.CustomerId);

            if (assinatura != null)
            {
                assinatura.Status = "vencida";
                await _db.SaveChangesAsync();
            }
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("⚠ Objeto de assinatura inválido.");
                Console.WriteLine("Objeto de assinatura inválido.");
                return;
            }

            _logger.LogInformation("❌ Assinatura cancelada no Stripe. SubscriptionId: {SubscriptionId}", subscription.Id);
            Console.WriteLine($"Assinatura cancelada no Stripe. SubscriptionId: {subscription.Id}");
            var assinatura = await _db.Assinaturas.FirstOrDefaultAsync(a => a.StripeSubscriptionId == subscription.Id);

            if (assinatura == null)
            {
                _logger.LogWarning("⚠ Nenhuma assinatura encontrada para SubscriptionId {SubscriptionId}", subscription.Id);
                Console.WriteLine($"Nenhuma assinatura encontrada para SubscriptionId {subscription.Id}");
                return;
            }

            assinatura.Status = "Cancelada";
            assinatura.CanceladaEm = DateTime.UtcNow;

            var userId = assinatura.UserId;
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("⚠ Usuário não encontrado para o ID {UserId}", userId);
                Console.WriteLine($"Usuário não encontrado para o ID {userId}");
                return; // Adicionado para evitar desreferência nula
            }

            user.Role = "UserNivel1";

            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Assinatura {Id} marcada como cancelada no sistema", assinatura.Id);
            Console.WriteLine($"Assinatura {assinatura.Id} marcada como cancelada no sistema");
        }
    }
}