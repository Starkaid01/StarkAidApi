using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StarkAid.Api.Entities
{
    public class ReceitaPasso
    {
        [Key]
        public int Id { get; set; }
        
        public int ReceitaId { get; set; }
        public int Ordem { get; set; }
        public string Descricao { get; set; } = string.Empty;

        [JsonIgnore]
        public Receita Receita { get; set; } = null!;
    }
}
