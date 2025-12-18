namespace StarkAid.Web.DTOs
{
    public class DispositivoEspDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public int Porta { get; set; }
        public string Comando { get; set; } = string.Empty;
        public string ComandToEsp { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool LigadoDesligado { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPingAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}
