namespace StarkAid.Api.Options
{
    public class AiTelemetryOptions
    {
        public const string ConfigSection = "AiTelemetry";

        /// <summary>
        /// Sugestão: 0.015 (USD 15.00 por 1M tokens) - Média entre GPT-4o, Claude, etc.
        /// </summary>
        public decimal CostPer1KTokens { get; set; } = 0.015m;

        /// <summary>
        /// Quantos tokens estimamos para uma interação padrão (Prompt + Completion)
        /// </summary>
        public int DefaultTokensPerInteraction { get; set; } = 150;
    }
}
