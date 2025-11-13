namespace StarkAid.Api.DTOs.SuperIA
{
    public class SuperIaDto
    {

        public string Texto { get; set; } = string.Empty;
        public string ContextoUser { get; set; } = string.Empty; // última pergunta
        public string ContextoIA { get; set; } = string.Empty;   // última resposta

        public string Estilo { get; set; } = string.Empty;
    }

}
