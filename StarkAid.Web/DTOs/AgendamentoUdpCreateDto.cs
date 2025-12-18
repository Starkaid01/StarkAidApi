namespace StarkAid.Web.DTOs
{
    public class AgendamentoUdpCreateDto
    {
        public string DispositivoEspId { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public int Hora { get; set; }
        public int Minuto { get; set; }
        public string Recorrencia { get; set; } = string.Empty;
    }
}
