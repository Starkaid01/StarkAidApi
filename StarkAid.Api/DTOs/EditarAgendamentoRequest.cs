namespace StarkAid.Api.DTOs
{
    public class EditarAgendamentoRequest
    {
        public DateTime AgendadoPara { get; set; }
        public string Comando { get; set; }
        public string? Recorrencia { get; set; } // novo campo opcional
    }
}
