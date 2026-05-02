using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using Microsoft.Extensions.Logging;

namespace StarkAid.Api.Services.V1.Support.Heuristics;

public record HeuristicResult(string Message, string? ActionToPropose = null, bool NeedsConfirmation = true);

public interface ISupportHeuristicService
{
    Task<HeuristicResult?> EvaluateAsync(Guid userId, string input, string origem);
}

public class SupportHeuristicService : ISupportHeuristicService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupportHeuristicService> _logger;

    public SupportHeuristicService(AppDbContext context, ILogger<SupportHeuristicService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HeuristicResult?> EvaluateAsync(Guid userId, string input, string origem)
    {
        var inputLower = input.ToLower().Trim();

        // 1. Reclamação de comando não funciona
        if (inputLower.Contains("não funciona") || inputLower.Contains("nao funciona") || inputLower.Contains("não está funcionando") || inputLower.Contains("comando falhou"))
        {
            var lastFalha = await _context.LogsFalhasSoft
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastFalha != null && !string.IsNullOrEmpty(lastFalha.DispositivoNome))
            {
                // Tenta encontrar dispositivo com nome similar
                var dispositivosVisiveis = await _context.DispositivosEsp
                    .Where(d => d.UserId == userId)
                    .Select(d => d.Nome)
                    .ToListAsync();
                
                var similar = dispositivosVisiveis.FirstOrDefault(d => 
                    CalculateSimilarity(d.ToLower(), lastFalha.DispositivoNome.ToLower()) > 0.7);

                if (similar != null && similar.ToLower() != lastFalha.DispositivoNome.ToLower())
                {
                    return new HeuristicResult(
                        $"Notei que você tentou controlar '{lastFalha.DispositivoNome}', mas encontrei um dispositivo chamado '{similar}' em sua conta. Deseja que eu tente corrigir o nome para você?",
                        $"UpdateDeviceName:{lastFalha.DispositivoNome}:{similar}",
                        true);
                }
            }
        }

        // 2. Comandos não aparecem / não consigo editar
        if (inputLower.Contains("comando") && (inputLower.Contains("aparece") || inputLower.Contains("sumiu") || inputLower.Contains("editar") || inputLower.Contains("carrega")))
        {
            return new HeuristicResult(
                "Identifiquei que seus comandos podem estar com falha de sincronização. Posso tentar uma limpeza profunda no banco de dados local para recarregar tudo do servidor. Isso geralmente resolve quando os comandos somem ou não podem ser editados. Posso prosseguir?",
                "CleanLocalDatabase",
                true);
        }

        // 3. App travou / bugou
        if (inputLower.Contains("travou") || inputLower.Contains("bugou") || inputLower.Contains("está lento") || inputLower.Contains("esta lento"))
        {
            return new HeuristicResult(
                "Sinto muito que o app esteja apresentando instabilidades. Posso tentar reiniciar o aplicativo remotamente para você. Deseja tentar?",
                "RestartApp",
                true);
        }

        return null; // Nenhuma heurística aplicada
    }

    // Levenshtein distance similarity
    private double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
        if (source == target) return 1.0;

        int n = source.Length;
        int m = target.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return 1.0 - ((double)d[n, m] / Math.Max(n, m));
    }
}
