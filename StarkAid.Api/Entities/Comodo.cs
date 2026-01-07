using System.Text.Json.Serialization;

namespace StarkAid.Api.Entities
{
    public class Comodo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public User? User { get; set; }
        
        public ICollection<ComodoDispositivo> Dispositivos { get; set; } = new List<ComodoDispositivo>();
    }
}
