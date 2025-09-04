using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs
{
    public class ComandSocEdtDelDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string Comando { get; set; } = string.Empty;

        [Required]
        public string Resposta { get; set; } = string.Empty;
    }
}
