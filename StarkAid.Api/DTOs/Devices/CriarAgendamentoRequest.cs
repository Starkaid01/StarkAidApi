namespace StarkAid.Api.DTOs.Devices
{
    public class CriarAgendamentoRequest
    {
        public Guid DeviceId { get; set; }
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; } // ligar / desligar
        public string? Recorrencia { get; set; } // <-- Adiciona isso
    }

}
