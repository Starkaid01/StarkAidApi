using System.ComponentModel.DataAnnotations;

namespace StarkAid.Web.DTOs
{
    public class UpdateDispositivoEspDto
    {
        public string? Nome { get; set; }
        public string? Ip { get; set; }
        public int? Porta { get; set; }
        public string? Comando { get; set; }
        public string? ComandToEsp { get; set; }
        public string? Status { get; set; }
        public bool? LigadoDesligado { get; set; }
    }
}
