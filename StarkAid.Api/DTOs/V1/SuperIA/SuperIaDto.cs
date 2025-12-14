namespace StarkAid.Api.DTOs.V1.SuperIA
{
    public class SuperIaDto
    {

        public string Texto { get; set; } = string.Empty;
        public string ContextoUser { get; set; } = string.Empty; // última pergunta
        public string ContextoIA { get; set; } = string.Empty;   // última resposta

        public string Estilo { get; set; } = string.Empty;
        public bool UseStarkCoins { get; set; } = false; // Indica se o app autorizou uso de StarkCoins
    }

}
