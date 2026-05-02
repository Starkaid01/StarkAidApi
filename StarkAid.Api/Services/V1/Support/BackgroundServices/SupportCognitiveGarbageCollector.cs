using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.SuperIA;

namespace StarkAid.Api.Services.V1.Support.BackgroundServices;

public class SupportCognitiveGarbageCollector : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SupportCognitiveGarbageCollector> _logger;

    public SupportCognitiveGarbageCollector(IServiceProvider serviceProvider, ILogger<SupportCognitiveGarbageCollector> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Iniciando Cognitive Garbage Collector de Suporte...");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var iaService = scope.ServiceProvider.GetRequiredService<IaService>();

                    await ProcessGarbageCollection(context, iaService);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no SupportCognitiveGarbageCollector");
            }

            // Executar uma vez por dia
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task ProcessGarbageCollection(AppDbContext context, IaService iaService)
    {
        var now = DateTime.UtcNow;

        // 1. Quarentena: Score < 50 ou UsageCount baixo (após algum tempo de criação)
        var toQuarantine = await context.SupportLearnings
            .Where(l => !l.IsQuarantined && !l.IsDisabled)
            .Where(l => l.ConfidenceScore < 50 || (l.UsageCount == 0 && l.CreatedAt < now.AddDays(-3)))
            .ToListAsync();

        foreach (var item in toQuarantine)
        {
            item.IsQuarantined = true;
            _logger.LogInformation("Item de aprendizado {Id} movido para quarentena.", item.Id);
        }

        // 2. Desativação: 7 dias na quarentena sem uso
        var toDisable = await context.SupportLearnings
            .Where(l => l.IsQuarantined && !l.IsDisabled)
            .Where(l => l.LastUsedAt == null || l.LastUsedAt < now.AddDays(-7))
            .ToListAsync();

        foreach (var item in toDisable)
        {
            item.IsDisabled = true;
            _logger.LogInformation("Item de aprendizado {Id} desativado após quarentena.", item.Id);
        }

        // 3. Promoção: Score > 80 e UsageCount alto
        var toPromote = await context.SupportLearnings
            .Where(l => !l.IsGlobal && !l.IsDisabled && !l.IsQuarantined)
            .Where(l => l.ConfidenceScore > 80 && l.UsageCount > 10)
            .ToListAsync();

        foreach (var item in toPromote)
        {
            // Limpar gírias e validar privacidade usando IA
            string cleanedResponse = await CleanAndAnonymize(iaService, item.IAResponseTxt);
            
            item.IAResponseTxt = cleanedResponse;
            item.IsGlobal = true;
            item.UserId = null; // Torna global de verdade
            _logger.LogInformation("Item de aprendizado {Id} promovido para Global.", item.Id);
        }

        await context.SaveChangesAsync();
    }

    private async Task<string> CleanAndAnonymize(IaService iaService, string text)
    {
        var prompt = new List<object>
        {
            new { role = "system", content = "Você é um sanitizador de dados. Remova gírias excessivas, mantenha o tom profissional e OBRIGATORIAMENTE remova qualquer dado pessoal (nomes, emails, documentos, endereços). Retorne APENAS o texto limpo." },
            new { role = "user", content = text }
        };

        var result = await iaService.ChamarOpenRouter(prompt.ToArray());
        return result?.Texto ?? text;
    }
}
