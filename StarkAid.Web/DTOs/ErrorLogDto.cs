namespace StarkAid.Web.DTOs
{
    public class ErrorLogDto
    {
        public int Id { get; set; }
        public string? Source { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
        public string? UltimoComando { get; set; }
        public string? UltimaResposta { get; set; }
        public string? UltimoDispositivoAcionado { get; set; }
        public string? ErroCompleto { get; set; }
        public string? CodigoDeErro { get; set; }
        public string? DataErro { get; set; }
        public string? HoraErro { get; set; }
        public string AcaoErro { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
