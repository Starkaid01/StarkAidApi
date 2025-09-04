namespace StarkAid.Api.Dtos
{
    public class DisparoResponse
    {
        public Guid Id { get; set; }
        public Guid DispositivoId { get; set; }
        public string DispositivoNome { get; set; } = string.Empty;
        public DateTimeOffset DisparadoEm { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public bool Confirmado { get; set; }
        public DateTimeOffset? ConfirmadoEm { get; set; }
    }
}
