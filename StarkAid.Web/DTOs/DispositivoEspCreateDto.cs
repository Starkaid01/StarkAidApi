namespace StarkAid.Web.DTOs
{
    public class DispositivoEspCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public int Porta { get; set; }
        public string Comando { get; set; } = string.Empty;
        public string ComandToEsp { get; set; } = string.Empty;
        public string? Status { get; set; }
        public bool? LigadoDesligado { get; set; }
    }
}
