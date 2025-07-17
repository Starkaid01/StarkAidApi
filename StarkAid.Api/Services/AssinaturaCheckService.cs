using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Services;

public class AssinaturaCheckService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly MercadoPagoService _mercadoPagoService;

    public AssinaturaCheckService(IServiceProvider services, MercadoPagoService mercadoPagoService)
    {
        _services = services;
        _mercadoPagoService = mercadoPagoService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var hoje = DateTime.UtcNow;

            var usuarios = await context.Users
                .Where(u => u.Role == "UserNivel2"
                    && u.UltimoPagamentoConfirmadoEm != null
                    && u.UltimoPagamentoConfirmadoEm.Value.AddDays(30 + 8) < hoje)
                .ToListAsync(stoppingToken);

            foreach (var user in usuarios)
            {
                user.Role = "PagAtrasado";
            }

            foreach (var user in usuarios)
            {
                if (string.IsNullOrEmpty(user.PreapprovalId))
                    continue;

                var assinaturaStatus = await _mercadoPagoService.ConsultarAssinaturaStatusAsync(user.PreapprovalId);

                if (assinaturaStatus != "authorized")
                {
                    user.Role = "PagAtrasado";
                }
            }

            await context.SaveChangesAsync(stoppingToken);

            // Espera 24h
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
