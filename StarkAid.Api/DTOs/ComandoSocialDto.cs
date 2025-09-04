using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs
{
    public class ComandoSocialDto
    {
        [Required]
        public string Comando { get; set; } = string.Empty;

        [Required]
        public string Resposta { get; set; } = string.Empty;
    }
}
