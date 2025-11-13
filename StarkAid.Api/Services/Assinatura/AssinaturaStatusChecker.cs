using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;

namespace StarkAid.Api.Services.Assinatura;

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

                // 2️⃣ Rebaixar usuários vencidos há mais de 5 dias
                var atrasoThreshold = now.AddDays(-5);
                var atrasadas = await db.Assinaturas
                    .Where(a => a.Status == "vencida" && a.ExpiraEm.HasValue && a.ExpiraEm.Value < atrasoThreshold)
                    .Include(a => a.User)
                    .ToListAsync(stoppingToken);

                foreach (var a in atrasadas)
                {
                    var user = a.User;
                    if (user == null) continue;

                    if (user.Role != "UserNivel1")
                    {
                        _logger.LogWarning("⬇ Rebaixando usuário {UserId} (assinatura {Id} vencida há mais de 5 dias)", user.Id, a.Id);
                        user.Role = "UserNivel1";
                        if(user.RemovalAds == "Ativo")
                        {
                            user.RemovalAds = "Desativado";
                        }
                    }
                }

                // 3️⃣ Garantir roles consistentes para assinaturas ativas
                var ativas = await db.Assinaturas
                    .Where(a => a.Status == "ativa")
                    .Include(a => a.User)
                    .ToListAsync(stoppingToken);

                foreach (var a in ativas)
                {
                    var user = a.User;
                    if (user == null) continue;

                    switch (a.Valor)
                    {
                        case 5:
                            user.Role = "UserNivel3";
                            break;
                        case 10:
                            user.RemovalAds = "Ativo";
                            break;
                        case 15:
                            user.Role = "UserNivel4";
                            break;
                        case 25:
                            user.Role = "UserNivel5";
                            break;
                        case 50:
                            user.Role = "UserNivel6";
                            break;
                        case 100:
                            user.Role = "UserNivel7";
                            break;
                    }
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
