using System.Text.Json.Serialization;

namespace StarkAid.Api.DTOs.V1.Comodos
{
    public class ComodoDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public List<ComodoDispositivoDto> Dispositivos { get; set; } = new();
    }
    
    public class ComodoDispositivoDto 
    {
         public string DispositivoId { get; set; } = string.Empty;
         public string Tipo { get; set; } = string.Empty;
         public string NomeDispositivo { get; set; } = string.Empty;
         public string? Papel { get; set; }
         [JsonPropertyName("isOn")]
         public bool IsOn { get; set; }
    }
}
