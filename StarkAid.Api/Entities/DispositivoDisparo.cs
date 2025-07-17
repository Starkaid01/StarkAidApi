namespace StarkAid.Api.Entities
{
    public class DispositivoDisparo
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string MqttTopic { get; set; } = string.Empty;
        public string StatusTopic { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    }
}
