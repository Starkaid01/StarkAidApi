namespace StarkAid.Api.Entities
{
    public class Agendamento
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; }  // ligar / desligar
        public bool Executado { get; set; }
        public string? Recorrencia { get; set; } // "Diario", "Semanal", "Nenhum"
    }
}