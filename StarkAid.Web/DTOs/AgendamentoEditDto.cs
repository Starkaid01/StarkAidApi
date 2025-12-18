namespace StarkAid.Web.DTOs
{
    public class AgendamentoEditDto
    {
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; } = string.Empty;
        public string Recorrencia { get; set; } = string.Empty;
    }
}
