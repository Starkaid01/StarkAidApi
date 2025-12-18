namespace StarkAid.Web.DTOs
{
    public class AgendamentoDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? DispositivoEspId { get; set; }
        public string? EwelinkDeviceId { get; set; }
        public string TipoAgendamento { get; set; } = string.Empty;
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; } = string.Empty;
        public bool Executado { get; set; }
        public string Recorrencia { get; set; } = string.Empty;
    }
}
