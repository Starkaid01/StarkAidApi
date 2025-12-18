namespace StarkAid.Web.DTOs
{
    public class AgendamentoEwelinkCreateDto
    {
        public string EwelinkDeviceId { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public int Hora { get; set; }
        public int Minuto { get; set; }
        public string Recorrencia { get; set; } = string.Empty;
    }
}
