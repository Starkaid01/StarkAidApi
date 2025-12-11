using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using System;

namespace StarkAid.Api.Services.V1.Assinatura;

public class AssinaturaStatusChecker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AssinaturaStatusChecker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(12);

    public AssinaturaStatusChecker(IServiceProvider services, ILogger<AssinaturaStatusChecker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTimeOffset.UtcNow;

                _logger.LogInformation("🔍 Iniciando verificação de assinaturas às {Time}", now);

                // 1️⃣ Marcar assinaturas expiradas como vencidas
                var vencidas = await db.Assinaturas
                    .Where(a => a.Status == "ativa" && a.ExpiraEm.HasValue && a.ExpiraEm.Value < now)
                    .ToListAsync(stoppingToken);

                foreach (var a in vencidas)
                {
                    a.Status = "vencida";
                    _logger.LogWarning("⚠ Assinatura {Id} vencida em {ExpiraEm}", a.Id, a.ExpiraEm);
                }

                // 2️⃣ Rebaixar usuários vencidos há mais de 5 dias - APENAS se for plano Remove Ads (valor 10)
                // Planos de StarkCoins (níveis 3-7) apenas param de adicionar StarkCoins, NÃO rebaixam o usuário
                var atrasoThreshold = now.AddDays(-5);
                var atrasadas = await db.Assinaturas
                    .Where(a => a.Status == "vencida" && a.ExpiraEm.HasValue && a.ExpiraEm.Value < atrasoThreshold)
                    .Include(a => a.User)
                    .ToListAsync(stoppingToken);

                foreach (var a in atrasadas)
                {
                    var user = a.User;
                    if (user == null) continue;

                    // ⚠️ Apenas rebaixar se for plano Remove Ads (valor 10)
                    // Planos de StarkCoins (5, 15, 25, 50, 100) apenas cancelam, não rebaixam
                    if (a.Valor == 10 && user.Role != "UserNivel1")
                    {
                        _logger.LogWarning("⬇ Rebaixando usuário {UserId} para UserNivel1 (plano Remove Ads vencido há mais de 5 dias)", user.Id);
                        user.Role = "UserNivel1";
                        user.RemovalAds = "Desativado";
                    }
                    else if (a.Valor != 10)
                    {
                        _logger.LogInformation("📋 Plano de StarkCoins {Id} (valor {Valor}) vencido - apenas cancelado, usuário não rebaixado", a.Id, a.Valor);
                    }
                }

                // 3️⃣ Garantir status consistente para assinaturas ativas
                // ⚠️ Role do usuário NÃO é atualizado para planos de StarkCoins (níveis 3-7)
                // Role só muda para UserNivel2 quando há plano Remove Ads (valor 10) ativo
                var ativas = await db.Assinaturas
                    .Where(a => a.Status == "ativa" || a.Status == "Ativa")
                    .Include(a => a.User)
                    .ToListAsync(stoppingToken);

                foreach (var a in ativas)
                {
                    var user = a.User;
                    if (user == null) continue;

                    // Verificar se realmente está ativa (não expirada)
                    bool notExpired = !a.ExpiraEm.HasValue || a.ExpiraEm.Value > now;

                    // Apenas para plano Remove Ads (valor 10): atualiza RemovalAds e Role se necessário
                    if (a.Valor == 10 && notExpired)
                    {
                        user.RemovalAds = "Ativo";
                        // Se Role for UserNivel1, atualiza para UserNivel2
                        if (user.Role == "UserNivel1")
                        {
                            user.Role = "UserNivel2";
                        }
                    }
                    // Planos de StarkCoins (5, 15, 25, 50, 100) NÃO alteram o Role do usuário
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("✅ Verificação de assinaturas concluída com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao executar AssinaturaStatusChecker");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
