using System;

namespace StarkAid.Web.DTOs
{
    public class ErrorLogDto
    {
        public int Id { get; set; }
        public string? UltimoComando { get; set; }
        public string? UltimaResposta { get; set; }
        public string? UltimoDispositivoAcionado { get; set; }
        public string? ErroCompleto { get; set; }
        public string? CodigoDeErro { get; set; }
        public string? DataErro { get; set; }
        public string? HoraErro { get; set; }
        public string? AcaoErro { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Mapeamentos para compatibilidade com o Support.razor atual se necessário
        public string? Message => AcaoErro ?? ErroCompleto;
        public string? StackTrace => ErroCompleto;
    }
}
