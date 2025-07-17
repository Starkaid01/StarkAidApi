using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class Disparo
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DispositivoId { get; set; }
        public DateTime DisparadoEm { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public bool Confirmado { get; set; }
        public DateTime? ConfirmadoEm { get; set; }
    }
}