namespace StarkAid.Api.DTOs.V1.Devices
{
    public class EditarAgendamentoRequest
    {
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; }
        public string? Recorrencia { get; set; } // novo campo opcional
    }
}
