using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class Piada
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Texto { get; set; } = string.Empty;
        
        public string Categoria { get; set; } = "Geral";
        
        public bool Ativa { get; set; } = true;
    }
}
