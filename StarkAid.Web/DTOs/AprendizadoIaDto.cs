using System;

namespace StarkAid.Web.DTOs
{
    public class AprendizadoIaDto
    {
        public Guid Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Global";
        public string? Contexto { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        
        public int HitCount { get; set; }
        public int ConfidenceScore { get; set; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public Guid? UserId { get; set; }
        
        public bool Ativo { get; set; }
        public bool EmQuarentena { get; set; }
        public DateTimeOffset? QuarentenaDesde { get; set; }
        public int VariantesDistintasUsadas { get; set; }
        
        public List<AprendizadoRespostaDto> Respostas { get; set; } = new();
    }
}
