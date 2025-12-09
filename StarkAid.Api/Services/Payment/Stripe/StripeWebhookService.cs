using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;
using StarkAid.Api.Options;
using Stripe;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.Assinatura
{
    public class StripeWebhookService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StripeWebhookService> _logger;
        private readonly string _stripeWebhookSecret;
        private readonly StripeSettings _stripeSettings;
        private readonly StarkAid.Api.Services.Notifications.NotificationService? _notificationService;

        public StripeWebhookService(
            AppDbContext db, 
            ILogger<StripeWebhookService> logger, 
            IConfiguration config, 
            StripeSettings stripeSettings,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _logger = logger;
            // Garantir que o valor não seja nulo usando o operador de coalescência nula (??) para fornecer um valor padrão.
            _stripeWebhookSecret = config["StripeSettings:WebhookSecret"] ?? string.Empty;
            _stripeSettings = stripeSettings;
            
            // Obter NotificationService via service provider (pode ser null se não estiver registrado)
            try
            {
                _notificationService = serviceProvider.GetService<StarkAid.Api.Services.Notifications.NotificationService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotificationService não disponível");
                _notificationService = null;
            }

            _logger.LogInformation("Stripe SecretKey carregada: {SecretKeyLength} caracteres",
                _stripeSettings.SecretKey?.Length ?? 0);
            _logger.LogInformation("PriceIdNivel2: {PriceId}", _stripeSettings.PriceIdNivel2);
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
            var subscription = stripeEvent.Data.Object as Subscription;
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

            // Rebaixar usuário se cancelada - apenas se for plano Remove Ads (valor 10)
            // Planos de StarkCoins (níveis 3-7) não alteram o Role ao cancelar
            if ((subscription.Status == "canceled" || subscription.Status == "unpaid") && assinatura.Valor == 10)
            {
                var user = await _db.Users.FindAsync(assinatura.UserId);
                if (user != null)
                {
                    user.Role = "UserNivel1";
                    user.RemovalAds = "Desativado";
                    _logger.LogInformation("⬇ Usuário {UserId} rebaixado para UserNivel1 (plano Remove Ads cancelado)", user.Id);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            try
            {
                if (stripeEvent.Data.Object is not Stripe.Checkout.Session session)
                {
                    _logger.LogWarning("⚠️ Evento Stripe sem sessão válida: {EventId}", stripeEvent.Id);
                    return;
                }

                var customerId = session.CustomerId;
                var subscriptionId = session.SubscriptionId;

                if (string.IsNullOrEmpty(customerId))
                {
                    _logger.LogWarning("⚠️ Checkout sem CustomerId, evento ignorado.");
                    return;
                }

                _logger.LogInformation("✅ Checkout concluído para CustomerId={CustomerId}, SubscriptionId={SubscriptionId}", customerId, subscriptionId);

                // 🔹 1. Tenta encontrar uma assinatura pendente
                var assinatura = await _db.Assinaturas
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.StripeCustomerId == customerId && a.Status == "pendente");

                // 🔹 2. Caso não seja uma assinatura, tenta encontrar um pagamento avulso
                var pagamentoAvulso = await _db.PagamentosAvulsos
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.StripeCustomerId == customerId && p.Status == "pendente");

                if (assinatura == null && pagamentoAvulso == null)
                {
                    _logger.LogWarning("⚠️ Nenhum registro pendente encontrado para CustomerId={CustomerId}", customerId);
                    return;
                }

                // 🔹 3A. Processa assinatura normal
                if (assinatura != null)
                {
                    var user = assinatura.User!;
                    assinatura.Status = "Ativa";
                    assinatura.PagamentoConfirmadoEm = DateTimeOffset.UtcNow;
                    assinatura.IniciadaEm ??= DateTimeOffset.UtcNow;
                    assinatura.StripeSubscriptionId = subscriptionId;
                    assinatura.ExpiraEm = DateTimeOffset.UtcNow.AddMonths(1);
                    assinatura.TipoPlano = assinatura.Valor switch
                    {
                        10 => "Nivel 2",
                        5 => "Nivel 3",
                        15 => "Nivel 4",
                        25 => "Nivel 5",
                        50 => "Nivel 6",
                        100 => "Nivel 7",
                        _ => "Custom"
                    };

                    // Adiciona StarkCoins apenas se não for plano sem anúncios
                    if (assinatura.Valor != 10)
                    {
                        user.StarkCoins += assinatura.Valor;
                        _logger.LogInformation("💰 Plano {TipoPlano}: adicionados {Valor} StarkCoins para usuário {UserId}",
                            assinatura.TipoPlano, assinatura.Valor, user.Id);
                    }
                    else
                    {
                        if (user.Role == "UserNivel1")
                        {
                            user.Role = "UserNivel2";
                        }
                        user.RemovalAds = "Ativo";
                    }

                    _logger.LogInformation("🌟 Assinatura {Id} confirmada e ativada para usuário {UserId}", assinatura.Id, user.Id);
                    
                    // Criar notificação para administrador
                    if (_notificationService != null)
                    {
                        try
                        {
                            await _notificationService.CriarNotificacaoAsync(
                                "assinatura",
                                $"Nova Assinatura - {assinatura.TipoPlano}",
                                $"Usuário {user.Name} ({user.Email}) assinou o plano {assinatura.TipoPlano} por R$ {assinatura.Valor:F2}/mês.",
                                user.Id,
                                user.Email,
                                user.Name,
                                assinatura.Valor,
                                assinatura.Id.ToString()
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de assinatura");
                        }
                    }
                }

                // 🔹 3B. Processa pagamento avulso (adiciona StarkCoins)
                if (pagamentoAvulso != null)
                {
                    var user = pagamentoAvulso.User!;
                    pagamentoAvulso.Status = "Pago";
                    pagamentoAvulso.PagamentoConfirmadoEm = DateTimeOffset.UtcNow;

                    // 🔹 Adiciona StarkCoins proporcionalmente ao valor
                    user.StarkCoins += pagamentoAvulso.Valor;

                    _db.Users.Update(user);
                    _logger.LogInformation("💰 Pagamento avulso confirmado. +{Valor} StarkCoins para usuário {UserId}",
                        pagamentoAvulso.Valor, user.Id);
                    
                    // Criar notificação para administrador
                    if (_notificationService != null)
                    {
                        try
                        {
                            await _notificationService.CriarNotificacaoAsync(
                                "pagamento_avulso",
                                "Adição de Fundos",
                                $"Usuário {user.Name} ({user.Email}) adicionou R$ {pagamentoAvulso.Valor:F2} em StarkCoins.",
                                user.Id,
                                user.Email,
                                user.Name,
                                pagamentoAvulso.Valor,
                                pagamentoAvulso.Id.ToString()
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de pagamento avulso");
                        }
                    }
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("✅ Atualizações de pagamento aplicadas com sucesso para CustomerId={CustomerId}", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar checkout.session.completed");
            }
        }

        private async Task HandlePaymentSucceeded(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("⚠ Objeto da fatura inválido.");
                return;
            }

            _logger.LogInformation("💰 Pagamento confirmado. CustomerId: {CustomerId}, SubscriptionId: {SubscriptionId}",
                invoice.CustomerId, invoice.SubscriptionId);

            var assinatura = await _db.Assinaturas
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.StripeCustomerId == invoice.CustomerId);

            if (assinatura == null)
            {
                _logger.LogWarning("⚠ Nenhuma assinatura encontrada para o cliente {CustomerId}", invoice.CustomerId);
                return;
            }

            var user = assinatura.User;
            if (user == null)
            {
                _logger.LogWarning("⚠ Usuário não encontrado para a assinatura {AssinaturaId}", assinatura.Id);
                return;
            }

            // Atualiza informações da assinatura
            assinatura.Status = "Ativa";
            assinatura.PagamentoConfirmadoEm = DateTimeOffset.UtcNow;
            assinatura.ExpiraEm = DateTimeOffset.UtcNow.AddMonths(1);
            assinatura.TipoPlano = assinatura.Valor switch
            {
                10 => "Nivel 2",
                5 => "Nivel 3",
                15 => "Nivel 4",
                25 => "Nivel 5",
                50 => "Nivel 6",
                100 => "Nivel 7",
                _ => assinatura.TipoPlano
            };

            // 🔹 Verifica se é o primeiro pagamento (ou uma renovação)
            bool primeiraVez = assinatura.IniciadaEm == null;

            if (primeiraVez)
            {
                assinatura.IniciadaEm = DateTimeOffset.UtcNow;

                // 🆕 Primeira assinatura: apenas adiciona StarkCoins
                if (assinatura.Valor != 10) // exceto plano sem anúncios
                {
                    user.StarkCoins += assinatura.Valor;
                }
                else
                {
                    if (user.Role == "UserNivel1")
                    {
                        user.Role = "UserNivel2";
                    }
                    user.RemovalAds = "Ativo";
                }

                _logger.LogInformation("🆕 Primeira assinatura: adicionando {ValorPlano} StarkCoins ao usuário {UserId}",
                    assinatura.Valor, user.Id);
            }
            else
            {
                if (assinatura.Valor != 10) // exceto plano sem anúncios
                {
                    user.StarkCoins += assinatura.Valor;
                }
                else
                {
                    if (user.Role == "UserNivel1")
                    {
                        user.Role = "UserNivel2";
                    }
                    user.RemovalAds = "Ativo";
                }
                _logger.LogInformation("🔁 Renovação: saldo ajustado para {StarkCoins} após cobrança do plano {ValorPlano}",
                    user.StarkCoins, assinatura.Valor);
            }

            // ⚠️ Role do usuário NÃO é atualizado para planos de StarkCoins (níveis 3-7)
            // Role só muda para UserNivel2 quando há plano Remove Ads (valor 10) ativo
            // Se o plano remove ads atrasar, volta para UserNivel1

            user.LastUpdatedAt = DateTimeOffset.UtcNow;

            _db.Assinaturas.Update(assinatura);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Assinatura {Id} processada com sucesso até {ExpiraEm}", assinatura.Id, assinatura.ExpiraEm);
        }

        private async Task HandlePaymentFailed(Event stripeEvent)
        {
            Console.WriteLine("evento recebido PaymentFailed");
            var invoice = stripeEvent.Data.Object as Invoice;
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
            var subscription = stripeEvent.Data.Object as Subscription;
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

            // ⚠️ Apenas rebaixar Role se for o plano Remove Ads (valor 10)
            // Planos de StarkCoins (níveis 3-7) não alteram o Role ao cancelar
            if (assinatura.Valor == 10)
            {
                user.Role = "UserNivel1";
                user.RemovalAds = "Desativado";
                _logger.LogInformation("⬇ Usuário {UserId} rebaixado para UserNivel1 (plano Remove Ads cancelado)", userId);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ Assinatura {Id} marcada como cancelada no sistema", assinatura.Id);
            Console.WriteLine($"Assinatura {assinatura.Id} marcada como cancelada no sistema");
        }
    }
}
