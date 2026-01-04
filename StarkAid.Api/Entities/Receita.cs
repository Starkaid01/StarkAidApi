using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace StarkAid.Api.Entities
{
    public class Receita
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Nome { get; set; } = string.Empty;
        
        public string Categoria { get; set; } = "Geral";
        
        [Required]
        public string Ingredientes { get; set; } = string.Empty;
        
        public List<ReceitaPasso> Passos { get; set; } = new List<ReceitaPasso>();
    }
}
