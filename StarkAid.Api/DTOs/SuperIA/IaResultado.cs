namespace StarkAid.Api.DTOs.SuperIA
{
    public class IaResultado
    {
        public string Texto { get; set; } = "";
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public string Modelo { get; set; } = "";
        public string? HitResult { get; set; }
        public double? SimilarityScore { get; set; }
        public string? AprendizadoTipo { get; set; }
        public Guid? AprendizadoId { get; set; }
    }
}
