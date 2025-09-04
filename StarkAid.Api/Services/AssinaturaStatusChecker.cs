using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;

namespace StarkAid.Api.Services;

public class AssinaturaStatusChecker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly TimeSpan _interval = TimeSpan.FromHours(12);

    public AssinaturaStatusChecker(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1) Se assinaturas vencidas e ExpiraEm + 5 dias < now => colocar UserNivel3
                var now = DateTimeOffset.UtcNow;
                var atrasoThreshold = now.AddDays(-5);

                var atrasadas = await db.Assinaturas
                    .Where(a => a.Status == "vencida" && a.ExpiraEm.HasValue && a.ExpiraEm.Value < atrasoThreshold)
                    .ToListAsync(stoppingToken);

                foreach (var a in atrasadas)
                {
                    var user = await db.Users.FindAsync(a.UserId);
                    if (user != null && user.Role != "UserNivel3")
                    {
                        user.Role = "UserNivel3";
                    }
                }

                // 2) Garantir que assinaturas ativas mantenham UserNivel2
                var ativas = await db.Assinaturas
                    .Where(a => a.Status == "ativa")
                    .ToListAsync(stoppingToken);

                foreach (var a in ativas)
                {
                    var user = await db.Users.FindAsync(a.UserId);
                    if (user != null && user.Role != "UserNivel2")
                    {
                        user.Role = "UserNivel2";
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // aqui você pode logar via ILogger (omiti para manter código enxuto)
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
