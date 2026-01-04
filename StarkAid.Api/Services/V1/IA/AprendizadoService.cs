using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1.IA
{
    public class AprendizadoSearchResult
    {
        public string? Resposta { get; set; }
        public string Resultado { get; set; } = "CacheMiss"; // CacheHit_Exact, CacheHit_FuzzyStrong, CacheHit_FuzzyWeak, CacheMiss
        public double SimilarityScore { get; set; }
        public Aprendizado? Match { get; set; }
    }

    public interface IAprendizadoService
    {
        Task<AprendizadoSearchResult> BuscarAprendizadoAsync(Guid userId, string texto, string? contexto = null);
    }

    public sealed class AprendizadoService : IAprendizadoService
    {
        private readonly AppDbContext _context;

        public AprendizadoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AprendizadoSearchResult> BuscarAprendizadoAsync(Guid userId, string texto, string? contexto = null)
        {
            var result = new AprendizadoSearchResult();
            var normalizedInput = Helpers.TextHelper.NormalizarParaBusca(texto);
            if (string.IsNullOrWhiteSpace(normalizedInput)) return result;

            // 1. Query otimizada: Apenas ativos, filtrados por escopo (Dono ou Global) e Contexto
            var query = _context.Aprendizados
                .Where(a => a.Ativo && (a.UserId == userId || a.Tipo == "Global"));

            if (!string.IsNullOrEmpty(contexto))
                query = query.Where(a => a.Contexto == contexto || a.Contexto == null);
            else
                query = query.Where(a => a.Contexto == null);

            var candidatos = await query
                .Include(a => a.Respostas)
                .ToListAsync();
            if (!candidatos.Any()) return result;

            // 2. Tentar Match Exato (Caminho mais rápido)
            var matchExato = candidatos.FirstOrDefault(a => a.Texto == normalizedInput);
            if (matchExato != null)
            {
                return await ProcessarMatchEncontrado(matchExato, 1.0, originalInput: normalizedInput, tipoHit: "CacheHit_Exact");
            }

            // 3. Match por Similaridade Combinada (Léxica + Semântica Leve)
            Aprendizado? melhorMatchFuzzy = null;
            double maiorScore = 0;

            foreach (var cand in candidatos)
            {
                double jaccard = Helpers.TextHelper.JaccardSimilarity(normalizedInput, cand.Texto);
                
                // Otimização: Só calcula Levenshtein se Jaccard der algum sinal de vida (> 0.3)
                // ou se as strings forem muito curtas.
                double scoreRelativo = 0;
                if (jaccard > 0.3 || (normalizedInput.Length < 10 && jaccard > 0))
                {
                    double levenshtein = Helpers.TextHelper.LevenshteinSimilarity(normalizedInput, cand.Texto);
                    
                    // Peso: 60% Jaccard (conjunto de palavras) + 40% Levenshtein (ordem/escrita)
                    scoreRelativo = (jaccard * 0.6) + (levenshtein * 0.4);
                }

                if (scoreRelativo > maiorScore)
                {
                    maiorScore = scoreRelativo;
                    melhorMatchFuzzy = cand;
                }
            }

            // Thresholds de decisão
            if (melhorMatchFuzzy != null)
            {
                if (maiorScore >= 0.85)
                {
                    // Reuso direto (Cache Hit forte)
                    return await ProcessarMatchEncontrado(melhorMatchFuzzy, maiorScore, originalInput: normalizedInput, tipoHit: "CacheHit_FuzzyStrong");
                }
                else if (maiorScore >= 0.70)
                {
                    // Reuso com penalidade de confiança (Cache Hit fraco)
                    return await ProcessarMatchEncontrado(melhorMatchFuzzy, maiorScore, originalInput: normalizedInput, penalizar: true, tipoHit: "CacheHit_FuzzyWeak");
                }
            }

            return result;
        }

        private async Task<AprendizadoSearchResult> ProcessarMatchEncontrado(Aprendizado match, double score, string originalInput, string tipoHit, bool penalizar = false)
        {
            // Incrementa métricas de reuso
            match.HitCount++;
            match.LastUsedAt = DateTimeOffset.UtcNow;

            // Se o texto que causou o match é diferente do texto armazenado, contamos como uma variante distinta
            if (match.Texto != originalInput)
            {
                match.VariantesDistintasUsadas++;
            }
            
            if (match.EmQuarentena)
            {
                // Ressurreição
                match.EmQuarentena = false;
                match.QuarentenaDesde = null;
                match.UltimaRessurreicaoAt = DateTimeOffset.UtcNow;
                match.ConfidenceScore = Math.Min(100, match.ConfidenceScore + 10);
            }
            else
            {
                if (penalizar)
                {
                    // Match fraco (0.70-0.85): recupera a resposta mas não sobe confiança rápido, ou até desce um pouco se for muito incerto
                    // Mantém um piso de 10 para não ser deletado pelo GC por causa de variações legítimas
                    match.ConfidenceScore = Math.Max(10, match.ConfidenceScore - 1); 
                }
                else
                {
                    // Match forte (>= 0.85 ou exato): bonifica confiança
                    match.ConfidenceScore = Math.Min(100, match.ConfidenceScore + 2);
                }
            }

            // Seleção de variação de resposta Inteligente (Anti-Robô)
            var respostaEscolhida = match.Resposta;
            if (match.Respostas.Any())
            {
                // Busca a variação menos usada ou sorteia entre as menos usadas
                var varEscolhida = match.Respostas
                    .OrderBy(r => r.UsoCount)
                    .ThenBy(_ => Guid.NewGuid()) // Random entre as que empatam em menor uso
                    .First();
                
                varEscolhida.UsoCount++;
                respostaEscolhida = varEscolhida.Texto;
            }

            // Nota: SaveChangesAsync será chamado pelo controller para evitar conflitos de transação
            
            return new AprendizadoSearchResult 
            {
                Resposta = respostaEscolhida,
                Resultado = tipoHit,
                SimilarityScore = score,
                Match = match
            };
        }
    }
}
