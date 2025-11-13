using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class ConfiguracaoStarkNlp
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string StarkNlpUrl { get; set; } = string.Empty;

        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}