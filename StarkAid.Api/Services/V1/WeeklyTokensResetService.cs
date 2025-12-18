using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;

namespace StarkAid.Api.Services;

/// <summary>
/// Reseta o consumo semanal de tokens toda segunda-feira às 00:00 (UTC).
/// </summary>
public class WeeklyTokensResetService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyTokensResetService> _logger;
    private DateOnly? _lastResetDate;

    public WeeklyTokensResetService(IServiceProvider serviceProvider, ILogger<WeeklyTokensResetService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var today = DateOnly.FromDateTime(now.UtcDateTime);

                // Rodar somente às segundas-feiras às 00:00 UTC e apenas uma vez no dia
                if (now.DayOfWeek != DayOfWeek.Monday) continue;
                if (now.Hour != 0) continue; // Apenas à meia-noite (00:00)
                if (_lastResetDate.HasValue && _lastResetDate.Value == today) continue;

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var rows = await db.Database.ExecuteSqlRawAsync(
                    $"UPDATE [Users] SET [TokensConsumidosSemana] = 0",
                    cancellationToken: stoppingToken);

                _lastResetDate = today;
                _logger.LogInformation("✅ Reset semanal de tokens executado às {Time} UTC. Registros afetados: {Rows}", now, rows);
            }
            catch (OperationCanceledException)
            {
                // Ignora cancelamentos durante o shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar reset semanal de tokens");
            }
        }
    }
}

