using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public sealed class AiInteractionEvent
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        
        /// <summary>
        /// Hash do usuário para análise estatística sem identificação direta (opcional/privacidade)
        /// </summary>
        [MaxLength(64)]
        public string? UserHash { get; set; }

        // Entrada
        public string TextoOriginal { get; set; } = string.Empty;
        public string TextoNormalizado { get; set; } = string.Empty;

        // Decisão do pipeline
        // Valores: CacheHit_Exact, CacheHit_FuzzyStrong, CacheHit_FuzzyWeak, CacheMiss
        [MaxLength(50)]
        public string Resultado { get; set; } = string.Empty;

        public double? SimilarityScore { get; set; }

        // Fonte do conhecimento
        [MaxLength(50)]
        public string? AprendizadoTipo { get; set; }
        public Guid? AprendizadoId { get; set; }

        // Métricas
        public int LatenciaMs { get; set; }
        public bool ChamouIaExterna { get; set; }

        // Economia
        public int TokensEstimadosEvitados { get; set; }
        public decimal EconomiaUSD { get; set; }

        // Contexto técnico
        [MaxLength(50)]
        public string Origem { get; set; } = "Android";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
