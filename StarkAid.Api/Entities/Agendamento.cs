using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class Agendamento
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DeviceId { get; set; }

    [Required]
    public DateTimeOffset AgendadoPara { get; set; } // datetimeoffset no DbContext

    [Required]
    public string Comando { get; set; } = string.Empty;

    [Required]
    public bool Executado { get; set; }

    public string? Recorrencia { get; set; }

    public User User { get; set; } = null!;
    public Device Device { get; set; } = null!;
}