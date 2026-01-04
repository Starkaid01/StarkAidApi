using System;

namespace StarkAid.Web.DTOs
{
    public class ComandoSocialDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Comando { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public string? RespostasAleatorias { get; set; } = string.Empty;
        public string? Estilo { get; set; } = string.Empty;
    }
}
