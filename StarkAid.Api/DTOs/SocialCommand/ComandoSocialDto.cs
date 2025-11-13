using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs.SocialCommand
{
    public class ComandoSocialDto
    {
        [Required]
        public string Comando { get; set; } = string.Empty;

        [Required]
        public string Resposta { get; set; } = string.Empty;

        public string Estilo { get; set; } = string.Empty;
    }
}
