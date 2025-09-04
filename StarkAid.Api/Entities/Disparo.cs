using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class Disparo
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DispositivoId { get; set; }

    [Required]
    public DateTimeOffset DisparadoEm { get; set; } // datetimeoffset no DbContext

    [Required]
    public string Mensagem { get; set; } = string.Empty;

    [Required]
    public bool Confirmado { get; set; }

    public DateTimeOffset? ConfirmadoEm { get; set; } // datetimeoffset no DbContext

    public User User { get; set; } = null!;
    public DispositivoDisparo Dispositivo { get; set; } = null!;
}