namespace StarkAid.Api.DTOs.V1.Devices
{
    public class CriarAgendamentoRequest
    {
        public Guid DeviceId { get; set; }
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; } // ligar / desligar
        public string? Recorrencia { get; set; } // <-- Adiciona isso
    }

}
