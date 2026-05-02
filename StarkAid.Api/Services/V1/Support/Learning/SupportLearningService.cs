using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.SuperIA;
using Microsoft.Extensions.Logging;

namespace StarkAid.Api.Services.V1.Support.Learning;

public interface ISupportLearningService
{
    Task SaveLearningAsync(string input, string response, string contextTitle, Guid? userId, bool isGlobal);
    Task<string?> GetLearnedResponseAsync(string input, string contextTitle, Guid? userId);
    Task GenerateVariationsAsync(int learningId);
}

public class SupportLearningService : ISupportLearningService
{
    private readonly AppDbContext _context;
    private readonly IaService _iaService;
    private readonly ILogger<SupportLearningService> _logger;

    public SupportLearningService(AppDbContext context, IaService iaService, ILogger<SupportLearningService> logger)
    {
        _context = context;
        _iaService = iaService;
        _logger = logger;
    }

    public async Task SaveLearningAsync(string input, string response, string contextTitle, Guid? userId, bool isGlobal)
    {
        // Evitar duplicados exatos para o mesmo usuário/global e contexto
        var exists = await _context.SupportLearnings
            .AnyAsync(l => l.UserEntradaTxt == input && l.ContextTitle == contextTitle && l.UserId == userId && l.IsGlobal == isGlobal);

        if (exists) return;

        var learning = new SupportLearning
        {
            UserId = userId,
            UserEntradaTxt = input,
            IAResponseTxt = response,
            ContextTitle = contextTitle,
            ConfidenceScore = 100,
            UsageCount = 1,
            IsGlobal = isGlobal,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _context.SupportLearnings.Add(learning);
        await _context.SaveChangesAsync();

        // Gerar variações (Engine de Variações)
        _ = Task.Run(async () => await GenerateVariationsAsync(learning.Id));
    }

    public async Task<string?> GetLearnedResponseAsync(string input, string contextTitle, Guid? userId)
    {
        // 1. Tentar aprendizado do usuário no contexto
        var learned = await _context.SupportLearnings
            .Where(l => !l.IsDisabled && !l.IsQuarantined)
            .Where(l => l.UserId == userId && l.ContextTitle == contextTitle)
            .OrderByDescending(l => l.ConfidenceScore)
            .FirstOrDefaultAsync(l => l.UserEntradaTxt.ToLower() == input.ToLower());

        if (learned == null)
        {
            // 2. Tentar aprendizado global no contexto
            learned = await _context.SupportLearnings
                .Where(l => !l.IsDisabled && !l.IsQuarantined)
                .Where(l => l.IsGlobal == true && l.ContextTitle == contextTitle)
                .OrderByDescending(l => l.ConfidenceScore)
                .FirstOrDefaultAsync(l => l.UserEntradaTxt.ToLower() == input.ToLower());
        }

        if (learned != null)
        {
            learned.UsageCount++;
            learned.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return learned.IAResponseTxt;
        }

        return null;
    }

    public async Task GenerateVariationsAsync(int learningId)
    {
        try
        {
            // Usar um novo scope se necessário, mas aqui assumimos que o logger e context são de curta duração ou injetados corretamente
            // Para background task em ASP.NET Core, o ideal seria usar IServiceScopeFactory
            
            var learning = await _context.SupportLearnings.FindAsync(learningId);
            if (learning == null) return;

            // Chamar IA para gerar 3-4 variações
            var variacoes = await _iaService.GerarVariacoesParaGlobal(learning.UserEntradaTxt, learning.IAResponseTxt);
            
            foreach (var v in variacoes)
            {
                if (string.IsNullOrWhiteSpace(v) || v.Equals(learning.UserEntradaTxt, StringComparison.OrdinalIgnoreCase)) continue;

                // Verificar se variação já existe
                var exists = await _context.SupportLearnings.AnyAsync(l => l.UserEntradaTxt == v && l.ContextTitle == learning.ContextTitle);
                if (exists) continue;

                var vLearning = new SupportLearning
                {
                    UserId = learning.UserId,
                    UserEntradaTxt = v,
                    IAResponseTxt = learning.IAResponseTxt,
                    ContextTitle = learning.ContextTitle,
                    ConfidenceScore = 85, // Variações geradas por IA começam com score menor
                    UsageCount = 0,
                    IsGlobal = learning.IsGlobal,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SupportLearnings.Add(vLearning);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Geradas {Count} variações para aprendizado de suporte {Id}", variacoes.Count, learningId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar variações para aprendizado de suporte {Id}", learningId);
        }
    }
}
