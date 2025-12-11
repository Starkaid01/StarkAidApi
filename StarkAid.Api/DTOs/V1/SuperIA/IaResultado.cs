namespace StarkAid.Api.DTOs.V1.SuperIA
{
    public class IaResultado
    {
        public string Texto { get; set; } = "";
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public string Modelo { get; set; } = "";
    }
}
