using StarkAid.Api.Data;

namespace StarkAid.Api.Services.V1;

public class PasswordResetCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PasswordResetCleanupService> _logger;

    public PasswordResetCleanupService(IServiceProvider serviceProvider, ILogger<PasswordResetCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PasswordResetCleanupService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var expirados = context.PasswordResetTokens
                    .Where(t => t.Expiration < DateTime.UtcNow)
                    .ToList();

                if (expirados.Any())
                {
                    context.PasswordResetTokens.RemoveRange(expirados);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Removidos {Count} tokens expirados", expirados.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar tokens expirados");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }

        _logger.LogInformation("PasswordResetCleanupService finalizado.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PasswordResetCleanupService está sendo finalizado...");
        await base.StopAsync(cancellationToken);
    }
}
