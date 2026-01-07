using System.Text.Json.Serialization;

namespace StarkAid.Api.Entities
{
    public class ComodoDispositivo
    {
        public Guid ComodoId { get; set; }
        
        // Supports Guid (Device/Esp) or other IDs.
        public string DispositivoId { get; set; } = string.Empty; 
        
        public string Tipo { get; set; } = string.Empty; // "Device", "Ewelink", "Esp"
        
        public string? Papel { get; set; } // "luz", "tv", "ar", etc.

        // Navigation Properties
        [JsonIgnore]
        public Comodo? Comodo { get; set; }
        
        // No direct navigation to Device/Ewelink since it's polymorphic
    }
}
